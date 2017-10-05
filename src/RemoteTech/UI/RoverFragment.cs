using System;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;

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
            mHeading = "";


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
                if (!Single.TryParse(mHeading, out heading)) {
                    heading = 0;
                }
                return heading;
            }
            set { mHeading = value.ToString(); }
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

        private int selectedModeIndex = 0;
        private bool MouseClick = false;
        private readonly GUIContent[] Tabs = { new GUIContent("TGT", "Drive to the latitude and longitude of a body."), //TODO: Add ability to drive towards a vessel target
                                               new GUIContent("HDT", "Drive with specific heading and distance."),
                                               new GUIContent("FINE", "Drive with specific turning or distance.") };
        private enum RoverModes { TargetMode = 0,
                                  HeadingMode = 1,
                                  FineMode = 2 };

        public void Draw()
        {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;

            GUILayout.BeginVertical();
            {
                selectedModeIndex = GUILayout.Toolbar(selectedModeIndex, Tabs, GUILayout.Width(width3*3 + GUI.skin.button.margin.right * 2.0f));

                GUILayout.Space(5);

                switch (selectedModeIndex) {
                    case (int) RoverModes.TargetMode:
                        DrawTargetContent();
                        break;
                    case (int) RoverModes.HeadingMode:
                        DrawHDGContent();
                        break;
                    case (int) RoverModes.FineMode:
                        DrawFineContent();
                        break;
                }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent("DRIVE", "Starts the automatic driving."),
                        delegate { OnExecClick(selectedModeIndex); }, GUILayout.Width(width3));
                    GUILayout.FlexibleSpace();
                    RTUtil.Button(new GUIContent(">>", "Toggles the queue and delay functionality."),
                        mOnClickQueue, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void OnExecClick(int modeIndex)
        {
            //FINE
            if (modeIndex == (int)RoverModes.FineMode && Speed != 0)
            {
                if (mSteering != 0 && Turn != 0)
                    EnqueueTurn();
                else if (Dist != 0)
                    EnqueueDist();
                else
                {
                    if (!distDefault && mSteering != 0 && Turn != 0)
                        EnqueueTurn();
                    else if (distDefault && Dist != 0)
                        EnqueueDist();
                }
            }
            //HDG
            else if (modeIndex == (int)RoverModes.HeadingMode)
            {
                mFlightComputer.Enqueue(DriveCommand.DistanceHeading(Dist, Heading, mSteerClamp, Speed));
            }
            //TGT
            else if (modeIndex == (int)RoverModes.TargetMode)
            {
                mFlightComputer.Enqueue(DriveCommand.Coord(mSteerClamp, Latitude, Longitude, Speed));
            }
        }

        private void DrawFineContent()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Wheel: ", "How much to steer"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(Math.Abs(mSteering).ToString("P"), ""));
                if (mSteering != 0) {
                    if (mSteering < 0)
                        GUILayout.Label(new GUIContent("right", ""), GUILayout.Width(40));
                    else
                        GUILayout.Label(new GUIContent("left", ""), GUILayout.Width(40));
                } else
                    GUILayout.Label(new GUIContent("", ""), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteering, 1, -1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Turn", "How many degrees to turn"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mTurn, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "How many degrees to turn"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Dist.", "Distance to drive"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m)", "Distance to drive"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep, negative for reverse"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep, negative for reverse"), GUI.skin.textField, GUILayout.Width(40));
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

        private void DrawHDGContent()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Wheel: ", "How sharp to turn at max"));
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(mSteerClamp.ToString("P"), "How sharp to turn at max"));
                GUILayout.Label(new GUIContent("max", "How sharp to turn at max"), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Hdg.", "Heading to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mHeading, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Dist.", "Distance to drive"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            mHeading = RTUtil.ConstrictNum(mHeading, 360);
            mDist = RTUtil.ConstrictNum(mDist, false);
            mSpeed = RTUtil.ConstrictNum(mSpeed, false);
        }

        private void DrawTargetContent()
        {
            if (GameSettings.MODIFIER_KEY.GetKey() && ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) != MouseClick)) // on lookout for mouse click on body
            {
                MouseClick = Input.GetMouseButton(0) || Input.GetMouseButton(1);
                Vector2 latlon;
                if (MouseClick && RTUtil.CBhit(mFlightComputer.Vessel.mainBody, out latlon))
                {
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

            RTUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("LAT.", "Latitude to drive to"), GUILayout.Width(50));
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mLatitude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "Hold " + GameSettings.MODIFIER_KEY.name + " and click on ground to input coordinates"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("LON.", "Longitude to drive to"), GUILayout.Width(50));
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mLongditude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", "Hold " + GameSettings.MODIFIER_KEY.name + " and click on ground to input coordinates"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent("Speed", "Speed to keep"), GUILayout.Width(50));
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", "Speed to keep"), GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();


            mLatitude = RTUtil.ConstrictNum(mLatitude);
            mLongditude = RTUtil.ConstrictNum(mLongditude);
            mSpeed = RTUtil.ConstrictNum(mSpeed, false);
        }

    }
}