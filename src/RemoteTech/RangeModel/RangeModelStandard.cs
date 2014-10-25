using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelStandard
    {
        /// <summary>Finds the maximum distance between two satellites with ranges r1 and r2.</summary>
        /// <returns>The maximum range at which the two satellites can communicate.</returns>
        private static double MaxDistance(double r1, double r2)
        {
            return Math.Min(r1, r2);
        }

        /// <summary>Finds the maximum range between an antenna and a potential target.</summary>
        /// <returns>The maximum distance at which the two spacecraft could communicate.</returns>
        /// <param name="antenna">The antenna attempting to target.</param>
        /// <param name="target">The satellite being targeted by <paramref name="antenna"/>.</param>
        /// <param name="antennaSat">The satellite on which <paramref name="antenna"/> is mounted.</param>
        public static double GetContextRange(IAntenna antenna, ISatellite target, ISatellite antennaSat) {
            return AbstractRangeModel.GetContextRange(antenna, target, antennaSat, MaxDistance);
        }

        /// <summary>Constructs a link between two satellites, if one is possible.</summary>
        /// <returns>The new link, or null if the two satellites cannot connect.</returns>
        public static NetworkLink<ISatellite> GetLink(ISatellite satA, ISatellite satB) {
            return AbstractRangeModel.GetLink(satA, satB, MaxDistance);
        }
    }
}
