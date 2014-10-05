using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelRoot
    {
        /// <summary>Constructs a link between two satellites, if one is possible.</summary>
        /// <returns>The new link, or null if the two satellites cannot connect.</returns>
        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b) {
            return AbstractRangeModel.GetLink(sat_a, sat_b, MaxDistance);
        }

        /// <summary>Finds the maximum distance between two satellites with ranges r1 and r2.</summary>
        /// <returns>The maximum range.</returns>
        private static double MaxDistance(double r1, double r2)
        {
            return Math.Min(r1, r2) + Math.Sqrt(r1 * r2);
        }
    }
}
