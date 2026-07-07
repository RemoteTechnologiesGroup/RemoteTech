using KSP.Testing;
using RemoteTech.Collections;
using RemoteTech.Network;
using Unity.Collections;
using Unity.Mathematics;

namespace RemoteTech.InGameTests.Network;

public class NetworkUpdateMathTests : RTTestBase
{
    [TestInfo("NetworkUpdateMathTests_PairCount_IsTriangularNumber")]
    public void PairCount_IsTriangularNumber()
    {
        Assert.AreEqual(0, NetworkUpdateMath.PairCount(0));
        Assert.AreEqual(0, NetworkUpdateMath.PairCount(1));
        Assert.AreEqual(1, NetworkUpdateMath.PairCount(2));
        Assert.AreEqual(3, NetworkUpdateMath.PairCount(3));
        Assert.AreEqual(6, NetworkUpdateMath.PairCount(4));
        Assert.AreEqual(45, NetworkUpdateMath.PairCount(10));
    }

    [TestInfo("NetworkUpdateMathTests_DecodePairIndex_RoundTripsEveryPair")]
    public void DecodePairIndex_RoundTripsEveryPair()
    {
        // For every node count, walking pair indices 0..PairCount must reproduce
        // the (i, j) with 0 <= i < j used to encode them: k = j*(j-1)/2 + i.
        for (int n = 2; n <= 32; n++)
        {
            int k = 0;
            for (int j = 1; j < n; j++)
            {
                for (int i = 0; i < j; i++, k++)
                {
                    NetworkUpdateMath.DecodePairIndex(k, out int di, out int dj);
                    Assert.AreEqual(i, di);
                    Assert.AreEqual(j, dj);
                }
            }
            Assert.AreEqual(NetworkUpdateMath.PairCount(n), k);
        }
    }

    [TestInfo("NetworkUpdateMathTests_CouldPossiblyLink_ComparesRangeSumToDistance")]
    public void CouldPossiblyLink_ComparesRangeSumToDistance()
    {
        Assert.IsTrue(NetworkUpdateMath.CouldPossiblyLink(100, 100, 200));   // sum exactly meets distance
        Assert.IsFalse(NetworkUpdateMath.CouldPossiblyLink(100, 99, 200));  // just short
        Assert.IsTrue(NetworkUpdateMath.CouldPossiblyLink(0, 0, 0));         // coincident
    }

    [TestInfo("NetworkUpdateMathTests_CheckRange_Standard_TakesMinOfJointAndPerNodeClamps")]
    public void CheckRange_Standard_TakesMinOfJointAndPerNodeClamps()
    {
        var model = default(StandardRangeModel);
        // joint = min(100,200)=100; clamps wide open -> 100
        Assert.AreEqual(100.0, NetworkUpdateMath.CheckRange(in model, 100, 1.0, 200, 1.0), 1e-9);
        // r1 clamp bites: min(100, 100*0.5, 200*1) = 50
        Assert.AreEqual(50.0, NetworkUpdateMath.CheckRange(in model, 100, 0.5, 200, 1.0), 1e-9);
    }

    [TestInfo("NetworkUpdateMathTests_CheckRange_Additive_AddsSqrtProductThenClamps")]
    public void CheckRange_Additive_AddsSqrtProductThenClamps()
    {
        var model = default(AdditiveRangeModel);
        // joint = min(100,100) + sqrt(100*100) = 200, but capped by r*clamp=100
        Assert.AreEqual(100.0, NetworkUpdateMath.CheckRange(in model, 100, 1.0, 100, 1.0), 1e-9);
        // wide clamps let the additive joint through
        Assert.AreEqual(200.0, NetworkUpdateMath.CheckRange(in model, 100, 10.0, 100, 10.0), 1e-9);
    }

    [TestInfo("NetworkUpdateMathTests_GetMultipleAntennaBonus_SumsNonBestOmni")]
    public void GetMultipleAntennaBonus_SumsNonBestOmni()
    {
        var antennas = new NativeArray<JobAntenna>(new[]
        {
            new JobAntenna { omni = 10 },
            new JobAntenna { omni = 20 },
            new JobAntenna { omni = 30 },
        }, Allocator.Temp);
        var range = new IntRange(0, 3);

        Assert.AreEqual(15.0, NetworkUpdateMath.GetMultipleAntennaBonus(antennas, range, maxOmni: 30, 0.5), 1e-9); // (10+20+30-30)*0.5
        Assert.AreEqual(0.0, NetworkUpdateMath.GetMultipleAntennaBonus(antennas, range, maxOmni: 30, 0.0), 1e-9);  // multiplier off -> no bonus
    }

    // --- Line of sight -----------------------------------------------------

    private static NativeArray<JobBody> SingleBody(double3 pos, double radius) =>
        new(new[] { new JobBody { position = pos, radius = radius } }, Allocator.Temp);

    [TestInfo("NetworkUpdateMathTests_HasLineOfSight_BodyDirectlyBetween_Occludes")]
    public void HasLineOfSight_BodyDirectlyBetween_Occludes()
    {
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        var bodies = SingleBody(new double3(50, 0, 0), radius: 10);
        Assert.IsFalse(NetworkUpdateMath.HasLineOfSight(a, b, bodies, new IntRange(0, 1)));
    }

    [TestInfo("NetworkUpdateMathTests_HasLineOfSight_BodyOffToTheSide_Clear")]
    public void HasLineOfSight_BodyOffToTheSide_Clear()
    {
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        var bodies = SingleBody(new double3(50, 100, 0), radius: 10);
        Assert.IsTrue(NetworkUpdateMath.HasLineOfSight(a, b, bodies, new IntRange(0, 1)));
    }

    [TestInfo("NetworkUpdateMathTests_HasLineOfSight_BodyBehindEndpoint_Ignored")]
    public void HasLineOfSight_BodyBehindEndpoint_Ignored()
    {
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        // Behind A (negative projection) and beyond B (projection past the segment)
        var behind = SingleBody(new double3(-50, 0, 0), radius: 40);
        var beyond = SingleBody(new double3(150, 0, 0), radius: 40);
        Assert.IsTrue(NetworkUpdateMath.HasLineOfSight(a, b, behind, new IntRange(0, 1)));
        Assert.IsTrue(NetworkUpdateMath.HasLineOfSight(a, b, beyond, new IntRange(0, 1)));
    }

    [TestInfo("NetworkUpdateMathTests_HasLineOfSight_GrazingWithinMinHeight_DoesNotOcclude")]
    public void HasLineOfSight_GrazingWithinMinHeight_DoesNotOcclude()
    {
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        // Surface passes within MIN_HEIGHT (5m) of the segment: radius - MIN_HEIGHT
        // is the effective occlusion radius. Body centered 5m off-axis with radius 10
        // -> threshold 5, lateral 5 -> 25 < 25 is false -> clear.
        var bodies = SingleBody(new double3(50, 5, 0), radius: 10);
        Assert.IsTrue(NetworkUpdateMath.HasLineOfSight(a, b, bodies, new IntRange(0, 1)));

        // Drop the off-axis distance below the threshold -> occluded.
        var closer = SingleBody(new double3(50, 4, 0), radius: 10);
        Assert.IsFalse(NetworkUpdateMath.HasLineOfSight(a, b, closer, new IntRange(0, 1)));
    }
}
