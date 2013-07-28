using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class DynamicTarget {
        enum TargetTypes {
            NOTARGET,
            BODY,
            ISATELLITE
        }

        TargetTypes type;
        CelestialBody body = null;
        ISatellite sat = null;

        public bool NoTarget {
            get {
                return type == TargetTypes.NOTARGET;
            }
        }
        public Vector3 Position {
            get {
                switch (type) {
                    case TargetTypes.ISATELLITE:
                        return sat.Position;
                    case TargetTypes.NOTARGET:
                        return Vector3.zero;
                    case TargetTypes.BODY:
                        return body.position;
                }
                return Vector3.zero;
            }
        }

        public DynamicTarget(Guid g) {
            if (RTCore.Instance.Network[g] != null) {
                sat = RTCore.Instance.Network[g];
                type = TargetTypes.ISATELLITE;
            } else if (RTCore.Instance.Network.Planets.ContainsKey(g)) {
                body = RTCore.Instance.Network.Planets[g];
                type = TargetTypes.BODY;
            } else {
                type = TargetTypes.NOTARGET;
            }
        }

        public DynamicTarget() {
            type = TargetTypes.NOTARGET;
        }
    }
}
