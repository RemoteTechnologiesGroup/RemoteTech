using System;
using UnityEngine;

namespace RemoteTech.UI
{
    public class FlightComputerWindow : AbstractWindow
    {
        private enum FragmentTab
        {
            Attitude = 0,
            Rover = 1,
        }

        private FragmentTab mTab = FragmentTab.Attitude;
        private readonly AttitudeFragment mAttitude;
        private readonly RoverFragment mRover;
        private readonly QueueFragment mQueue;
        private bool mQueueEnabled;
        private FlightComputer.FlightComputer mFlightComputer;

        private FragmentTab Tab
        {
            get
            {
                return mTab;
            }
            set
            {
                int NumberOfTabs = 2;
                if ((int)value >= NumberOfTabs) {
                    mTab = (FragmentTab)0;
                } else if ((int)value < 0) {
                    mTab = (FragmentTab)(NumberOfTabs - 1);
                } else {
                    mTab = value;
                }
            }
        }

        public FlightComputerWindow(FlightComputer.FlightComputer fc)
            : base(Guid.NewGuid(), "FlightComputer", new Rect(100, 100, 0, 0), WindowAlign.Floating)
        {
            mSavePosition = true;
            mFlightComputer = fc;
            mAttitude = new AttitudeFragment(fc, () => OnQueue());
            mRover = new RoverFragment(fc, () => OnQueue());
            mQueue = new QueueFragment(fc);
            mQueueEnabled = false;
        }

        public override void Show()
        {
            base.Show();
            mFlightComputer.OnActiveCommandAbort += mAttitude.Reset;
            mFlightComputer.OnNewCommandPop += mAttitude.getActiveFlightMode;

            mAttitude.getActiveFlightMode();
        }

        public override void Hide()
        {
            mFlightComputer.OnActiveCommandAbort -= mAttitude.Reset;
            mFlightComputer.OnNewCommandPop -= mAttitude.getActiveFlightMode;
            base.Hide();
        }

        public override void Window(int id)
        {
            GUI.skin = null;
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginHorizontal();
                {
                    switch (mTab) {
                        case FragmentTab.Attitude:
                            mAttitude.Draw();
                            break;
                        case FragmentTab.Rover:
                            mRover.Draw();
                            break;
                    }
                }
                GUILayout.EndHorizontal();

                // RoverComputer
                if (GUI.Button(new Rect(2, 2, 16, 16), "<")) {
                    Tab--;
                }
                if (GUI.Button(new Rect(16, 2, 16, 16), ">")) {
                    Tab++;
                }

                if (mQueueEnabled) {
                    mQueue.Draw();
                } else {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginScrollView(Vector2.zero, GUILayout.ExpandHeight(true));
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndHorizontal();
            base.Window(id);
        }

        private void OnQueue()
        {
            mQueueEnabled = !mQueueEnabled;
            if(mQueueEnabled)
            {
                this.Title = "Flight Computer: " + mFlightComputer.Vessel.vesselName.Substring(0, Math.Min(25, mFlightComputer.Vessel.vesselName.Length));
            }
            else
            {
                this.Title = "Flight Computer";
            }

        }
    }
}
