using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelStandard
    {
        /// <summary>Finds the maximum distance between two satellites with ranges r1 and r2.</summary>
        /// <returns>The maximum range.</returns>
        private static double MaxDistance(double r1, double r2)
        {
            return Math.Min(r1, r2);
        }

        /// <summary>Finds the maximum range between an antenna and a potential target.</summary>
        /// <returns>The maximum distance at which the two spacecraft could communicate.</returns>
        /// <param name="ant_a">The antenna attempting to target.</param>
        /// <param name="sat_a">The satellite on which <paramref name="ant_a"/> is mounted.</param>
        /// <param name="sat_b">The satellite being targeted by <paramref name="ant_a"/>.</param>
        public static double GetContextRange(IAntenna ant_a, ISatellite sat_a, ISatellite sat_b) {
            return AbstractRangeModel.GetContextRange(ant_a, sat_a, sat_b, MaxDistance);
        }

        /// <summary>Constructs a link between two satellites, if one is possible.</summary>
        /// <returns>The new link, or null if the two satellites cannot connect.</returns>
        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b) {
            return AbstractRangeModel.GetLink(sat_a, sat_b, MaxDistance);
        }
    }
}
