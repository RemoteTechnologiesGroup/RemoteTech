using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class FlightComputerWindow : AbstractWindow {
        private enum FragmentTab {
            Attitude = 0,
            Aerial   = 1,
            Rover    = 2,
            Bifrost  = 3,
        }

        private FragmentTab mTab;
        private readonly AttitudeFragment mAttitude;
        private readonly RoverFragment mRover;
        private readonly AerialFragment mAerial;
        private readonly BifrostFragment mBifrost;
        private readonly QueueFragment mQueue;
        private bool mQueueEnabled;
        private readonly FlightComputer mFlightComputer;

        private FragmentTab Tab {
            get {
                return mTab;
            }
            set {
                int NumberOfTabs = 4;
                if ((int)value >= NumberOfTabs) {
                    mTab = (FragmentTab) 0;
                } else if ((int) value < 0) {
                    mTab = (FragmentTab)(NumberOfTabs - 1);
                } else {
                    mTab = value;
                }
            }
        }

        public FlightComputerWindow(FlightComputer fc)
            : base("", new Rect(100, 100, 0, 0), WindowAlign.Floating) {
            mFlightComputer = fc;

            mAttitude = new AttitudeFragment(fc, () => mQueueEnabled = !mQueueEnabled);
            mRover = new RoverFragment(fc, () => mQueueEnabled = !mQueueEnabled);
            mAerial = new AerialFragment(fc);
            mBifrost = new BifrostFragment(fc);
            mQueue = new QueueFragment(fc);
            mQueueEnabled = false;
        }

        public override void Window(int id) {
            GUILayout.BeginHorizontal();
            {
                switch (mTab) {
                    case FragmentTab.Attitude:
                        mAttitude.Draw();
                        break;
                    case FragmentTab.Rover:
                        mRover.Draw();
                        break;
                    case FragmentTab.Aerial:
                        mAerial.Draw();
                        break;
                    case FragmentTab.Bifrost:
                        mBifrost.Draw();
                        break;
                }
                if (mQueueEnabled) {
                    mQueue.Draw();
                }
            }
            GUILayout.EndHorizontal();
            if (GUI.Button(new Rect(2, 2, 16, 16), "<")) {
                Tab--;
            }
            if (GUI.Button(new Rect(20, 2, 16, 16), ">")) {
                Tab++;
            }
            base.Window(id);
        }

        protected override void Draw() {
            switch (mTab) {
                case FragmentTab.Attitude:
                    Title = "Attitude";
                    break;
                case FragmentTab.Rover:
                    Title = "Rover";
                    break;
                case FragmentTab.Aerial:
                    Title = "Aerial";
                    break;
                case FragmentTab.Bifrost:
                    Title = "Bifrost";
                    break;
            }
            base.Draw();
        }

        public override void Show() {
            base.Show();
        }

        public override void Hide() {
            base.Hide();
        }
    }
}
