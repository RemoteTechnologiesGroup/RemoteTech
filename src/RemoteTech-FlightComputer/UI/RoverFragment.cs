using System;
using RemoteTech.Common.Extensions;
using RemoteTech.Common.Utils;
using UnityEngine;

namespace RemoteTech.UI
{
    public class RoverFragment : IFragment
    {
        private FlightComputer.FlightComputer mFlightComputer;
        private Action mOnClickQueue;

        private bool distDefault = true;

        private float
            mSteering = 0,
            mSteerClamp = 1,
            prevTurn = 0,
            prevDist = 0;

        private string
            mTurn = "",
            mSpeed = "5",
            mDist = "",
            mLatitude = "",
            mLongditude = "",
            Mheading = "";


        public RoverFragment(FlightComputer.FlightComputer fc, Action queue)
        {
            mFlightComputer = fc;
            mOnClickQueue = queue;
        }

        private float Turn
        {
            get
            {
                float turn;
                if (!Single.TryParse(mTurn, out turn)) {
                    turn = 0;
                }
                return turn;
            }
            set { mTurn = value.ToString(); }
        }

        private float Heading
        {
            get
            {
                float heading;
                if (!Single.TryParse(Mheading, out heading)) {
                    heading = 0;
                }
                return heading;
            }
            set { mTurn = value.ToString(); }
        }

        private float Speed
        {
            get
            {
                float speed;
                if (!Single.TryParse(mSpeed, out speed)) {
                    speed = 0;
                }
                return speed;
            }
            set { mSpeed = value.ToString(); }
        }

        private float Dist
        {
            get
            {
                float dist;
                if (!Single.TryParse(mDist, out dist)) {
                    dist = 0;
                }
                return dist;
            }
            set { mDist = value.ToString(); }
        }

        private float Latitude
        {
            get
            {
                float latitude;
                if (!Single.TryParse(mLatitude, out latitude)) {
                    latitude = 0;
                }
                return latitude;
            }
            set { mLatitude = value.ToString(); }
        }

        private float Longitude
        {
            get
            {
                float longitude;
                if (!Single.TryParse(mLongditude, out longitude)) {
                    longitude = 0;
                }
                return longitude;
            }
            set { mLongditude = value.ToString(); }
        }

        private void EnqueueTurn()
        {
            mFlightComputer.Enqueue(DriveCommand.Turn(mSteering, Turn, Speed));
        }
        private void EnqueueDist()
        {
            mFlightComputer.Enqueue(DriveCommand.Distance(Dist, 0, Speed));
        }
        int selected = 0;
        private bool MouseClick = false;
        private readonly string[] Tabs = { "Tgt.", "Hdt.", "Fine" };

        public void Draw()
        {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;

            GUILayout.BeginVertical(GUILayout.Width(156), GUILayout.Height(300));
            {
                selected = GUILayout.Toolbar(selected, Tabs, GUILayout.Width(160 - GUI.skin.button.margin.right * 2.0f));

                switch (selected) {
                    case 0:
                        Target();
                        break;
                    case 1:
                        HDG();
                        break;
                    case 2:
                        Fine();
                        break;
                }

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GuiUtil.Button(new GUIContent(">>", "Toggles the queue and delay functionality."),
                        mOnClickQueue, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void Fine()
        {

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
                GUILayout.Label(new GUIContent("Wheel: ", "how much to turn"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(Math.Abs(mSteering).ToString("P"), "How much to turn"));
                if (mSteering != 0) {
                    if (mSteering < 0)
                        GUILayout.Label(new GUIContent("right", "how much to turn"), GUILayout.Width(40));
                    else
                        GUILayout.Label(new GUIContent("left", "how much to turn"), GUILayout.Width(40));
                } else
                    GUILayout.Label(new GUIContent("", "how much to turn"), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GuiUtil.HorizontalSlider(ref mSteering, 1, -1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Turn", "How many degrees to turn"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                GuiUtil.TextField(ref mTurn, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "How many degrees to turn"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Dist.", "Distance to drive"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                GuiUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m)", "Distance to drive"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep, negative for reverse"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                GuiUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep, negative for reverse"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            mTurn = FormatUtil.ConstrictNum(mTurn, 90);
            mDist = FormatUtil.ConstrictNum(mDist, false);
            mSpeed = FormatUtil.ConstrictNum(mSpeed);


            if (prevTurn != Turn)
                distDefault = false;
            else if (prevDist != Dist)
                distDefault = true;

            prevTurn = Turn;
            prevDist = Dist;

        }

        private void HDG()
        {

            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl().StartsWith("RC"))
                mFlightComputer.Enqueue(DriveCommand.DistanceHeading(Dist, Heading, mSteerClamp, Speed));

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Wheel: ", "How sharp to turn at max"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(mSteerClamp.ToString("P"), "How sharp to turn at max"));
                GUILayout.Label(new GUIContent("max", "How sharp to turn at max"), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GuiUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Hdg.", "Heading to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                GuiUtil.TextField(ref Mheading, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Dist.", "Distance to drive"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                GuiUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                GuiUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            Mheading = FormatUtil.ConstrictNum(Mheading, 360);
            mDist = FormatUtil.ConstrictNum(mDist, false);
            mSpeed = FormatUtil.ConstrictNum(mSpeed, false);
        }

        private void Target()
        {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl().StartsWith("RC"))
                mFlightComputer.Enqueue(DriveCommand.Coord(mSteerClamp, Latitude, Longitude, Speed));
            else if (GameSettings.MODIFIER_KEY.GetKey() && ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) != MouseClick)) {
                MouseClick = Input.GetMouseButton(0) || Input.GetMouseButton(1);
                Vector2 latlon;
                if (MouseClick && CelestialBodyExtension.CBhit(mFlightComputer.Vessel.mainBody, out latlon)) {
                    Latitude = latlon.x;
                    Longitude = latlon.y;

                    if (Input.GetMouseButton(1))
                        mFlightComputer.Enqueue(DriveCommand.Coord(mSteerClamp, Latitude, Longitude, Speed));
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Wheel: ", "How sharp to turn at max"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(mSteerClamp.ToString("P"), "How sharp to turn at max"));
                GUILayout.Label(new GUIContent("max", "How sharp to turn at max"), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GuiUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("LAT.", "Latitude to drive to"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                GuiUtil.TextField(ref mLatitude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "Hold " + GameSettings.MODIFIER_KEY.name + " and click on ground to input coordinates"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("LON.", "Longitude to drive to"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                GuiUtil.TextField(ref mLongditude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "Hold " + GameSettings.MODIFIER_KEY.name + " and click on ground to input coordinates"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                GuiUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();


            mLatitude = FormatUtil.ConstrictNum(mLatitude);
            mLongditude = FormatUtil.ConstrictNum(mLongditude);
            mSpeed = FormatUtil.ConstrictNum(mSpeed, false);
        }

    }
}