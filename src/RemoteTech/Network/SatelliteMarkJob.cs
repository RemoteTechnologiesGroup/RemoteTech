using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RemoteTech.Network;

/// <summary>
/// A satellite whose map-view mark should be considered for drawing (every ground
/// station plus every command-station vessel), gathered once per tick by
/// <see cref="NetworkUpdate.ComputeMarkCandidates"/>.
/// </summary>
internal struct SatelliteMarkCandidate
{
    public int nodeIndex;
    public Color32 color;

    // Orbiting vessels are always shown, so they skip the occlusion/distance filters.
    [MarshalAs(UnmanagedType.U1)]
    public bool alwaysShow;
}

/// <summary>
/// Camera/ScaledSpace snapshot plus the ground-station hiding settings for
/// <see cref="NetworkState.ScheduleSatelliteMarks"/>.
/// </summary>
internal struct SatelliteMarkViewParams
{
    public double invScale;
    public double3 totalOffset;
    public double3 camPosLocal;
    public float4x4 view, proj;
    public float pixelWidth, pixelHeight, screenHeight;
    [MarshalAs(UnmanagedType.U1)]
    public bool hideBehindBody;
    [MarshalAs(UnmanagedType.U1)]
    public bool hideOnDistance;
    public float distanceThreshold;
}

/// <summary>
/// A mark the renderer should draw: the node it belongs to (for main-thread
/// mouse-over lookup), its GUI-space centre and its colour.
/// </summary>
internal struct SatelliteMark
{
    public float2 screenPos;
    public int nodeIndex;
    public Color32 color;
}

/// <summary>
/// Filters the mark candidates down to those visible this frame and projects
/// each survivor to its GUI-space screen position.
/// </summary>
[BurstCompile]
internal struct SatelliteMarkJob : IJob
{
    [ReadOnly] public NativeArray<SatelliteMarkCandidate> candidates;
    [ReadOnly] public NativeArray<JobNode> nodes;
    [ReadOnly] public NativeArray<JobBody> bodies;
    public SatelliteMarkViewParams p;

    public NativeList<SatelliteMark> marks;

    public void Execute()
    {
        marks.Clear();
        for (int i = 0; i < candidates.Length; i++)
        {
            SatelliteMarkCandidate c = candidates[i];
            JobNode node = nodes[c.nodeIndex];

            float3 scaled = LineMeshCore.ToScaled(node.position, p.invScale, p.totalOffset);
            float3 screen = LineMeshCore.WorldToScreen(scaled, p.view, p.proj, p.pixelWidth, p.pixelHeight);
            if (screen.z < 0f)
                continue;

            if (!c.alwaysShow)
            {
                bool occluded = IsOccluded(node.position, bodies[node.bodyIndex].position, p.camPosLocal);
                if (p.hideBehindBody && occluded)
                    continue;
                if (p.hideOnDistance && !occluded
                    && math.distance((float3)p.camPosLocal, (float3)node.position) >= p.distanceThreshold)
                    continue;
            }

            marks.Add(new SatelliteMark
            {
                nodeIndex = c.nodeIndex,
                screenPos = new float2(screen.x, p.screenHeight - screen.y),
                color = c.color,
            });
        }
    }

    // Behind its body when the camera and the body sit on the same side of the mark.
    private static bool IsOccluded(double3 loc, double3 bodyPos, double3 camPos)
        => math.dot(camPos - loc, bodyPos - loc) >= 0.0;
}

internal struct SatelliteMarkData
{
    public NativeList<SatelliteMark> marks;
    public JobHandle handle;

    public void Dispose()
    {
        if (marks.IsCreated)
            marks.Dispose();
    }
}
