using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class FlightComputerWindow : AbstractWindow
    {
        private readonly AttitudeFragment mAttitude;
        private readonly QueueFragment mQueue;
        private bool mQueueEnabled;

        public FlightComputerWindow(FlightComputer fc)
            : base(Guid.NewGuid(), "Flight Computer", new Rect(100, 100, 0, 0), WindowAlign.Floating)
        {
            mSavePosition = true;
            mAttitude = new AttitudeFragment(fc, () => mQueueEnabled = !mQueueEnabled);
            mQueue = new QueueFragment(fc);
            mQueueEnabled = false;
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