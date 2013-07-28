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
            GUILayout.BeginVertical();
            {
                GUILayout.Label("Altitude: ");
                GUI.SetNextControlName("alt");
                if (Event.current.type == EventType.KeyDown &&
                        Event.current.keyCode == KeyCode.Return &&
                            GUI.GetNameOfFocusedControl() == "alt") {
                    Submit();
                }
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