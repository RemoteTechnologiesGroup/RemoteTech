using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelStandard
    {
        public static NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b)
        {
            double distance = sat_a.DistanceTo(sat_b);
            var omni_a = sat_a.Antennas.Where(a => a.Omni > 0);
            var omni_b = sat_b.Antennas.Where(b => b.Omni > 0);
            var dish_a = sat_a.Antennas.Where(a => a.Dish > distance && (a.IsTargetingDirectly(sat_b) || a.IsTargetingActiveVessel(sat_b) || a.IsTargetingPlanet(sat_b, sat_a)));
            var dish_b = sat_b.Antennas.Where(b => b.Dish > distance && (b.IsTargetingDirectly(sat_a) || b.IsTargetingActiveVessel(sat_a) || b.IsTargetingPlanet(sat_a, sat_b)));

            double bonus_a = 0;
            double bonus_b = 0;

            if (RTSettings.Instance.MultipleAntennaMultiplier > 0.0)
            {
                double max_omni_a = omni_a.Any() ? omni_a.Max(a => a.Omni) : 0.0;
                double max_omni_b = omni_b.Any() ? omni_b.Max(b => b.Omni) : 0.0;
                double sum_omni_a = omni_a.Sum(a => a.Omni);
                double sum_omni_b = omni_b.Sum(b => b.Omni);

                bonus_a = (sum_omni_a - max_omni_a) * RTSettings.Instance.MultipleAntennaMultiplier;
                bonus_b = (sum_omni_b - max_omni_b) * RTSettings.Instance.MultipleAntennaMultiplier;
            }

            omni_a = omni_a.Where(a => a.Omni + bonus_a >= distance);
            omni_b = omni_b.Where(b => b.Omni + bonus_b >= distance);

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
    }
}
