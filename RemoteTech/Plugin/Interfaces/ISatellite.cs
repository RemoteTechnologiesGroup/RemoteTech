using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public interface ISatellite {
        bool Active { get; }
        bool Visible { get; }

        String Name { get; }
        Guid Guid { get; }
        Vector3 Position { get; }
        CelestialBody Body { get; }

        bool LocalControl { get; }

        float Omni { get; }
        IEnumerable<Dish> Dishes { get; }
    }
}
