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
        /// Gets the type of the vessel, if one exists. Undefined behavior if <c>!isVessel</c>.
        /// </summary>
        /// <value>The type of the vessel corresponding to this ISatellite.</value>
        VesselType getType { get; }

        IEnumerable<IAntenna> Antennas { get; }

        void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes);
    }
}
