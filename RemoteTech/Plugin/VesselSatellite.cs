using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class VesselSatellite : ISatellite, IDisposable {
        public ISignalProcessor SignalProcessor { get; set; }

        public Vessel Vessel { get { return SignalProcessor.Vessel; } }

        public bool Powered { get { return SignalProcessor.Powered; } }

        public bool Visible { get { return MapViewFiltering.CheckAgainstFilter(SignalProcessor.Vessel); } }

        public String Name { get { return SignalProcessor.Name; } }

        public Guid Guid { get { return SignalProcessor.Guid; } }

        public Vector3 Position { get { return SignalProcessor.Position; } }

        public CelestialBody Body { get { return SignalProcessor.Body; } }

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
                    if (a.CanTarget && !a.DishTarget.Equals(Guid.Empty)) yield return new Dish(a.DishTarget, a.DishFactor, a.DishRange);
                }
            }
        }
        public bool CommandStation { get { return SignalProcessor.CommandStation; } }
        public bool LocalControl { get { return SignalProcessor.LocalControl; } }
        public FlightComputer FlightComputer { get; private set; }
        public Path<ISatellite> Connection { get; set; }

        public VesselSatellite(ISignalProcessor parent) {
            SignalProcessor = parent;
            FlightComputer = new FlightComputer(this);
            Connection = Path.Empty((ISatellite) this);
            RTCore.Instance.Network.ConnectionUpdated += OnConnectionUpdate;
        }

        public void OnConnectionUpdate(Path<ISatellite> path) {
            if (path.Start == this) Connection = path;
        }

        public void Dispose() {
            FlightComputer.Dispose();
            RTCore.Instance.Network.ConnectionUpdated -= OnConnectionUpdate;
        }

        public override string ToString() {
            return SignalProcessor.Vessel.vesselName;
        }
    }
}
