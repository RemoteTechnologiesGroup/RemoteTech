using RemoteTech.Collections;
using RemoteTech.Network;
using Unity.Collections;
using Unity.Mathematics;
using Xunit;

namespace RemoteTech.Tests;

public class NetworkUpdateMathTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(4, 6)]
    [InlineData(10, 45)]
    public void PairCount_IsTriangularNumber(int nodeCount, int expected)
    {
        Assert.Equal(expected, NetworkUpdateMath.PairCount(nodeCount));
    }

    [Fact]
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
                    Assert.Equal(i, di);
                    Assert.Equal(j, dj);
                }
            }
            Assert.Equal(NetworkUpdateMath.PairCount(n), k);
        }
    }

    [Fact]
    public void EncodePairIndex_InvertsDecodeAndIsOrderIndependent()
    {
        for (int n = 2; n <= 32; n++)
        {
            int k = 0;
            for (int j = 1; j < n; j++)
            {
                for (int i = 0; i < j; i++, k++)
                {
                    Assert.Equal(k, NetworkUpdateMath.EncodePairIndex(i, j));
                    Assert.Equal(k, NetworkUpdateMath.EncodePairIndex(j, i));
                }
            }
        }
    }

    [Theory]
    [InlineData(100, 100, 200, true)]   // sum exactly meets distance
    [InlineData(100, 99, 200, false)]   // just short
    [InlineData(0, 0, 0, true)]         // coincident
    public void CouldPossiblyLink_ComparesRangeSumToDistance(double rA, double rB, double dist, bool expected)
    {
        Assert.Equal(expected, NetworkUpdateMath.CouldPossiblyLink(rA, rB, dist));
    }

    [Fact]
    public void CheckRange_Standard_TakesMinOfJointAndPerNodeClamps()
    {
        var model = default(StandardRangeModel);
        // joint = min(100,200)=100; clamps wide open -> 100
        Assert.Equal(100.0, NetworkUpdateMath.CheckRange(in model, 100, 1.0, 200, 1.0), 9);
        // r1 clamp bites: min(100, 100*0.5, 200*1) = 50
        Assert.Equal(50.0, NetworkUpdateMath.CheckRange(in model, 100, 0.5, 200, 1.0), 9);
    }

    [Fact]
    public void CheckRange_Additive_AddsSqrtProductThenClamps()
    {
        var model = default(AdditiveRangeModel);
        // joint = min(100,100) + sqrt(100*100) = 200, but capped by r*clamp=100
        Assert.Equal(100.0, NetworkUpdateMath.CheckRange(in model, 100, 1.0, 100, 1.0), 9);
        // wide clamps let the additive joint through
        Assert.Equal(200.0, NetworkUpdateMath.CheckRange(in model, 100, 10.0, 100, 10.0), 9);
    }

    [Theory]
    [InlineData(0.5, 15.0)] // (10+20+30 - 30) * 0.5
    [InlineData(0.0, 0.0)]  // multiplier off -> no bonus
    public void GetMultipleAntennaBonus_SumsNonBestOmni(double multiplier, double expected)
    {
        using var arena = new NativeArena();
        var antennas = arena.Of(
            new JobAntenna { omni = 10 },
            new JobAntenna { omni = 20 },
            new JobAntenna { omni = 30 });
        var range = new IntRange(0, 3);

        double bonus = NetworkUpdateMath.GetMultipleAntennaBonus(antennas, range, maxOmni: 30, multiplier);
        Assert.Equal(expected, bonus, 9);
    }

    // --- Line of sight -----------------------------------------------------

    private static NativeArray<JobBody> SingleBody(NativeArena arena, double3 pos, double radius)
    {
        return arena.Of(new JobBody
        {
            position = pos,
            radius = radius,
            subtree = new IntRange(0, 1),
        });
    }

    [Fact]
    public void HasLineOfSight_BodyDirectlyBetween_Occludes()
    {
        using var arena = new NativeArena();
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        var bodies = SingleBody(arena, new double3(50, 0, 0), radius: 10);
        Assert.False(NetworkUpdateMath.HasLineOfSight(a, b, bodies, new IntRange(0, 1)));
    }

    [Fact]
    public void HasLineOfSight_BodyOffToTheSide_Clear()
    {
        using var arena = new NativeArena();
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        var bodies = SingleBody(arena, new double3(50, 100, 0), radius: 10);
        Assert.True(NetworkUpdateMath.HasLineOfSight(a, b, bodies, new IntRange(0, 1)));
    }

    [Fact]
    public void HasLineOfSight_BodyBehindEndpoint_Ignored()
    {
        using var arena = new NativeArena();
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        // Behind A (negative projection) and beyond B (projection past the segment)
        var behind = SingleBody(arena, new double3(-50, 0, 0), radius: 40);
        var beyond = SingleBody(arena, new double3(150, 0, 0), radius: 40);
        Assert.True(NetworkUpdateMath.HasLineOfSight(a, b, behind, new IntRange(0, 1)));
        Assert.True(NetworkUpdateMath.HasLineOfSight(a, b, beyond, new IntRange(0, 1)));
    }

    [Fact]
    public void HasLineOfSight_GrazingWithinMinHeight_DoesNotOcclude()
    {
        using var arena = new NativeArena();
        var a = new double3(0, 0, 0);
        var b = new double3(100, 0, 0);
        // Surface passes within MIN_HEIGHT (5m) of the segment: radius - MIN_HEIGHT
        // is the effective occlusion radius. Body centered 5m off-axis with radius 10
        // -> threshold 5, lateral 5 -> 25 < 25 is false -> clear.
        var bodies = SingleBody(arena, new double3(50, 5, 0), radius: 10);
        Assert.True(NetworkUpdateMath.HasLineOfSight(a, b, bodies, new IntRange(0, 1)));

        // Drop the off-axis distance below the threshold -> occluded.
        var closer = SingleBody(arena, new double3(50, 4, 0), radius: 10);
        Assert.False(NetworkUpdateMath.HasLineOfSight(a, b, closer, new IntRange(0, 1)));
    }
}
