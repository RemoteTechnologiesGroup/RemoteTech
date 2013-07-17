using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class VesselSatellite : ISatellite, IDisposable {
        public bool Powered {
            get {
                return mSignalProcessors.Any(s => s.Powered);
            }
        }

        public bool Visible {
            get {
                return MapViewFiltering.CheckAgainstFilter(Vessel);
            }
        }

        public String Name {
            get {
                return Vessel.vesselName;
            }
        }

        public Guid Guid {
            get {
                return mSignalProcessors[0].Guid;
            }
        }

        public Vector3 Position {
            get {
                return Vessel.GetWorldPos3D();
            }
        }

        public CelestialBody Body {
            get {
                return Vessel.mainBody;
            }
        }

        public float Omni {
            get {
                return RTCore.Instance.Antennas.For(Guid).Any()
                       ? RTCore.Instance.Antennas.For(Guid).Max(a => a.OmniRange)
                       : 0.0f;
            }
        }

        public IEnumerable<Dish> Dishes {
            get {
                foreach (IAntenna a in RTCore.Instance.Antennas.For(this)) {
                    if (a.CanTarget && !a.DishTarget.Equals(Guid.Empty)) {
                        yield return new Dish(a.DishTarget, a.DishFactor, a.DishRange);
                    }
                }
            }
        }

        public Vessel Vessel {
            get {
                return mSignalProcessors[0].Vessel;
            }
        }

        public bool CommandStation {
            get {
                return mSignalProcessors.Any(s => s.CommandStation);
            }
        }

        public bool LocalControl {
            get {
                return Vessel.GetVesselCrew().Count > 0;
            }
        }

        public FlightComputer FlightComputer {
            get {
                return mSignalProcessors[0].FlightComputer;
            }
        }

        public Path<ISatellite> Connection { get; set; }

        private List<ISignalProcessor> mSignalProcessors; 

        public VesselSatellite(List<ISignalProcessor> parts) {
            mSignalProcessors = parts;
            Connection = Path.Empty<ISatellite>(this);
            RTCore.Instance.Network.ConnectionUpdated += OnConnectionUpdate;
        }

        public void OnConnectionUpdate(Path<ISatellite> path) {
            if (path.Start == this) Connection = path;
        }

        public void Dispose() {
            RTCore.Instance.Network.ConnectionUpdated -= OnConnectionUpdate;
        }

        public override String ToString() {
            return Vessel.vesselName;
        }

        public override int GetHashCode() {
            return Guid.GetHashCode();
        }
    }
}
