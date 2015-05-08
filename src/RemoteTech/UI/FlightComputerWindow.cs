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
            mAttitude = new AttitudeFragment(fc, () => mQueueEnabled = !mQueueEnabled);
            mRover = new RoverFragment(fc, () => mQueueEnabled = !mQueueEnabled);
            mQueue = new QueueFragment(fc);
            mQueueEnabled = false;
        }

        public override void Show()
        {
            base.Show();
            mFlightComputer.onActiveCommandAbort += mAttitude.Reset;
            mFlightComputer.onNewCommandPop += mAttitude.getActiveFlightMode;

            mAttitude.getActiveFlightMode();
        }

        public override void Hide()
        {
            mFlightComputer.onActiveCommandAbort -= mAttitude.Reset;
            mFlightComputer.onNewCommandPop -= mAttitude.getActiveFlightMode;
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

                // Disabled RoverComputer
                // We will add this feature on a later release
                //
                //if (GUI.Button(new Rect(2, 2, 16, 16), "<")) {
                //    Tab--;
                //}
                //if (GUI.Button(new Rect(16, 2, 16, 16), ">")) {
                //    Tab++;
                //}

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
    }
}