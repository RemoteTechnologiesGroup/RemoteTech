using System;
using UnityEngine;

namespace RemoteTech {
    public class RoverFragment : IFragment {
        private FlightComputer mFlightComputer;
        private OnClick mOnClickQueue;

        private bool distDefault = true;

        private float
            mSteering = 0,
            mSteerClamp = 1,
            prevTurn = 0,
            prevDist = 0;

        private string
            mTurn = "",
            mSpeed = "",
            mDist = "",
            mLatitude = "",
            mLongditude = "",
            Mheading = "";


        public RoverFragment(FlightComputer fc, OnClick queue) {
            mFlightComputer = fc;
            mOnClickQueue = queue;
        }

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

        private float Heading {
            get {
                float heading;
                if (!Single.TryParse(Mheading, out heading)) {
                    heading = 0;
                }
                return heading;
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

        private float Latitude {
            get {
                float latitude;
                if (!Single.TryParse(mLatitude, out latitude)) {
                    latitude = 0;
                }
                return latitude;
            }
            set { mLatitude = value.ToString(); }
        }

        private float Longitude {
            get {
                float longitude;
                if (!Single.TryParse(mLongditude, out longitude)) {
                    longitude = 0;
                }
                return longitude;
            }
            set { mLongditude = value.ToString(); }
        }

        private void EnqueueTurn() {
                mFlightComputer.Enqueue(DriveCommand.Turn(mSteering, Turn, Speed));
        }
        private void EnqueueDist() {
                mFlightComputer.Enqueue(DriveCommand.Distance(Dist, 0, Speed));
        }
        int selected = 0;
        private bool MouseClick = false;

        private readonly string[] Tabs = { "Fine", "Hdg.", "Tgt." };

        public void Draw() {

            GUILayout.BeginVertical(GUILayout.Width(156), GUILayout.Height(300));
            {
                selected = GUILayout.Toolbar(selected, Tabs, GUI.skin.textField, GUILayout.Width(156 - GUI.skin.button.margin.right * 2.0f));

                switch (selected) {
                    case 0:
                        Fine();
                        break;
                    case 1:
                        HDG();
                        break;
                    case 2:
                        Target();
                        break;
                }

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    RTUtil.Button("Queue", mOnClickQueue);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void Fine() {

            if (Event.current.Equals(Event.KeyboardEvent("return")) && Speed != 0) {
                if (GUI.GetNameOfFocusedControl() == "RC1" && mSteering != 0 && Turn != 0)
                    EnqueueTurn();
                else if (GUI.GetNameOfFocusedControl() == "RC2" && Dist != 0)
                    EnqueueDist();
                else if (GUI.GetNameOfFocusedControl() == "RC3") {
                    if (!distDefault && mSteering != 0 && Turn != 0)
                        EnqueueTurn();
                    else if (distDefault && Dist != 0)
                        EnqueueDist();
                }
            }


            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Wheel: ");
                GUILayout.FlexibleSpace();
                GUILayout.Label(Math.Abs(mSteering).ToString("P"));
                if (mSteering != 0) {
                    if (mSteering < 0)
                        GUILayout.Label("right", GUILayout.Width(40));
                    else
                        GUILayout.Label("left", GUILayout.Width(40));
                } else
                    GUILayout.Label("", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteering, 1, -1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Turn", GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mTurn, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Dist.", GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Speed", GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m/s)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

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

        private void HDG() {

            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl().StartsWith("RC"))
                mFlightComputer.Enqueue(DriveCommand.DistanceHeading(Dist, Heading, mSteerClamp, Speed));

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Wheel: ");
                GUILayout.FlexibleSpace();
                GUILayout.Label(mSteerClamp.ToString("P"));
                GUILayout.Label("Clamp", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Hdg.", GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref Mheading, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Dist.", GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Speed", GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m/s)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            Mheading = RTUtil.ConstrictNum(Mheading, 360);
            mDist = RTUtil.ConstrictNum(mDist, false);
            mSpeed = RTUtil.ConstrictNum(mSpeed, false);
        }

        private void Target() {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl().StartsWith("RC"))
                mFlightComputer.Enqueue(DriveCommand.Coord(mSteerClamp, Latitude, Longitude, Speed));
            else if (GameSettings.MODIFIER_KEY.GetKey() && ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) != MouseClick)) {
                MouseClick = Input.GetMouseButton(0) || Input.GetMouseButton(1);
                Vector2 latlon;
                if (MouseClick && RTUtil.CBhit(mFlightComputer.mAttachedVessel.mainBody, out latlon)) {
                    Latitude = latlon.x;
                    Longitude = latlon.y;

                    if (Input.GetMouseButton(1))
                        mFlightComputer.Enqueue(DriveCommand.Coord(mSteerClamp, Latitude, Longitude, Speed));
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Wheel: ");
                GUILayout.FlexibleSpace();
                GUILayout.Label(mSteerClamp.ToString("P"));
                GUILayout.Label("Clamp", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("LAT.", GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mLatitude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("LON.", GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mLongditude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Speed", GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m/s)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();


            mLatitude = RTUtil.ConstrictNum(mLatitude);
            mLongditude = RTUtil.ConstrictNum(mLongditude);
            mSpeed = RTUtil.ConstrictNum(mSpeed, false);
        }

    }
}