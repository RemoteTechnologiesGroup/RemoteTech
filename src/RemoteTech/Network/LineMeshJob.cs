using System.Runtime.InteropServices;
using RemoteTech.SimpleTypes;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RemoteTech.Network;

/// <summary>
/// A connection line to draw: two node indices and a resolved colour.
/// </summary>
internal struct DrawEdge
{
    public int a;
    public int b;
    public Color32 color;
}

/// <summary>
/// Axis-aligned bounds in scaled space, computed by <see cref="LineBoundsJob"/>.
/// </summary>
internal struct Bounds3
{
    public float3 min;
    public float3 max;
}

/// <summary>
/// One mesh vertex; field order matches the renderer's vertex layout (pos, Color32, uv).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LineVertex
{
    public float3 position;
    public Color32 color;
    public float2 uv;
}

/// <summary>
/// Per-frame inputs for <see cref="BuildDrawEdgesJob"/>, captured on the main thread.
/// </summary>
internal struct DrawEdgeParams
{
    public int targetNode;   // camera-target vessel's node, or -1
    public int nodeCount;
    public int pairCount;
    public byte showOmni, showDish, showPath, showMultiPath, signalRelay;
    public Color32 active, direct, omni, dish, grey;
}

/// <summary>
/// Selects the visible edges and resolves each colour (the old CheckVisibility/CheckColor, by node index),
/// then sizes the vertex/index buffers to the selected-edge count (4 verts / 6 indices each).
/// </summary>
[BurstCompile]
internal struct BuildDrawEdgesJob : IJob
{
    [ReadOnly] public NativeArray<NetworkEdge> edges;    // triangular pair index
    [ReadOnly] public NativeArray<int> parent;           // any-root predecessor forest (command-station roots)
    [ReadOnly] public NativeArray<JobNode> nodes;        // flags: Visible | CanRelay, set at tick time by NetworkUpdateJob
    public DrawEdgeParams p;
    public NativeList<DrawEdge> outEdges;
    public NativeList<LineVertex> verts;
    public NativeList<uint> indices;

    public void Execute()
    {
        var onPath = new NativeBitArray(math.max(p.nodeCount, 1), Allocator.Temp, NativeArrayOptions.ClearMemory);
        if (p.showPath != 0 && p.targetNode >= 0)
        {
            for (int cur = p.targetNode; cur >= 0; cur = parent[cur])
                onPath.Set(cur, true);
        }
        // A route back to the command station is only worth drawing if a visible
        // vessel actually sits at its far end; a path rooted at a filtered-out
        // vessel stays hidden even though its edges still exist in the forest.
        if (p.showMultiPath != 0)
        {
            for (int i = 0; i < p.nodeCount; i++)
            {
                if (nodes[i].kind != NodeKind.Vessel || (nodes[i].flags & NodeFlags.Visible) == 0)
                    continue;
                for (int cur = i; cur >= 0; cur = parent[cur])
                    onPath.Set(cur, true);
            }
        }

        outEdges.Capacity = math.max(p.nodeCount, 16);
        outEdges.Clear();
        for (int pair = 0; pair < p.pairCount; pair++)
        {
            NetworkEdge r = edges[pair];
            if (!r.valid)
                continue;

            bool tree = parent[r.aIdx] == r.bIdx || parent[r.bIdx] == r.aIdx;
            bool onP = tree && onPath.IsSet(r.aIdx) && onPath.IsSet(r.bIdx);

            bool visA = (nodes[r.aIdx].flags & NodeFlags.Visible) != 0;
            bool visB = (nodes[r.bIdx].flags & NodeFlags.Visible) != 0;
            bool show = onP
                     || (r.linkType == LinkType.Omni && p.showOmni != 0 && visA && visB)
                     || (r.linkType == LinkType.Dish && p.showDish != 0 && visA && visB);
            if (!show)
                continue;

            Color32 col;
            if (onP)
                col = p.active;
            else if (p.signalRelay != 0 && (!nodes[r.aIdx].CanRelay || !nodes[r.bIdx].CanRelay))
                col = p.direct;
            else if (r.linkType == LinkType.Omni)
                col = p.omni;
            else if (r.linkType == LinkType.Dish)
                col = p.dish;
            else
                col = p.grey;

            outEdges.Add(new DrawEdge { a = r.aIdx, b = r.bIdx, color = col });
        }

        onPath.Dispose();

        int l = outEdges.Length;
        verts.ResizeUninitialized(4 * l);
        indices.ResizeUninitialized(6 * l);
    }
}

/// <summary>
/// Camera/projection helpers shared by the mesh job (replicate Unity's screen transforms).
/// </summary>
internal static class LineMeshCore
{
    /// <summary>
    /// Local (physics) space -> scaled space: the affine transform ScaledSpace applies.
    /// </summary>
    public static float3 ToScaled(double3 local, double invScale, double3 totalOffset)
        => (float3)(local * invScale - totalOffset);

    /// <summary>
    /// Replicates <c>Camera.WorldToScreenPoint</c> (z = world distance in front of the camera).
    /// </summary>
    public static float3 WorldToScreen(float3 world, float4x4 view, float4x4 proj, float pw, float ph)
    {
        float4 v = math.mul(view, new float4(world, 1f));
        float4 c = math.mul(proj, v);
        float3 ndc = c.xyz / c.w;
        return new float3((ndc.x * 0.5f + 0.5f) * pw, (ndc.y * 0.5f + 0.5f) * ph, -v.z);
    }

    /// <summary>
    /// Replicates <c>Camera.ScreenToWorldPoint</c> (s.z = world distance in front).
    /// </summary>
    public static float3 ScreenToWorld(float3 s, float4x4 proj, float4x4 invProj, float4x4 invView, float pw, float ph)
    {
        float2 ndc = new(s.x / pw * 2f - 1f, s.y / ph * 2f - 1f);
        float eyeZ = -s.z;
        float4 atDepth = math.mul(proj, new float4(0f, 0f, eyeZ, 1f));
        float clipW = atDepth.w;
        float ndcZ = atDepth.z / atDepth.w;
        float4 clip = new float4(ndc.x, ndc.y, ndcZ, 1f) * clipW;
        float4 eye = math.mul(invProj, clip);
        float3 eyePt = eye.xyz / eye.w;
        return math.mul(invView, new float4(eyePt, 1f)).xyz;
    }

    /// <summary>
    /// Mirror a screen point through a pivot (behind-camera handling for the 2D path).
    /// </summary>
    public static float3 FlipDirection(float3 point, float3 pivot) => 2f * pivot - point;
}

/// <summary>
/// Camera/ScaledSpace snapshot for <see cref="NetworkState.ScheduleLineMesh"/>.
/// </summary>
internal struct LineMeshViewParams
{
    public double invScale;
    public double3 totalOffset;
    public float4x4 view, proj, invView, invProj;
    public float pixelWidth, pixelHeight, halfWidth;
    public int mode;
}

/// <summary>
/// Builds a billboard quad per edge into the mesh buffers.
/// </summary>
[BurstCompile(FloatMode = FloatMode.Fast)]
internal struct LineMeshJob : IJobParallelForDefer
{
    [ReadOnly] public NativeArray<DrawEdge> edges;      // drawEdges.AsDeferredJobArray()
    [ReadOnly] public NativeArray<JobNode> nodes;       // NetworkState.Nodes (position is local space)
    public double invScale;
    public double3 totalOffset;
    public float4x4 view, proj, invView, invProj;
    public float pixelWidth, pixelHeight, halfWidth;
    public int mode;                                    // 0 = 3D billboard, 1 = 2D faked-depth overlay

    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<LineVertex> verts;
    [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<uint> indices;

    public void Execute(int i)
    {
        DrawEdge e = edges[i];
        float3 a = LineMeshCore.WorldToScreen(LineMeshCore.ToScaled(nodes[e.a].position, invScale, totalOffset), view, proj, pixelWidth, pixelHeight);
        float3 b = LineMeshCore.WorldToScreen(LineMeshCore.ToScaled(nodes[e.b].position, invScale, totalOffset), view, proj, pixelWidth, pixelHeight);

        if (mode != 0) // 2D flat overlay: clamp to a near plane, flip behind-camera endpoints
        {
            if (a.z < 0f) a = LineMeshCore.FlipDirection(a, b);
            else if (b.z < 0f) b = LineMeshCore.FlipDirection(b, a);
            float d = pixelHeight * 0.5f + 0.01f;
            a.z = a.z >= 0.15f ? d : -d;
            b.z = b.z >= 0.15f ? d : -d;
        }

        float2 dir = new(b.y - a.y, a.x - b.x);
        float len = math.length(dir);
        float2 ndir = len > 1e-6f ? dir / len : new float2(1f, 0f);
        float3 seg = new(ndir * halfWidth, 0f);

        float3 p0 = LineMeshCore.ScreenToWorld(a - seg, proj, invProj, invView, pixelWidth, pixelHeight);
        float3 p1 = LineMeshCore.ScreenToWorld(a + seg, proj, invProj, invView, pixelWidth, pixelHeight);
        float3 p2 = LineMeshCore.ScreenToWorld(b - seg, proj, invProj, invView, pixelWidth, pixelHeight);
        float3 p3 = LineMeshCore.ScreenToWorld(b + seg, proj, invProj, invView, pixelWidth, pixelHeight);

        Color32 col = e.color;
        int vb = 4 * i;
        verts[vb + 0] = new LineVertex { position = p0, color = col, uv = new float2(0f, 1f) };
        verts[vb + 1] = new LineVertex { position = p1, color = col, uv = new float2(0f, 0f) };
        verts[vb + 2] = new LineVertex { position = p2, color = col, uv = new float2(1f, 1f) };
        verts[vb + 3] = new LineVertex { position = p3, color = col, uv = new float2(1f, 0f) };

        int ib = 6 * i;
        uint v = (uint)vb;
        indices[ib + 0] = v + 0; indices[ib + 1] = v + 2; indices[ib + 2] = v + 1;
        indices[ib + 3] = v + 2; indices[ib + 4] = v + 3; indices[ib + 5] = v + 1;
    }
}

/// <summary>
/// Reduces the written verts to an AABB for mesh.bounds.
/// </summary>
[BurstCompile]
internal struct LineBoundsJob : IJob
{
    [ReadOnly] public NativeArray<LineVertex> verts;    // _verts.AsDeferredJobArray()
    [WriteOnly] public NativeArray<Bounds3> boundsOut;

    public void Execute()
    {
        int n = verts.Length;
        if (n == 0)
        {
            boundsOut[0] = new Bounds3 { min = float3.zero, max = float3.zero };
            return;
        }
        float3 mn = verts[0].position;
        float3 mx = mn;
        for (int i = 1; i < n; i++)
        {
            float3 pos = verts[i].position;
            mn = math.min(mn, pos);
            mx = math.max(mx, pos);
        }
        boundsOut[0] = new Bounds3 { min = mn, max = mx };
    }
}

/// <summary>
/// One frame's mesh buffers from <see cref="NetworkState.ScheduleLineMesh"/>; verts are
/// billboarded against <see cref="builtOffset"/>, so the draw must translate them onto the
/// current ScaledSpace origin.
/// </summary>
internal struct LineMeshData
{
    public NativeList<DrawEdge> drawEdges;
    public NativeList<LineVertex> verts;
    public NativeList<uint> indices;
    public NativeArray<Bounds3> bounds;
    public JobHandle handle;
    public double3 builtOffset;

    public void Dispose()
    {
        if (drawEdges.IsCreated) drawEdges.Dispose();
        if (verts.IsCreated) verts.Dispose();
        if (indices.IsCreated) indices.Dispose();
        if (bounds.IsCreated) bounds.Dispose();
    }
}
