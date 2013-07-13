using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public interface ISatellite {
        bool Powered { get; }
        bool Visible { get; }

        String Name { get; }
        Guid Guid { get; }
        Vector3 Position { get; }
        CelestialBody Body { get; }

        float Omni { get; }
        IEnumerable<Dish> Dishes { get; }

        bool CommandStation { get; }
    }
}
