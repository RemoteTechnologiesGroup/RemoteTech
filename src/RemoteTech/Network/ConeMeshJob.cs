using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RemoteTech.Network;

/// <summary>
/// A cone-eligible antenna, gathered once per tick by
/// <see cref="NetworkUpdate.ComputeConeCandidates"/>; target uses the same
/// encoding as <see cref="JobAntenna.target"/>.
/// </summary>
internal struct ConeCandidate
{
    public int nodeIndex;
    public int target;
    public double cosAngle;
    public double dishRange;
}

/// <summary>
/// Camera/ScaledSpace snapshot for <see cref="NetworkState.ScheduleConeMesh"/>; refUp is the camera target body's up axis used for the cone's side-spread.
/// </summary>
internal struct ConeViewParams
{
    public double invScale;
    public double3 totalOffset;
    public float3 refUp;
    public float4x4 view, proj, invView, invProj;
    public float pixelWidth, pixelHeight, halfWidth;
    public int mode;
    public Color32 color;
}

/// <summary>
/// Builds a pair of billboard quads (the cone's two side lines) per candidate into the mesh buffers.
/// </summary>
[BurstCompile]
internal struct ConeMeshJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<ConeCandidate> candidates;
    [ReadOnly] public NativeArray<JobNode> nodes;
    [ReadOnly] public NativeArray<JobBody> bodies;
    public ConeViewParams p;

    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<LineVertex> verts;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<uint> indices;

    public void Execute(int i)
    {
        ConeCandidate c = candidates[i];
        double3 antennaLocal = nodes[c.nodeIndex].position;
        double3 targetLocal = c.target > 0 ? nodes[c.target - 1].position : bodies[-c.target - 1].position;

        float3 antenna = LineMeshCore.ToScaled(antennaLocal, p.invScale, p.totalOffset);
        float3 target = LineMeshCore.ToScaled(targetLocal, p.invScale, p.totalOffset);

        float dist = math.distance(antenna, target);
        float spread = dist * (float)math.tan(math.acos(c.cosAngle));
        float3 space = math.normalizesafe(math.cross(target - antenna, p.refUp)) * spread;

        float lim = math.min((float)(c.dishRange * p.invScale), dist);
        float3 end1 = antenna + math.normalizesafe(target + space - antenna) * lim;
        float3 end2 = antenna + math.normalizesafe(target - space - antenna) * lim;

        float3 a = LineMeshCore.WorldToScreen(antenna, p.view, p.proj, p.pixelWidth, p.pixelHeight);
        float3 e1 = LineMeshCore.WorldToScreen(end1, p.view, p.proj, p.pixelWidth, p.pixelHeight);
        float3 e2 = LineMeshCore.WorldToScreen(end2, p.view, p.proj, p.pixelWidth, p.pixelHeight);

        if (p.mode != 0) // 2D flat overlay: clamp to a near plane, flip behind-camera endpoints
        {
            if (a.z < 0f)
            {
                float3 coneCenter = LineMeshCore.WorldToScreen(target, p.view, p.proj, p.pixelWidth, p.pixelHeight);
                a = LineMeshCore.FlipDirection(a, coneCenter);
            }
            else if (e1.z < 0f || e2.z < 0f)
            {
                e1 = LineMeshCore.FlipDirection(e1, a);
                e2 = LineMeshCore.FlipDirection(e2, a);
            }

            float d = p.pixelHeight * 0.5f + 0.01f;
            a.z = a.z >= 0f ? d : -d;
            e1.z = e1.z >= 0f ? d : -d;
            e2.z = e2.z >= 0f ? d : -d;
        }

        float2 dir1 = new(e1.y - a.y, a.x - e1.x);
        float2 dir2 = new(e2.y - a.y, a.x - e2.x);
        float3 seg1 = new(math.normalizesafe(dir1) * p.halfWidth, 0f);
        float3 seg2 = new(math.normalizesafe(dir2) * p.halfWidth, 0f);

        int vb = 8 * i;
        verts[vb + 0] = MakeVertex(a - seg1, p, new float2(0f, 1f));
        verts[vb + 1] = MakeVertex(a + seg1, p, new float2(0f, 0f));
        verts[vb + 2] = MakeVertex(e1 - seg1, p, new float2(1f, 1f));
        verts[vb + 3] = MakeVertex(e1 + seg1, p, new float2(1f, 0f));
        verts[vb + 4] = MakeVertex(a - seg2, p, new float2(0f, 1f));
        verts[vb + 5] = MakeVertex(a + seg2, p, new float2(0f, 0f));
        verts[vb + 6] = MakeVertex(e2 - seg2, p, new float2(1f, 1f));
        verts[vb + 7] = MakeVertex(e2 + seg2, p, new float2(1f, 0f));

        int ib = 12 * i;
        uint v = (uint)vb;
        indices[ib + 0] = v + 0; indices[ib + 1] = v + 2; indices[ib + 2] = v + 1;
        indices[ib + 3] = v + 2; indices[ib + 4] = v + 3; indices[ib + 5] = v + 1;
        indices[ib + 6] = v + 4; indices[ib + 7] = v + 6; indices[ib + 8] = v + 5;
        indices[ib + 9] = v + 6; indices[ib + 10] = v + 7; indices[ib + 11] = v + 5;
    }

    private static LineVertex MakeVertex(float3 screen, in ConeViewParams p, float2 uv) => new LineVertex
    {
        position = LineMeshCore.ScreenToWorld(screen, p.proj, p.invProj, p.invView, p.pixelWidth, p.pixelHeight),
        color = p.color,
        uv = uv,
    };
}

internal struct ConeMeshData
{
    public NativeArray<LineVertex> verts;
    public NativeArray<uint> indices;
    public NativeArray<Bounds3> bounds;
    public JobHandle handle;
    public double3 builtOffset;

    public void Dispose()
    {
        verts.Dispose();
        indices.Dispose();
        bounds.Dispose();
    }
}
