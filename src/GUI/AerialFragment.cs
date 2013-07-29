using System;
using UnityEngine;

namespace RemoteTech {
    public class AerialFragment : IFragment {

        private String mAltitude = "5000";

        private float Altitude {
            get {
                float altitude;
                if (!Single.TryParse(mAltitude, out altitude)) {
                    altitude = 0;
                }
                return altitude;
            }
            set { mAltitude = value.ToString(); }
        }

        private readonly FlightComputer mFlightComputer;

        public AerialFragment(FlightComputer fc) {
            mFlightComputer = fc;
        }

        public void Draw() {
            if (Event.current.Equals(Event.KeyboardEvent("return")) &&
                    GUI.GetNameOfFocusedControl() == "alt") {
                Submit();
            }
            GUILayout.BeginVertical(GUILayout.Width(156), GUILayout.Height(300));
            {
                GUILayout.Label("Altitude: ");
                GUI.SetNextControlName("alt");
                RTUtil.TextField(ref mAltitude);
            }
            GUILayout.EndVertical();
        }

        private void Submit() {
            if (!mFlightComputer.InputAllowed)
                return;
            var newCommand = AttitudeCommand.WithAltitude(Altitude);
            mFlightComputer.Enqueue(newCommand);
        }
    }
}