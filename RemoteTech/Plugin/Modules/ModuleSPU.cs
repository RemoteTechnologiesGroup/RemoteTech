using System;
using System.Collections;
using UnityEngine;

namespace RemoteTech {
    public class ModuleSPU : PartModule, ISignalProcessor {

        public bool Powered { get { return IsPowered; } }
        public Vessel Vessel { get { return vessel; } }


        [KSPField(isPersistant = true)]
        public bool
            IsRTSignalProcessor = true,
            IsPowered = false;

        public override string GetInfo() {
            return "Remote Control";
        }

        [KSPEvent(guiName = "Settings", guiActive = true)]
        public void Settings() {
            (new SatelliteGUIWindow(RTCore.Instance.Satellites.For(vessel))).Show();
        }

        public void FixedUpdate() {
            StartCoroutine(LateFixedUpdate());
        }

        IEnumerator LateFixedUpdate() {
            yield return new WaitForFixedUpdate();
            /*if (vessel == FlightGlobals.ActiveVessel && !RTCore.Instance.Connection.Connection.Exists) {
                part.isControlSource = false;
            }
            IsPowered = part.isControlSource;*/
        }
    }
}



