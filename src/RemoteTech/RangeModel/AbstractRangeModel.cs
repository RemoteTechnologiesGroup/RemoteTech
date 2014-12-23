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

        /// <summary>Finds the maximum range between an antenna and a specific target.</summary>
        /// <returns>The maximum distance at which the two spacecraft could communicate.</returns>
        /// <param name="ant_a">The antenna attempting to target.</param>
        /// <param name="sat_a">The satellite on which <paramref name="ant_a"/> is mounted.</param>
        /// <param name="sat_b">The satellite being targeted by <paramref name="ant_a"/>.</param>
        /// <param name="rangeFunc">A function that computes the maximum range between two 
        /// satellites, given their individual ranges.</param>
        public static double GetContextRange(IAntenna ant_a, ISatellite sat_a, ISatellite sat_b, 
            Func<double, double, double> rangeFunc) {
            // Which antennas on the other craft are capable of communication?
            IEnumerable<IAntenna> omni_a = getOmnis(sat_a);
            IEnumerable<IAntenna> omni_b = getOmnis(sat_b);
            IEnumerable<IAntenna> dish_b = getDishesThatSee(sat_b, sat_a);

            // Pick the best range for each case
            double max_omni_a = omni_a.Any() ? omni_a.Max(a => a.Omni) : 0.0;
            double max_omni_b = omni_b.Any() ? omni_b.Max(b => b.Omni) : 0.0;
            double max_dish_b = dish_b.Any() ? dish_b.Max(b => b.Dish) : 0.0;

            double bonus_a = getMultipleAntennaBonus(omni_a, max_omni_a);
            double bonus_b = getMultipleAntennaBonus(omni_b, max_omni_b);

            // What is the range?
            // Note: IAntenna.Omni and IAntenna.Dish are zero for antennas of the other type
            double max_omni = Math.Max(checkRange(rangeFunc, ant_a.Omni + bonus_a, OmniClamp, max_omni_b + bonus_b, OmniClamp),
                                       checkRange(rangeFunc, ant_a.Omni + bonus_a, OmniClamp, max_dish_b          , DishClamp));
            double max_dish = Math.Max(checkRange(rangeFunc, ant_a.Dish          , DishClamp, max_omni_b + bonus_b, OmniClamp), 
                                       checkRange(rangeFunc, ant_a.Dish          , DishClamp, max_dish_b          , DishClamp));

            return Math.Max(max_omni, max_dish);
        }

        /// <summary>Constructs a link between two satellites, if one is possible.</summary>
        /// <returns>The new link, or null if the two satellites cannot connect.</returns>
        /// <param name="rangeFunc">A function that computes the maximum range between two 
        /// satellites, given their individual ranges.</param>
        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b, 
            Func<double, double, double> rangeFunc) {
            // Which antennas on either craft are capable of communication?
            IEnumerable<IAntenna> omni_a = getOmnis(sat_a);
            IEnumerable<IAntenna> omni_b = getOmnis(sat_b);
            IEnumerable<IAntenna> dish_a = getDishesThatSee(sat_a, sat_b);
            IEnumerable<IAntenna> dish_b = getDishesThatSee(sat_b, sat_a);

            // Pick the best range for each case
            double max_omni_a = omni_a.Any() ? omni_a.Max(a => a.Omni) : 0.0;
            double max_omni_b = omni_b.Any() ? omni_b.Max(b => b.Omni) : 0.0;
            double max_dish_a = dish_a.Any() ? dish_a.Max(a => a.Dish) : 0.0;
            double max_dish_b = dish_b.Any() ? dish_b.Max(b => b.Dish) : 0.0;

            double bonus_a = getMultipleAntennaBonus(omni_a, max_omni_a);
            double bonus_b = getMultipleAntennaBonus(omni_b, max_omni_b);

            double distance = sat_a.DistanceTo(sat_b);

            // Which antennas are in range?
            omni_a = omni_a.Where(a => 
                   checkRange(rangeFunc, a.Omni + bonus_a, OmniClamp, max_omni_b + bonus_b, OmniClamp) >= distance
                || checkRange(rangeFunc, a.Omni + bonus_a, OmniClamp, max_dish_b          , DishClamp) >= distance);
            dish_a = dish_a.Where(a => 
                   checkRange(rangeFunc, a.Dish          , DishClamp, max_omni_b + bonus_b, OmniClamp) >= distance 
                || checkRange(rangeFunc, a.Dish          , DishClamp, max_dish_b          , DishClamp) >= distance);
            omni_b = omni_b.Where(b => 
                   checkRange(rangeFunc, b.Omni + bonus_b, OmniClamp, max_omni_a + bonus_a, OmniClamp) >= distance
                || checkRange(rangeFunc, b.Omni + bonus_b, OmniClamp, max_dish_a          , DishClamp) >= distance);
            dish_b = dish_b.Where(b => 
                   checkRange(rangeFunc, b.Dish          , DishClamp, max_omni_a + bonus_a, OmniClamp) >= distance 
                || checkRange(rangeFunc, b.Dish          , DishClamp, max_dish_a          , DishClamp) >= distance);

            // BUG: there's no guarantee that `conn_a` and `conn_b` can connect to each 
            //  other, except in RangeModelStandard
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

        /// <summary>Returns the bonus from having multiple antennas</summary>
        /// <returns>The boost to all omni antenna ranges, if MultipleAntennaMultiplier is enabled;
        /// otherwise zero.</returns>
        private static double getMultipleAntennaBonus(IEnumerable<IAntenna> omniList, double max_omni) {
            if (RTSettings.Instance.MultipleAntennaMultiplier > 0.0) {
                double total = omniList.Sum(a => a.Omni);
                return (total - max_omni) * RTSettings.Instance.MultipleAntennaMultiplier;
            } else {
                return 0.0;
            }
        }

        /// <summary>Returns all omnidirectional antennas on a satellite.</summary>
        /// <returns>A possibly empty collection of omnis.</returns>
        private static IEnumerable<IAntenna> getOmnis(ISatellite sat)
        {
            return sat.Antennas.Where(a => a.Omni > 0);
        }

        /// <summary>Returns all dishes on a satellite that are pointed, directly or indirectly, 
        /// at a particular target.</summary>
        /// <returns>A possibly empty collection of dishes.</returns>
        /// <param name="sat">The satellite whose dishes are being queried.</param>
        /// <param name="target">The target to be contacted.</param>
        private static IEnumerable<IAntenna> getDishesThatSee(ISatellite sat, ISatellite target)
        {
            return sat.Antennas.Where(a => a.Dish > 0 
                && (a.IsTargetingDirectly(target) || a.IsTargetingActiveVessel(target) || a.IsTargetingPlanet(target, sat)));
        }
    }
}
