using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public interface ISatellite {

        String Name { get; }
        Guid Guid { get; }
        Vector3 Position { get; }
        ISignalProcessor SignalProcessor { get; set; }
        bool Powered { get; }

        float OmniRange { get; }
        IEnumerable<Pair<Guid, float>> DishRange { get; }
    }
}

