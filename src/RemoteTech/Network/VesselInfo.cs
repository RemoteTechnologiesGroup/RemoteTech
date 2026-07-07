using System;
using RemoteTech.Collections;
using RemoteTech.SimpleTypes;
using Unity.Mathematics;

namespace RemoteTech.Network;

internal enum NodeKind : byte
{
    Vessel = 0,
    GroundStation = 1,
}

[Flags]
internal enum NodeFlags : byte
{
    None = 0,
    Powered = 1 << 0,
    CanRelay = 1 << 1,
    InBlackout = 1 << 2,
    IsCommandStation = 1 << 3,

    // Set by NetworkUpdate.ComputeVesselVisibility, for line-render filtering.
    Visible = 1 << 4,
}

[Flags]
internal enum AntennaFlags : byte
{
    None = 0,
    Activated = 1 << 0,
    Powered = 1 << 1,
    CanTarget = 1 << 2,
}

internal struct JobNode
{
    public Guid guid;
    public double3 position;
    public IntRange antennas;
    public int bodyIndex;
    public NodeKind kind;
    public NodeFlags flags;

    public bool Powered => (flags & NodeFlags.Powered) != 0;
    public bool CanRelay => (flags & NodeFlags.CanRelay) != 0;
    public bool InBlackout => (flags & NodeFlags.InBlackout) != 0;
    public bool IsCommandStation => (flags & NodeFlags.IsCommandStation) != 0;
}

/// <summary>
/// Snapshot of an antenna for the per-tick network update jobs.
/// </summary>
internal struct JobAntenna
{
    public double omni;
    public double dish;
    public double cosAngle;
    public int nodeIndex;

    /// <summary>
    /// Target encoded as a signed int:
    /// - 0: no target
    /// - n > 0: node at index n-1
    /// - n < 0: body at index -n-1
    /// </summary>
    public int target;

    public AntennaFlags flags;

    public bool Activated => (flags & AntennaFlags.Activated) != 0;
    public bool Powered => (flags & AntennaFlags.Powered) != 0;
    public bool CanTarget => (flags & AntennaFlags.CanTarget) != 0;
}

/// <summary>
/// A <see cref="JobAntenna"/> paired with its still-unresolved target guid;
/// <see cref="NetworkUpdate.ComputeAntennaInfo"/> resolves the guid to a
/// node or body index.
/// </summary>
internal struct RawAntenna
{
    public JobAntenna antenna;
    public Guid target;
}

internal static class SatelliteRecordUtil
{
    public static RawAntenna BuildAntenna(in AntennaData antenna)
    {
        var flags = AntennaFlags.None;
        if (antenna.Activated) flags |= AntennaFlags.Activated;
        if (antenna.Powered) flags |= AntennaFlags.Powered;
        if (antenna.CanTarget) flags |= AntennaFlags.CanTarget;

        return new()
        {
            antenna = new JobAntenna
            {
                omni = antenna.Omni,
                dish = antenna.Dish,
                cosAngle = antenna.CosAngle,
                flags = flags
            },
            target = antenna.Target
        };
    }
}

