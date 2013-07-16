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

        public DynamicTarget(CelestialBody b) {
            body = b;
            type = TargetTypes.BODY;
        }
        public DynamicTarget(ISatellite s) {
            sat = s;
            type = TargetTypes.ISATELLITE;
        }

        public DynamicTarget() {
            type = TargetTypes.NOTARGET;
        }

    }
}
