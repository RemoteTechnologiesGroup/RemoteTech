using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTech.SimpleTypes;

namespace RemoteTech.RangeModel
{
    public static class AbstractRangeModel
    {
        /// <summary>
        /// Can't boost the range of an omni antenna by more than this factor, no matter what.
        /// </summary>
        private static readonly double omniClamp = RTSettings.Instance.OmniRangeClampFactor;
        /// <summary>
        /// Can't boost the range of a dish antenna by more than this factor, no matter what.
        /// </summary>
        private static readonly double dishClamp = RTSettings.Instance.DishRangeClampFactor;

        /// <summary>
        /// Finds the maximum range between an antenna and a specific target.
        /// </summary>
        /// <returns>The maximum distance at which the two spacecraft could communicate.</returns>
        /// <param name="antenna">The antenna attempting to target.</param>
        /// <param name="target">The satellite being targeted by <paramref name="antenna"/>.</param>
        /// <param name="antennaSat">The satellite on which <paramref name="antenna"/> is mounted.</param>
        /// <typeparam name="TRange">The range model to evaluate joint ranges with.</typeparam>
        public static double GetRangeInContext<TRange>(IAntenna antenna, ISatellite target, ISatellite antennaSat)
            where TRange : struct, IRangeModel
        {
            TRange model = default;

            // Which antennas on the other craft are capable of communication?
            IEnumerable<IAntenna>  omnisB = GetOmnis(target);
            IEnumerable<IAntenna> dishesB = GetDishesThatSee(target, antennaSat);

            // Pick the best range for each case
            double maxOmniB =  omnisB.Any() ?  omnisB.Max(ant => ant.Omni) : 0.0;
            double maxDishB = dishesB.Any() ? dishesB.Max(ant => ant.Dish) : 0.0;
            double   bonusB = GetMultipleAntennaBonus(omnisB, maxOmniB);

            // Also need local antenna bonus, if antenna is an omni
            IEnumerable<IAntenna> omnisA  = GetOmnis(antennaSat);
            double maxOmniA = omnisA.Any()  ? omnisA.Max( ant => ant.Omni) : 0.0;
            double   bonusA = GetMultipleAntennaBonus(omnisA, maxOmniA);

            // What is the range?
            // Note: IAntenna.Omni and IAntenna.Dish are zero for antennas of the other type
            double maxOmni = Math.Max(CheckRange(model, antenna.Omni + bonusA, omniClamp, maxOmniB + bonusB, omniClamp),
                                      CheckRange(model, antenna.Omni + bonusA, omniClamp, maxDishB         , dishClamp));
            double maxDish = Math.Max(CheckRange(model, antenna.Dish         , dishClamp, maxOmniB + bonusB, omniClamp),
                                      CheckRange(model, antenna.Dish         , dishClamp, maxDishB         , dishClamp));

            if (Double.IsNaN(maxOmni)) { maxOmni = 0.0; }
            if (Double.IsNaN(maxDish)) { maxDish = 0.0; }

            return Math.Max(maxOmni, maxDish);
        }

        /// <summary>
        /// Constructs a link between two satellites, if one is possible.
        /// </summary>
        /// <returns>The new link, or null if the two satellites cannot connect.</returns>
        /// <typeparam name="TRange">The range model to evaluate joint ranges with.</typeparam>
        public static NetworkLink<ISatellite>? GetLink<TRange>(ISatellite satA, ISatellite satB)
            where TRange : struct, IRangeModel {
            return TryFindConnectionCandidates<TRange>(satA, satB, out _, out var type)
                ? new NetworkLink<ISatellite>(satB, type)
                : null;
        }

        /// <summary>The antennas on <paramref name="satA"/> individually capable of reaching
        /// <paramref name="satB"/> — a superset of the one antenna <see cref="GetLink{TRange}"/>
        /// picks. Empty if the two satellites cannot connect.</summary>
        /// <typeparam name="TRange">The range model to evaluate joint ranges with.</typeparam>
        public static List<IAntenna> GetLinkInterfaces<TRange>(ISatellite satA, ISatellite satB)
            where TRange : struct, IRangeModel {
            return TryFindConnectionCandidates<TRange>(satA, satB, out var candidatesA, out _)
                ? candidatesA
                : [];
        }

        private static bool TryFindConnectionCandidates<TRange>(ISatellite satA, ISatellite satB,
            out List<IAntenna> candidatesA, out LinkType type)
            where TRange : struct, IRangeModel {
            TRange model = default;

            // Which antennas on either craft are capable of communication?
            IEnumerable<IAntenna>  omnisA = GetOmnis(satA);
            IEnumerable<IAntenna>  omnisB = GetOmnis(satB);
            IEnumerable<IAntenna> dishesA = GetDishesThatSee(satA, satB);
            IEnumerable<IAntenna> dishesB = GetDishesThatSee(satB, satA);

            // Pick the best range for each case
            double maxOmniA =  omnisA.Any() ?  omnisA.Max(ant => ant.Omni) : 0.0;
            double maxOmniB =  omnisB.Any() ?  omnisB.Max(ant => ant.Omni) : 0.0;
            double maxDishA = dishesA.Any() ? dishesA.Max(ant => ant.Dish) : 0.0;
            double maxDishB = dishesB.Any() ? dishesB.Max(ant => ant.Dish) : 0.0;

            double bonusA = GetMultipleAntennaBonus(omnisA, maxOmniA);
            double bonusB = GetMultipleAntennaBonus(omnisB, maxOmniB);

            double distance = satA.DistanceTo(satB);

            // Which antennas have the range to reach at least one antenna on the other satellite??
            omnisA = omnisA.Where(ant =>
                   CheckRange(model, ant.Omni + bonusA, omniClamp, maxOmniB + bonusB, omniClamp) >= distance
                || CheckRange(model, ant.Omni + bonusA, omniClamp, maxDishB         , dishClamp) >= distance);
            dishesA = dishesA.Where(ant =>
                   CheckRange(model, ant.Dish         , dishClamp, maxOmniB + bonusB, omniClamp) >= distance
                || CheckRange(model, ant.Dish         , dishClamp, maxDishB         , dishClamp) >= distance);
            omnisB = omnisB.Where(ant =>
                   CheckRange(model, ant.Omni + bonusB, omniClamp, maxOmniA + bonusA, omniClamp) >= distance
                || CheckRange(model, ant.Omni + bonusB, omniClamp, maxDishA         , dishClamp) >= distance);
            dishesB = dishesB.Where(ant =>
                   CheckRange(model, ant.Dish         , dishClamp, maxOmniA + bonusA, omniClamp) >= distance
                || CheckRange(model, ant.Dish         , dishClamp, maxDishA         , dishClamp) >= distance);

            // Just because an antenna is in `omnisA.Concat(dishesA)` doesn't mean it can connect to *any*
            //  antenna in `omnisB.Concat(dishesB)`, and vice versa. Pick the max to be safe.
            IAntenna selectedAntennaA = omnisA.Concat(dishesA)
                .OrderByDescending(ant => Math.Max(ant.Omni, ant.Dish)).FirstOrDefault();
            IAntenna selectedAntennaB = omnisB.Concat(dishesB)
                .OrderByDescending(ant => Math.Max(ant.Omni, ant.Dish)).FirstOrDefault();

            if (selectedAntennaA == null || selectedAntennaB == null)
            {
                candidatesA = null;
                type = LinkType.None;
                return false;
            }

            candidatesA = omnisA.Concat(dishesA).ToList();
            type = dishesA.Contains(selectedAntennaA) || dishesB.Contains(selectedAntennaB)
                ? LinkType.Dish : LinkType.Omni;
            return true;
        }

        /// <summary>
        /// Checks the maximum range achievable by two satellites.
        /// </summary>
        /// <returns>The maximum range, including sanity limits.</returns>
        /// <param name="model">The range model to evaluate the joint range with.</param>
        /// <param name="range1">The range of the first satellite.</param>
        /// <param name="range2">The range of the second satellite.</param>
        /// <param name="clamp1">The maximum factor by which the first range can be boosted.</param>
        /// <param name="clamp2">The maximum factor by which the second range can be boosted.</param>
        private static double CheckRange<TRange>(in TRange model,
                double range1, double clamp1,
                double range2, double clamp2)
            where TRange : struct, IRangeModel {
            return Math.Min(Math.Min(model.MaxDistance(range1, range2), range1*clamp1), range2*clamp2);
        }

        /// <summary>
        /// Returns the bonus from having multiple antennas
        /// </summary>
        /// <returns>The boost to all omni antenna ranges, if MultipleAntennaMultiplier is enabled;
        /// otherwise zero.</returns>
        private static double GetMultipleAntennaBonus(IEnumerable<IAntenna> omniList, double maxOmni) {
            if (RTSettings.Instance.MultipleAntennaMultiplier > 0.0) {
                double total = omniList.Sum(a => a.Omni);
                return (total - maxOmni) * RTSettings.Instance.MultipleAntennaMultiplier;
            } else {
                return 0.0;
            }
        }

        /// <summary>
        /// Returns all omnidirectional antennas on a satellite.
        /// </summary>
        /// <returns>A possibly empty collection of omnis.</returns>
        private static IEnumerable<IAntenna> GetOmnis(ISatellite sat)
        {
            return sat.Antennas.Where(a => a.Omni > 0);
        }

        /// <summary>Returns all dishes on a satellite that are pointed, directly or indirectly, 
        /// at a particular target.</summary>
        /// <returns>A possibly empty collection of dishes.</returns>
        /// <param name="sat">The satellite whose dishes are being queried.</param>
        /// <param name="target">The target to be contacted.</param>
        private static IEnumerable<IAntenna> GetDishesThatSee(ISatellite sat, ISatellite target)
        {
            return sat.Antennas.Where(a => a.Dish > 0 
                && (a.IsTargetingDirectly(target) || a.IsTargetingActiveVessel(target) || a.IsInFieldOfView(target, sat)));
        }
    }
}
