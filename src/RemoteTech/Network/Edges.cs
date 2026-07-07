using System.Runtime.InteropServices;
using RemoteTech.Collections;
using RemoteTech.SimpleTypes;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using RModel = RemoteTech.RangeModel.RangeModel;

namespace RemoteTech.Network;


internal struct NetworkEdge
{
    public double distance;
    /// <summary>Max joint range the two nodes' antennas could achieve, independent
    /// of <see cref="distance"/>. Only nonzero when <see cref="valid"/>.</summary>
    public double maxRange;
    public int aIdx;
    public int bIdx;
    public LinkType linkType;

    public readonly bool valid => linkType != LinkType.None;
}

[BurstCompile]
internal struct BuildEdgesJob : IJobParallelFor
{
    public JobConfig config;
    public RModel model;

    [ReadOnly] public NativeArray<JobNode> nodes;
    [ReadOnly] public NativeArray<JobAntenna> antennas;
    [ReadOnly] public NativeArray<JobBody> bodies;
    [ReadOnly] public NativeArray<double> vesselMaxRange;
    [ReadOnly] public NativeArray<double3> antennaDirections;
    [WriteOnly] public NativeArray<NetworkEdge> edges;

    public void Execute(int index)
    {
        if (model == RModel.Additive)
            Execute(index, new AdditiveRangeModel());
        else
            Execute(index, new StandardRangeModel());
    }

    void Execute<TRange>(int index, TRange model)
        where TRange : unmanaged, IRangeModel
    {
        NetworkUpdateMath.DecodePairIndex(index, out int i, out int j);

        JobNode a = nodes[i];
        JobNode b = nodes[j];

        ref var record = ref edges.GetElement(index);
        record = new NetworkEdge()
        {
            aIdx = i,
            bIdx = j,
            linkType = LinkType.None,
            distance = math.distance(a.position, b.position)
        };

        double distance = record.distance;

        // if either node is in blackout then we have nothing else to do
        if (a.InBlackout || b.InBlackout)
            return;

        // otherwise, pre-filter by range
        if (!NetworkUpdateMath.CouldPossiblyLink(vesselMaxRange[i], vesselMaxRange[j], distance))
            return;

        // attempt line of sight
        if (!config.ignoreLineOfSight)
        {
            int nca = GetNearestCommonAncestor(a.bodyIndex, b.bodyIndex);
            JobBody ncaBody = bodies[nca];
            if (!NetworkUpdateMath.HasLineOfSight(a.position, b.position, bodies, ncaBody.subtree))
                return;
        }

        // now do the full range model
        double oc = config.omniClamp;
        double dc = config.dishClamp;
        double mult = config.multipleAntennaMultiplier;

        double maxOmniA = MaxOmni(a.antennas);
        double maxOmniB = MaxOmni(b.antennas);
        double bonusA = NetworkUpdateMath.GetMultipleAntennaBonus(antennas, a.antennas, maxOmniA, mult);
        double bonusB = NetworkUpdateMath.GetMultipleAntennaBonus(antennas, b.antennas, maxOmniB, mult);

        double3 dirAB = b.position - a.position;
        double3 dirBA = -dirAB;
        double invDist = record.distance > 0.0 ? 1.0 / record.distance : 0.0;
        double3 dirABNorm = dirAB * invDist;
        double3 dirBANorm = dirBA * invDist;

        double maxDishA = MaxConnectedDish(a.antennas, dirABNorm);
        double maxDishB = MaxConnectedDish(b.antennas, dirBANorm);

        // CheckRange is symmetric under swapping (r1,clamp1)<->(r2,clamp2), so these
        // four also cover the "from B" combinations instead of recomputing them.
        double rangeOO = Range(model, maxOmniA + bonusA, oc, maxOmniB + bonusB, oc);
        double rangeOD = Range(model, maxOmniA + bonusA, oc, maxDishB,          dc);
        double rangeDO = Range(model, maxDishA,          dc, maxOmniB + bonusB, oc);
        double rangeDD = Range(model, maxDishA,          dc, maxDishB,          dc);

        bool reachAOmni = rangeOO >= distance || rangeOD >= distance;
        bool reachADish = rangeDO >= distance || rangeDD >= distance;
        bool reachBOmni = rangeOO >= distance || rangeDO >= distance;
        bool reachBDish = rangeOD >= distance || rangeDD >= distance;

        bool aSideReaches = reachAOmni || reachADish;
        bool bSideReaches = reachBOmni || reachBDish;
        if (!aSideReaches || !bSideReaches)
            return;

        record.maxRange = math.max(math.max(rangeOO, rangeOD), math.max(rangeDO, rangeDD));
        record.linkType = (reachADish || reachBDish) ? LinkType.Dish : LinkType.Omni;
    }

    int GetNearestCommonAncestor(int bodyA, int bodyB)
    {
        while (bodyA != bodyB)
        {
            var min = math.min(bodyA, bodyB);
            var max = math.max(bodyA, bodyB);

            if (min < 0)
                return 0;

            bodyA = min;
            bodyB = bodies[max].parent;
        }

        return bodyA;
    }

    double MaxOmni(IntRange range)
    {
        double max = 0.0;
        foreach (int k in range)
            max = math.max(max, antennas[k].omni);

        return max;
    }
    
    /// <summary>
    /// The range of the largest dish that is actually pointed towards the target
    /// direction.
    /// </summary>
    private double MaxConnectedDish(IntRange range, double3 dir)
    {
        double max = 0.0;
        foreach (int k in range)
        {
            JobAntenna ant = antennas[k];
            if (ant.dish <= 0.0)
                continue;

            if (ant.target == 0)
                continue;
            double dot = math.dot(antennaDirections[k], dir);
            if (dot < ant.cosAngle)
                continue;

            max = math.max(max, ant.dish);
        }

        return max;
    }

    private static double Range<TRange>(
        TRange model,
        double r1,
        double clamp1,
        double r2,
        double clamp2)
        where TRange : unmanaged, IRangeModel
    {
        return NetworkUpdateMath.CheckRange(model, r1, clamp1, r2, clamp2);
    }
}
