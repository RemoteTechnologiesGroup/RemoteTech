using System;
using UnityEngine;

namespace RemoteTech.UI
{
    public class FlightComputerWindow : AbstractWindow
    {
        private readonly AttitudeFragment mAttitude;
        private readonly QueueFragment mQueue;
        private bool mQueueEnabled;
        private FlightComputer.FlightComputer mFlightComputer;

        public FlightComputerWindow(FlightComputer.FlightComputer fc)
            : base(Guid.NewGuid(), "Flight Computer", new Rect(100, 100, 0, 0), WindowAlign.Floating)
        {
            mSavePosition = true;
            mFlightComputer = fc;
            mAttitude = new AttitudeFragment(fc, () => mQueueEnabled = !mQueueEnabled);
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
                mAttitude.Draw();
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
    }
}