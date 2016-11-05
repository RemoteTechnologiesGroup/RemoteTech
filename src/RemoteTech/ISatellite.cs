using System;
using System.Collections.Generic;
using UnityEngine;
using RemoteTech.SimpleTypes;

namespace RemoteTech
{
    public interface ISatellite
    {
        /// <summary>Gets whether or not a satellite if visible in the Tracking station or the Flight Map view.</summary>
        bool Visible { get; }
        /// <summary>Gets or sets the name of the satellite.</summary>
        string Name { get; set; }
        /// <summary>Gets the satellite id.</summary>
        Guid Guid { get; }
        /// <summary>Gets a double precision vector for the satellite's world space position.</summary>
        Vector3d Position { get; }
        /// <summary>Gets the celestial body around which the satellite is orbiting.</summary>
        CelestialBody Body { get; }
        /// <summary>Gets the color of the ground station mark in Tracking station or Flight map view.</summary>
        Color MarkColor { get; }
        /// <summary>Gets if the satellite is actually powered or not.</summary>
        bool Powered { get; }
        /// <summary>Gets if the satellite is a RemoteTech command station.</summary>
        bool IsCommandStation { get; }
        /// <summary>Gets whether the satellite has local control or not (that is, if it is locally controlled or not).</summary>
        bool HasLocalControl { get; }
        /// <summary>Indicates whether the ISatellite corresponds to a vessel</summary>
        /// <value><c>true</c> if satellite is vessel or asteroid; otherwise (e.g. a ground station), <c>false</c>.</value>
        bool isVessel { get; }
        /// <summary>The vessel hosting the ISatellite, if one exists.</summary>
        /// <value>The vessel corresponding to this ISatellite. Returns null if !isVessel.</value>
        Vessel parentVessel { get; }
        /// <summary>Gets a list of antennas for this satellite.</summary>
        IEnumerable<IAntenna> Antennas { get; }
        /// <summary>Called on connection refresh to update the connections.</summary>
        /// <param name="routes">List of network routes.</param>
        void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes);
    }
}
