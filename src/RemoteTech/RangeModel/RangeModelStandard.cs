using System.Collections.Generic;
using RemoteTech.SimpleTypes;

namespace RemoteTech.RangeModel
{
    public static class RangeModelStandard
    {
        /// <summary>
        /// Finds the maximum range between an antenna and a potential target.
        /// </summary>
        /// <returns>The maximum distance at which the two spacecraft could communicate.</returns>
        /// <param name="antenna">The antenna attempting to target.</param>
        /// <param name="target">The satellite being targeted by <paramref name="antenna"/>.</param>
        /// <param name="antennaSat">The satellite on which <paramref name="antenna"/> is mounted.</param>
        public static double GetRangeInContext(IAntenna antenna, ISatellite target, ISatellite antennaSat) {
            return AbstractRangeModel.GetRangeInContext<StandardRangeModel>(antenna, target, antennaSat);
        }

        /// <summary>
        /// Constructs a link between two satellites, if one is possible.
        /// </summary>
        /// <returns>The new link, or null if the two satellites cannot connect.</returns>
        public static NetworkLink<ISatellite>? GetLink(ISatellite satA, ISatellite satB) {
            return AbstractRangeModel.GetLink<StandardRangeModel>(satA, satB);
        }

        /// <summary>
        /// The antennas on <paramref name="satA"/> individually capable of reaching <paramref name="satB"/>.
        /// </summary>
        public static List<IAntenna> GetLinkInterfaces(ISatellite satA, ISatellite satB) {
            return AbstractRangeModel.GetLinkInterfaces<StandardRangeModel>(satA, satB);
        }
    }
}
