using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelRoot
    {
        public const double OmniRangeClamp = 100.0;
        public const double DishRangeClamp = 1000.0;

        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b)
        {
            var omni_a = sat_a.Antennas.Where(a => a.Omni > 0);
            var omni_b = sat_b.Antennas.Where(b => b.Omni > 0);
            var dish_a = sat_a.Antennas.Where(a => a.Dish > 0 && (a.IsTargetingDirectly(sat_b) || a.IsTargetingActiveVessel(sat_b) || a.IsTargetingPlanet(sat_b, sat_a)));
            var dish_b = sat_b.Antennas.Where(b => b.Dish > 0 && (b.IsTargetingDirectly(sat_a) || b.IsTargetingActiveVessel(sat_a) || b.IsTargetingPlanet(sat_a, sat_b)));

            double max_omni_a = omni_a.Any() ? omni_a.Max(a => a.Omni) : 0.0;
            double max_omni_b = omni_b.Any() ? omni_b.Max(b => b.Omni) : 0.0;
            double max_dish_a = dish_a.Any() ? dish_a.Max(a => a.Dish) : 0.0;
            double max_dish_b = dish_b.Any() ? dish_b.Max(b => b.Dish) : 0.0;
            double bonus_a = 0.0;
            double bonus_b = 0.0;

            if (RTSettings.Instance.MultipleAntennaMultiplier > 0.0)
            {
                double sum_omni_a = omni_a.Sum(a => a.Omni);
                double sum_omni_b = omni_b.Sum(b => b.Omni);

                bonus_a = (sum_omni_a - max_omni_a) * RTSettings.Instance.MultipleAntennaMultiplier;
                bonus_b = (sum_omni_b - max_omni_b) * RTSettings.Instance.MultipleAntennaMultiplier;
            }

            double max_a = Math.Max(max_omni_a + bonus_a, max_dish_a);
            double max_b = Math.Max(max_omni_b + bonus_b, max_dish_b);
            double distance = sat_a.DistanceTo(sat_b);

            omni_a = omni_a.Where(a => MaxDistance(a.Omni + bonus_a, max_b, OmniRangeClamp) >= distance);
            dish_a = dish_a.Where(a => MaxDistance(a.Dish, max_omni_b + bonus_b, OmniRangeClamp) >= distance || MaxDistance(a.Dish, max_dish_b, DishRangeClamp) >= distance);
            omni_b = omni_b.Where(b => MaxDistance(b.Omni + bonus_b, max_a, OmniRangeClamp) >= distance);
            dish_b = dish_b.Where(b => MaxDistance(b.Dish, max_omni_a + bonus_a, OmniRangeClamp) >= distance || MaxDistance(b.Dish, max_dish_a, DishRangeClamp) >= distance);

            var conn_a = omni_a.Concat(dish_a).FirstOrDefault();
            var conn_b = omni_b.Concat(dish_b).FirstOrDefault();

            if (conn_a != null && conn_b != null)
            {
                var interfaces = omni_a.Concat(dish_a).ToList();
                var type = LinkType.Omni;
                if (dish_a.Contains(conn_a) || dish_b.Contains(conn_b)) type = LinkType.Dish;
                return new NetworkLink<ISatellite>(sat_b, interfaces, type);
            }

            return null;
        }

        public static double MaxDistance(double min, double max, double clamp)
        {
            double m = Math.Min(min, max);
            return Math.Min(clamp * m, m + Math.Sqrt(min * max));
        }
    }
}
