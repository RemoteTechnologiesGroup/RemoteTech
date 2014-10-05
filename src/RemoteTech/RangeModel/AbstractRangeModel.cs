using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class AbstractRangeModel
    {
        /// <summary>Can't boost the range of an omni antenna by more than this factor, no matter what.</summary>
        public const double OmniClamp = 100.0;
        /// <summary>Can't boost the range of a dish antenna by more than this factor, no matter what.</summary>
        public const double DishClamp = 1000.0;

        /// <summary>Constructs a link between two satellites, if one is possible.</summary>
        /// <returns>The new link, or null if the two satellites cannot connect.</returns>
        /// <param name="rangeFunc">A function that computes the maximum range between two 
        /// satellites, given their individual ranges.</param>
        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b, 
            Func<double, double, double> rangeFunc) {
            // Which antennas on either craft are capable of communication?
            IEnumerable<IAntenna> omni_a = sat_a.Antennas.Where(a => a.Omni > 0);
            IEnumerable<IAntenna> omni_b = sat_b.Antennas.Where(b => b.Omni > 0);
            IEnumerable<IAntenna> dish_a = sat_a.Antennas.Where(a => a.Dish > 0 
                && (a.IsTargetingDirectly(sat_b) || a.IsTargetingActiveVessel(sat_b) || a.IsTargetingPlanet(sat_b, sat_a)));
            IEnumerable<IAntenna> dish_b = sat_b.Antennas.Where(b => b.Dish > 0 
                && (b.IsTargetingDirectly(sat_a) || b.IsTargetingActiveVessel(sat_a) || b.IsTargetingPlanet(sat_a, sat_b)));

            // Pick the best range for each case
            double max_omni_a = omni_a.Any() ? omni_a.Max(a => a.Omni) : 0.0;
            double max_omni_b = omni_b.Any() ? omni_b.Max(b => b.Omni) : 0.0;
            double max_dish_a = dish_a.Any() ? dish_a.Max(a => a.Dish) : 0.0;
            double max_dish_b = dish_b.Any() ? dish_b.Max(b => b.Dish) : 0.0;
            double bonus_a = 0.0;
            double bonus_b = 0.0;

            // Boost omni ranges
            if (RTSettings.Instance.MultipleAntennaMultiplier > 0.0)
            {
                double sum_omni_a = omni_a.Sum(a => a.Omni);
                double sum_omni_b = omni_b.Sum(b => b.Omni);

                bonus_a = (sum_omni_a - max_omni_a) * RTSettings.Instance.MultipleAntennaMultiplier;
                bonus_b = (sum_omni_b - max_omni_b) * RTSettings.Instance.MultipleAntennaMultiplier;
            }

            // Pick the best range on each vessel
            double max_a = Math.Max(max_omni_a + bonus_a, max_dish_a);
            double max_b = Math.Max(max_omni_b + bonus_b, max_dish_b);
            double distance = sat_a.DistanceTo(sat_b);

            // Which antennas are in range?
            omni_a = omni_a.Where(a => 
                   checkRange(rangeFunc, a.Omni + bonus_a, OmniClamp, max_omni_b + bonus_b, OmniClamp) >= distance
                || checkRange(rangeFunc, a.Omni + bonus_a, OmniClamp, max_dish_b          , DishClamp) >= distance);
            dish_a = dish_a.Where(a => 
                   checkRange(rangeFunc, a.Dish, DishClamp, max_omni_b + bonus_b, OmniClamp) >= distance 
                || checkRange(rangeFunc, a.Dish, DishClamp, max_dish_b          , DishClamp) >= distance);
            omni_b = omni_b.Where(b => 
                   checkRange(rangeFunc, b.Omni + bonus_b, OmniClamp, max_omni_a + bonus_a, OmniClamp) >= distance
                || checkRange(rangeFunc, b.Omni + bonus_b, OmniClamp, max_dish_a          , DishClamp) >= distance);
            dish_b = dish_b.Where(b => 
                   checkRange(rangeFunc, b.Dish, DishClamp, max_omni_a + bonus_a, OmniClamp) >= distance 
                || checkRange(rangeFunc, b.Dish, DishClamp, max_dish_a          , DishClamp) >= distance);

            // Pick an antenna, pick any antenna...
            IAntenna conn_a = omni_a.Concat(dish_a).FirstOrDefault();
            IAntenna conn_b = omni_b.Concat(dish_b).FirstOrDefault();

            if (conn_a != null && conn_b != null)
            {
                var interfaces = omni_a.Concat(dish_a).ToList();
                var type = LinkType.Omni;
                if (dish_a.Contains(conn_a) || dish_b.Contains(conn_b)) type = LinkType.Dish;
                return new NetworkLink<ISatellite>(sat_b, interfaces, type);
            }

            return null;
        }

        /// <summary>Checks the maximum range achievable by two satellites.</summary>
        /// <returns>The maximum range, including sanity limits.</returns>
        /// <param name="rangeFunc">A function that takes two antenna ranges and returns a joint range.</param>
        /// <param name="range1">The range of the first satellite.</param>
        /// <param name="range2">The range of the second satellite.</param>
        /// <param name="clamp1">The maximum factor by which the first range can be boosted.</param>
        /// <param name="clamp2">The maximum factor by which the second range can be boosted.</param>
        private static double checkRange(Func<double, double, double> rangeFunc, 
                double range1, double clamp1, 
                double range2, double clamp2) {
            return Math.Min(Math.Min(rangeFunc.Invoke(range1, range2), range1*clamp1), range2*clamp2);
        }
    }
}
