using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public interface ISatellite
    {
        String Name                    { get; }
        Guid Guid                      { get; }
        Vector3d Position              { get; }
        ICelestialBody Body            { get; }
        bool IsPowered                 { get; }
        bool IsCommandStation          { get; }
        bool HasLocalControl           { get; }
        bool IsVisible                 { get; }
        IEnumerable<IAntenna> Antennas { get; }
        Group Group                    { get; set; }
    }

    public static class SatelliteExtensions
    {
        public static ConnectionMap Connections(this ISatellite satellite)
        {
            return RTCore.Instance.Network[satellite];
        }
    }
}
