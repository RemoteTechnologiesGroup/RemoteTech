using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class VesselProvider : IVesselProvider, IDisposable
    {
        public event Action<IVessel> VesselCreated = delegate { };
        public event Action<IVessel> VesselDestroyed = delegate { };
        public IEnumerable<IVessel> Vessels
        {
            get { return FlightGlobals.Vessels.Select(v => (IVessel)(VesselProxy) v); }
        }

        public IVessel ActiveVessel
        {
            get
            {
                return (VesselProxy) FlightGlobals.ActiveVessel ?? SelectedVessel;
            }
        }
        public IVessel SelectedVessel
        {
            get
            {
                return (VesselProxy) PlanetariumCamera.fetch.target.vessel;
            }
            set
            {
                // XXX: Hack
                var newTarget = PlanetariumCamera.fetch.targets.FirstOrDefault(t => t != null && t.gameObject.name == value.Name);
                if (newTarget == null)
                {
                    var vessel = value;
                    var scaledMovement = new GameObject().AddComponent<ScaledMovement>();
                    scaledMovement.tgtRef = ((Vessel)vessel).transform;
                    scaledMovement.name = vessel.Name;
                    scaledMovement.transform.parent = ScaledSpace.Instance.transform;
                    scaledMovement.vessel = ((Vessel)vessel);
                    scaledMovement.type = MapObject.MapObjectType.VESSEL;
                    newTarget = scaledMovement;
                    PlanetariumCamera.fetch.SetTarget(PlanetariumCamera.fetch.AddTarget(newTarget));
                    PlanetariumCamera.fetch.targets.Remove(newTarget);
                }
                else
                {
                    PlanetariumCamera.fetch.SetTarget(PlanetariumCamera.fetch.AddTarget(newTarget));
                }      
            }
        }

        public VesselProvider()
        {
            GameEvents.onVesselCreate.Add(OnVesselCreate);
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        public void Dispose()
        {
            GameEvents.onVesselCreate.Remove(OnVesselCreate);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
        }

        private void OnVesselCreate(Vessel vessel)
        {
            VesselCreated((VesselProxy) vessel);
        }

        private void OnVesselDestroy(Vessel vessel)
        {
            VesselDestroyed((VesselProxy) vessel);
        }

        public IEnumerator<IVessel> GetEnumerator()
        {
            return Vessels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Vessels.GetEnumerator();
        }
    }
}
