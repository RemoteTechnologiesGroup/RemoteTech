using System;
using UnityEngine;

namespace RemoteTech {
    public class QueueFragment : IFragment {
        private readonly VesselSatellite mSatellite;

        private Vector2 mScrollPosition;
        private String mExtraDelay;

        private double Delay {
            get {
                double delay;
                if (!Double.TryParse(mExtraDelay, out delay)) {
                    delay = 0;
                }
                return delay;
            }
            set { mExtraDelay = value.ToString(); }
        }

        public QueueFragment(VesselSatellite vs) {
            mSatellite = vs;
            mExtraDelay = "0";
        }
        public void Draw() {
            GUILayout.BeginVertical(GUILayout.Width(250));
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, 
                    GUILayout.ExpandHeight(true));
                {
                    foreach (String text in mSatellite.FlightComputer) {
                        GUILayout.Label(text, GUI.skin.textField);
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Latency:");
                        GUILayout.Label("Delay:");  
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(mSatellite.Connection.Delay.ToString(), GUI.skin.textField);
                        RTUtil.TextField(ref mExtraDelay);
                    }
                    GUILayout.EndVertical();

                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            mSatellite.FlightComputer.ExtraDelay = Delay;
        }
    }
}