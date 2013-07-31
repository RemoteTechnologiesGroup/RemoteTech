using System;
using UnityEngine;

namespace RemoteTech {
    public class RoverFragment : IFragment {
        private FlightComputer mFlightComputer;
        private OnClick mOnClickQueue;

        private bool distDefault = true;

        private float 
            mSteering = 0,
            prevTurn = 0,
            prevDist = 0;
        private string
            mTurn = "",
            mSpeed = "",
            mDist = "";
        private float Turn {
            get {
                float turn;
                if (!Single.TryParse(mTurn, out turn)) {
                    turn = 0;
                }
                return turn;
            }
            set { mTurn = value.ToString(); }
        }

        private float Speed {
            get {
                float speed;
                if (!Single.TryParse(mSpeed, out speed)) {
                    speed = 0;
                }
                return speed;
            }
            set { mSpeed = value.ToString(); }
        }

        private float Dist {
            get {
                float dist;
                if (!Single.TryParse(mDist, out dist)) {
                    dist = 0;
                }
                return dist;
            }
            set { mDist = value.ToString(); }
        }

        public RoverFragment(FlightComputer fc, OnClick queue) {
            mFlightComputer = fc;
            mOnClickQueue = queue;
        }


        private void EnqueueTurn() {
            Turn = Mathf.Clamp(Turn, 0, 90);
            mFlightComputer.Enqueue(DriveCommand.On(mSteering, Turn, Speed));
        }
        private void EnqueueDist() {
            mFlightComputer.Enqueue(DriveCommand.On(0, Dist, Speed));
        }


        public void Draw() {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && Speed != 0) {
                if (GUI.GetNameOfFocusedControl() == "RTRN" && mSteering != 0 && Turn != 0)
                    EnqueueTurn();
                else if (GUI.GetNameOfFocusedControl() == "LDST" && Dist != 0)
                    EnqueueDist();
                else if (GUI.GetNameOfFocusedControl() == "LSPD") {
                    if (!distDefault && mSteering != 0 && Turn != 0)
                        EnqueueTurn();
                    else if (distDefault && Dist != 0)
                        EnqueueDist();
                }
            }

            GUILayout.BeginVertical(GUILayout.Width(156), GUILayout.Height(300));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Wheel: ");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(Math.Abs(mSteering).ToString("P"));
                    if (mSteering != 0) {
                        if (mSteering < 0)
                            GUILayout.Label("right");
                        else
                            GUILayout.Label("left");
                    }
                }
                GUILayout.EndHorizontal();

                RTUtil.HorizontalSlider(ref mSteering, 1, -1);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Turn", GUILayout.Width(50));
                    GUI.SetNextControlName("RTRN");
                    RTUtil.TextField(ref mTurn, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                    GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Dist.", GUILayout.Width(50));
                    GUI.SetNextControlName("LDST");
                    RTUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                    GUILayout.Label("(m)", GUI.skin.textField, GUILayout.Width(40));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Speed", GUILayout.Width(50));
                    GUI.SetNextControlName("LSPD");
                    RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                    GUILayout.Label("(m/s)", GUI.skin.textField, GUILayout.Width(40));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("Queue", mOnClickQueue);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            mTurn = RTUtil.ConstrictNum(mTurn, 90);
            mDist = RTUtil.ConstrictNum(mDist, false);
            mSpeed = RTUtil.ConstrictNum(mSpeed);


            if (prevTurn != Turn)
                distDefault = false;
            else if (prevDist != Dist)
                distDefault = true;

            prevTurn = Turn;
            prevDist = Dist;
        }
    }
}