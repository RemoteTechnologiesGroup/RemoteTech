using System;

namespace RemoteTech.RangeModel
{
    /// <summary>
    /// Picks the maximum communication distance between two antennas given
    /// their individual ranges. A stateless struct rather than a delegate, so
    /// <see cref="AbstractRangeModel"/>'s generic methods dispatch to it
    /// without allocating a closure per call.
    /// </summary>
    public interface IRangeModel
    {
        /// <summary>
        /// Finds the maximum distance between two satellites with ranges r1 and r2.
        /// </summary>
        double MaxDistance(double r1, double r2);
    }

    /// <summary>
    /// The stock KSP range model: the smaller of the two ranges.
    /// </summary>
    public readonly struct StandardRangeModel : IRangeModel
    {
        public double MaxDistance(double r1, double r2) => Math.Min(r1, r2);
    }

    /// <summary>
    /// NathanKell's additive range model.
    /// </summary>
    public readonly struct AdditiveRangeModel : IRangeModel
    {
        public double MaxDistance(double r1, double r2) => Math.Min(r1, r2) + Math.Sqrt(r1 * r2);
    }
}
