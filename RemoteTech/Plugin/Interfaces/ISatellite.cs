using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public interface ISatellite {
        String Name { get; }
        Guid Guid { get; }
        Vector3 Position { get; }
        CelestialBody Body { get; }
        bool Powered { get; }
        float Omni { get; }
        IEnumerable<Dish> Dishes { get; }
    }
}

