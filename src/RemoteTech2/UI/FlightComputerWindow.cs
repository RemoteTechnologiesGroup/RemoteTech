using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
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
        private readonly FlightComputer mFlightComputer;

        private FragmentTab Tab
        {
            get
            {
                return mTab;
            }
            set
            {
                int NumberOfTabs = 2;
                if ((int)value >= NumberOfTabs)
                {
                    mTab = (FragmentTab)0;
                }
                else if ((int)value < 0)
                {
                    mTab = (FragmentTab)(NumberOfTabs - 1);
                }
                else
                {
                    mTab = value;
                }
            }
        }

        public FlightComputerWindow(FlightComputer fc)
            : base(Guid.NewGuid(), "Flight Computer", new Rect(100, 100, 0, 0), WindowAlign.Floating)
        {
            mFlightComputer = fc;

            mAttitude = new AttitudeFragment(fc, () => mQueueEnabled = !mQueueEnabled);
            mRover = new RoverFragment(fc, () => mQueueEnabled = !mQueueEnabled);
            mQueue = new QueueFragment(fc);
            mQueueEnabled = false;
        }

        public override void Window(int id)
        {
            GUI.skin = null;
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginHorizontal();
                {
                    switch (mTab)
                    {
                        case FragmentTab.Attitude:
                            mAttitude.Draw();
                            break;
                        case FragmentTab.Rover:
                            mRover.Draw();
                            break;
                    }
                }
                GUILayout.EndHorizontal();
                if (GUI.Button(new Rect(2, 2, 16, 16), "<"))
                {
                    Tab--;
                }
                if (GUI.Button(new Rect(20, 2, 16, 16), ">"))
                {
                    Tab++;
                }

                if (mQueueEnabled)
                {
                    mQueue.Draw();
                }
                else
                {
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

        public override void Draw()
        {
            switch (mTab)
            {
                case FragmentTab.Attitude:
                    Title = "Flight Computer";
                    break;
                case FragmentTab.Rover:
                    Title = "Rover Computer";
                    break;
            }
            base.Draw();
        }
    }
}