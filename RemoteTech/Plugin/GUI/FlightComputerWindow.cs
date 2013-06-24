using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class FlightComputerWindow : AbstractWindow {
        private enum Tab {
            Attitude = 0,
            Rover    = 1,
            Aerial   = 2,
            Progcom  = 3,
        }

        private Tab mTab;
        private const int NUMBER_OF_TABS = 3;
        private AttitudeFragment mAttitude;
        private RoverFragment mRover;
        private AerialFragment mAerial;
        private ProgcomFragment mProgcom;

        private QueueFragment mQueue;
        private bool mQueueEnabled;

        private VesselSatellite mSatellite;

        public FlightComputerWindow(VesselSatellite vs)
        : base("Flight Computer", new Rect(100, 100, 0, 0)) {
            mSatellite = vs;

            mAttitude = new AttitudeFragment(vs, OnClickQueue);
            mRover = new RoverFragment();
            mAerial = new AerialFragment();
            mProgcom = new ProgcomFragment();

            mQueue = new QueueFragment(vs);
            mQueueEnabled = false;
        }

        public override void Window(int id) {
            base.Window(id);
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(150));
                    {
                        RTUtil.Button("<", () => {
                            mTab = mTab == 0
                                   ? (Tab) NUMBER_OF_TABS - 1
                                   : (Tab) (((int) mTab - 1)%NUMBER_OF_TABS);
                        }, GUILayout.ExpandWidth(false));
                        GUILayout.Label("Mode: " + mTab.ToString(), GUILayout.ExpandWidth(true));
                        RTUtil.Button(">", () => {
                            mTab = (Tab) (((int) mTab + 1)%NUMBER_OF_TABS);
                        }, GUILayout.ExpandWidth(false));
                    }
                    GUILayout.EndHorizontal();
                    switch (mTab) {
                        case Tab.Attitude:
                            mAttitude.Draw();
                            break;
                        case Tab.Rover:
                            mRover.Draw();
                            break;
                        case Tab.Aerial:
                            mAerial.Draw();
                            break;
                        case Tab.Progcom:
                            mProgcom.Draw();
                            break;
                    }
                }
                GUILayout.EndVertical();
                if (mQueueEnabled)
                    mQueue.Draw();
            }
            GUILayout.EndHorizontal();
        }

        void OnClickQueue() {
            mQueueEnabled = !mQueueEnabled;
            mWindowPosition.width = 0;
        }

        public override void Show() {
            base.Show();
        }

        public override void Hide() {
            base.Hide();
        }

    }
}
