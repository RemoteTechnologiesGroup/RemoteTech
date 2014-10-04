using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public interface ISatellite
    {
        bool Visible { get; }
        String Name { get; set; }
        Guid Guid { get; }
        Vector3d Position { get; }
        CelestialBody Body { get; }

        bool Powered { get; }
        bool IsCommandStation { get; }
        bool HasLocalControl { get; }
        /// <summary>
        /// Indicates whether the ISatellite corresponds to a vessel
        /// </summary>
        /// <value><c>true</c> if satellite is vessel or asteroid; otherwise (e.g. a ground station), <c>false</c>.</value>
        bool isVessel { get; }
        /// <summary>
        /// The vessel hosting the ISatellite, if one exists.
        /// </summary>
        /// <value>The vessel corresponding to this ISatellite. Returns null if !isVessel.</value>
        Vessel parentVessel { get; }

        IEnumerable<IAntenna> Antennas { get; }

        void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes);
    }
}
