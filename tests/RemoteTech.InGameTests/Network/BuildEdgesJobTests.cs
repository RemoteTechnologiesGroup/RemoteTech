using System.Collections.Generic;
using KSP.Testing;
using RemoteTech.Collections;
using RemoteTech.Network;
using RemoteTech.SimpleTypes;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using RModel = RemoteTech.RangeModel.RangeModel;

namespace RemoteTech.InGameTests.Network;

/// <summary>
/// Two nodes a distance apart, joined (or not) by a single edge — exercises BuildEdgesJob's reject ladder and link typing.
/// </summary>
public class BuildEdgesJobTests : RTTestBase
{
    [TestInfo("BuildEdgesJobTests_TwoOmnis_WithinRange_FormOmniLink")]
    public void TwoOmnis_WithinRange_FormOmniLink()
    {
        var scene = new EdgeScene { IgnoreLos = true };
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsTrue(e.valid);
        Assert.AreEqual(LinkType.Omni, e.linkType);
        Assert.AreEqual(100.0, e.distance, 1e-6);
    }

    [TestInfo("BuildEdgesJobTests_TwoOmnis_OutOfRange_NoLink")]
    public void TwoOmnis_OutOfRange_NoLink()
    {
        // Prefilter passes (maxRange large) but the omni range itself is too short.
        var scene = new EdgeScene { IgnoreLos = true };
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9, AntSpec.Omni(50));
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Omni(50));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsFalse(e.valid);
    }

    [TestInfo("BuildEdgesJobTests_Blackout_ShortCircuits_NoLink")]
    public void Blackout_ShortCircuits_NoLink()
    {
        var scene = new EdgeScene { IgnoreLos = true };
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9, NodeFlags.Powered | NodeFlags.InBlackout, AntSpec.Omni(1000));
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsFalse(e.valid);
    }

    [TestInfo("BuildEdgesJobTests_RangePrefilter_RejectsBeforeRangeModel")]
    public void RangePrefilter_RejectsBeforeRangeModel()
    {
        // Huge antennas, but the per-vessel max-range cap is below the distance,
        // so the cheap prefilter rejects before the range model runs.
        var scene = new EdgeScene { IgnoreLos = true };
        scene.AddNode(new double3(0, 0, 0), maxRange: 10, AntSpec.Omni(1e9));
        scene.AddNode(new double3(100, 0, 0), maxRange: 10, AntSpec.Omni(1e9));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsFalse(e.valid);
    }

    [TestInfo("BuildEdgesJobTests_Occluder_BetweenNodes_BlocksLink")]
    public void Occluder_BetweenNodes_BlocksLink()
    {
        var scene = new EdgeScene { IgnoreLos = false };
        scene.AddBody(new double3(50, 0, 0), radius: 10);
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsFalse(e.valid);
    }

    [TestInfo("BuildEdgesJobTests_Occluder_OffAxis_LeavesLinkClear")]
    public void Occluder_OffAxis_LeavesLinkClear()
    {
        var scene = new EdgeScene { IgnoreLos = false };
        scene.AddBody(new double3(50, 100, 0), radius: 10);
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsTrue(e.valid);
        Assert.AreEqual(LinkType.Omni, e.linkType);
    }

    [TestInfo("BuildEdgesJobTests_TwoDishes_DirectlyTargeting_FormDishLink")]
    public void TwoDishes_DirectlyTargeting_FormDishLink()
    {
        var scene = new EdgeScene { IgnoreLos = true };
        // node 0 dish -> node 1 (encoded target = otherIndex + 1 = 2)
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9, AntSpec.Dish(1000, target: 2));
        // node 1 dish -> node 0 (encoded target = 1)
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Dish(1000, target: 1));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsTrue(e.valid);
        Assert.AreEqual(LinkType.Dish, e.linkType);
    }

    [TestInfo("BuildEdgesJobTests_Dish_SeesTargetWithinCone_FormsLink")]
    public void Dish_SeesTargetWithinCone_FormsLink()
    {
        var scene = new EdgeScene { IgnoreLos = true };
        // node 0 dish points down +X (toward node 1) with a wide cone, but is not
        // directly targeting node 1 (target encodes "some body"). The cone test
        // should still see node 1.
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9,
            AntSpec.Dish(1000, target: -1, cosAngle: 0.5, dir: new double3(1, 0, 0)));
        // node 1 answers with a plain omni.
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsTrue(e.valid);
        Assert.AreEqual(LinkType.Dish, e.linkType);
    }

    [TestInfo("BuildEdgesJobTests_Dish_TargetOutsideCone_NoLink")]
    public void Dish_TargetOutsideCone_NoLink()
    {
        // Dish points away (-X) from node 1, narrow cone -> cannot see it; no omni to fall back on.
        var scene = new EdgeScene { IgnoreLos = true };
        scene.AddNode(new double3(0, 0, 0), maxRange: 1e9,
            AntSpec.Dish(1000, target: -1, cosAngle: 0.9, dir: new double3(-1, 0, 0)));
        scene.AddNode(new double3(100, 0, 0), maxRange: 1e9, AntSpec.Omni(1000));

        NetworkEdge e = scene.Run(RModel.Standard);

        Assert.IsFalse(e.valid);
    }
}

/// <summary>
/// Antenna spec for <see cref="EdgeScene"/>.
/// </summary>
internal struct AntSpec
{
    public double OmniRange;
    public double DishRange;
    public double CosAngle;
    public int Target;
    public double3 Dir;

    public static AntSpec Omni(double range) => new AntSpec { OmniRange = range };

    public static AntSpec Dish(double range, int target, double cosAngle = 0.0, double3 dir = default)
        => new AntSpec { DishRange = range, Target = target, CosAngle = cosAngle, Dir = dir };
}

/// <summary>
/// Builds a real BuildEdgesJob run over a 2-node pair. Body 0 is the universal
/// nearest-common-ancestor whose subtree spans every body, so any added body is
/// an occlusion candidate.
/// </summary>
internal sealed class EdgeScene
{
    private readonly List<JobNode> _nodes = new();
    private readonly List<JobAntenna> _antennas = new();
    private readonly List<double3> _dirs = new();
    private readonly List<JobBody> _bodies = new();
    private readonly List<double> _maxRange = new();

    public bool IgnoreLos = true;
    public double OmniClamp = 1.0;
    public double DishClamp = 1.0;
    public double MultipleAntennaMultiplier = 0.0;

    public void AddBody(double3 pos, double radius)
    {
        _bodies.Add(new JobBody { position = pos, radius = radius });
    }

    public int AddNode(double3 pos, double maxRange, params AntSpec[] ants)
        => AddNode(pos, maxRange, NodeFlags.Powered | NodeFlags.CanRelay, ants);

    public int AddNode(double3 pos, double maxRange, NodeFlags flags, params AntSpec[] ants)
    {
        int start = _antennas.Count;
        int nodeIdx = _nodes.Count;
        foreach (var a in ants)
        {
            _antennas.Add(new JobAntenna
            {
                nodeIndex = nodeIdx,
                omni = a.OmniRange,
                dish = a.DishRange,
                cosAngle = a.CosAngle,
                target = a.Target,
                flags = AntennaFlags.Activated | AntennaFlags.Powered,
            });
            _dirs.Add(a.Dir);
        }
        _nodes.Add(new JobNode
        {
            position = pos,
            bodyIndex = 0,
            antennas = new IntRange(start, ants.Length),
            kind = NodeKind.Vessel,
            flags = flags,
        });
        _maxRange.Add(maxRange);
        return nodeIdx;
    }

    public NetworkEdge Run(RModel model)
    {
        int bodyCount = System.Math.Max(1, _bodies.Count);
        var bodyArr = new JobBody[bodyCount];
        for (int i = 0; i < _bodies.Count; i++) bodyArr[i] = _bodies[i];
        // Body 0 is the universal NCA; its subtree spans all bodies so the LOS
        // sweep considers every occluder we added.
        var b0 = bodyArr[0];
        b0.subtree = new IntRange(0, bodyCount);
        bodyArr[0] = b0;

        var nodes = new NativeArray<JobNode>(_nodes.ToArray(), Allocator.TempJob);
        var antennas = new NativeArray<JobAntenna>(_antennas.ToArray(), Allocator.TempJob);
        var dirs = new NativeArray<double3>(_dirs.ToArray(), Allocator.TempJob);
        var bodies = new NativeArray<JobBody>(bodyArr, Allocator.TempJob);
        var maxRange = new NativeArray<double>(_maxRange.ToArray(), Allocator.TempJob);
        var edges = new NativeList<NetworkEdge>(1, Allocator.TempJob);
        edges.ResizeUninitialized(1);

        var config = new JobConfig
        {
            omniClamp = OmniClamp,
            dishClamp = DishClamp,
            multipleAntennaMultiplier = MultipleAntennaMultiplier,
            ignoreLineOfSight = IgnoreLos,
        };

        new BuildEdgesJob
        {
            config = config,
            model = model,
            nodes = nodes,
            antennas = antennas,
            bodies = bodies,
            vesselMaxRange = maxRange,
            antennaDirections = dirs,
            edges = edges.AsArray(),
        }.Schedule(edges.Length, 64).Complete();

        NetworkEdge result = edges[0];

        nodes.Dispose();
        antennas.Dispose();
        dirs.Dispose();
        bodies.Dispose();
        maxRange.Dispose();
        edges.Dispose();

        return result;
    }
}
