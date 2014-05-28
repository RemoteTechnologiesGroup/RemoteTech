using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class ConnectionMap : Dictionary<ISatellite, NetworkRoute<ISatellite>>
    {
        public NetworkRoute<ISatellite> ShortestDelay()
        {
            NetworkRoute<ISatellite> min = null;
            foreach (var route in Values)
            {
                if (min == null || route.Cost < min.Cost)
                    min = route;
            }
            return min;
        }

        public bool ConnectedToKSC() {
            return DelayToKSC() != Double.PositiveInfinity;
        }

        public double DelayToKSC()
        {
            var connection = this.Values.FirstOrDefault(r => RTCore.Instance.Network.GroundStations.Values.FirstOrDefault() == r.Start);
            return connection != null ? connection.SignalDelay : Double.PositiveInfinity;
        }

        public bool ConnectedToCommandStation(ISatellite satellite)
        {
            return DelayToCommandStation(satellite) != Double.PositiveInfinity;
        }

        public double DelayToCommandStation(ISatellite satellite)
        {
            var connection = this.Values.FirstOrDefault(r => satellite == r.Start);
            return connection != null ? connection.SignalDelay : Double.PositiveInfinity;
        }
    }
}
