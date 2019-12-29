using System;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;
using KSP.Localization;

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
        private readonly GUIContent[] Tabs = { new GUIContent(Localizer.Format("#RT_RoverFragment_TGT"), Localizer.Format("#RT_RoverFragment_TGT_desc")),//"TGT", "Drive to the latitude and longitude of a body or towards vessel target."
                                               new GUIContent(Localizer.Format("#RT_RoverFragment_HDG"), Localizer.Format("#RT_RoverFragment_HDG_desc")),//"HDG", "Drive with specific heading and distance."
                                               new GUIContent(Localizer.Format("#RT_RoverFragment_FINE"), Localizer.Format("#RT_RoverFragment_FINE_desc")) };//"FINE", "Drive with specific turning or distance."
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
                    RTUtil.Button(new GUIContent(Localizer.Format("#RT_RoverFragment_DRIVE"), Localizer.Format("#RT_RoverFragment_DRIVE_desc")),//"DRIVE", "Starts the automatic driving."
                        delegate { OnExecClick(selectedModeIndex); }, GUILayout.Width(width3));
                    GUILayout.FlexibleSpace();
                    RTUtil.Button(new GUIContent(">>", Localizer.Format("#RT_RoverFragment_Queue_desc")),//"Toggles the queue and delay functionality."
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
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_FINESteer"), Localizer.Format("#RT_RoverFragment_FINESteer_desc")));//"Steer: ", "How much to turn"
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(Math.Abs(mSteering).ToString("P"), ""));
                if (mSteering != 0) {
                    if (mSteering < 0)
                        GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_right"), ""), GUILayout.Width(40));//"right"
                    else
                        GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_left"),  ""), GUILayout.Width(40));//"left"
                } else
                    GUILayout.Label(new GUIContent("", ""), GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteering, 1, -1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_Turn"), Localizer.Format("#RT_RoverFragment_Turn_desc")), GUILayout.Width(50));//"Turn", "How many degrees to turn"
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mTurn, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", Localizer.Format("#RT_RoverFragment_Turn_desc")), GUI.skin.textField, GUILayout.Width(40));//, "How many degrees to turn"
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_Dist"), Localizer.Format("#RT_RoverFragment_Dist_desc")),GUILayout.Width(50));//"Dist", "Distance to drive"
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m)", Localizer.Format("#RT_RoverFragment_Dist_desc")), GUI.skin.textField, GUILayout.Width(40));//"Distance to drive"
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_Speed"), Localizer.Format("#RT_RoverFragment_Speed_desc")), GUILayout.Width(50));//"Speed", "Speed to maintain, negative for reverse"
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", Localizer.Format("#RT_RoverFragment_Speed_desc")), GUI.skin.textField, GUILayout.Width(40));//"Speed to maintain, negative for reverse"
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
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_HDGSteer"), Localizer.Format("#RT_RoverFragment_HDGSteer_desc")));//"Steer: ", "How sharp to turn at max"
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(mSteerClamp.ToString("P"), Localizer.Format("#RT_RoverFragment_HDGSteer_desc")));//"How sharp to turn at max"
                GUILayout.Label(new GUIContent("max",  Localizer.Format("#RT_RoverFragment_HDGSteer_desc")), GUILayout.Width(40));//"How sharp to turn at max"
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_mHdg"), Localizer.Format("#RT_RoverFragment_mHdg_desc")), GUILayout.Width(50));//"Hdg", "Heading to maintain"
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mHeading, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(°)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_Dist"), Localizer.Format("#RT_RoverFragment_Dist_desc")), GUILayout.Width(50));//"Dist", "Distance to drive"
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mDist, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label("(m)", GUI.skin.textField, GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_Speed"), Localizer.Format("#RT_RoverFragment_HDGSpeed_desc")), GUILayout.Width(50));//"Speed", "Speed to maintain"
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", Localizer.Format("")), GUI.skin.textField, GUILayout.Width(40));//"Speed to maintain"
            }
            GUILayout.EndHorizontal();

            mHeading = RTUtil.ConstrictNum(mHeading, 360);
            mDist = RTUtil.ConstrictNum(mDist, false);
            mSpeed = RTUtil.ConstrictNum(mSpeed, false);
        }

        private void DrawTargetContent()
        {
            string targetTypeString = Localizer.Format("#RT_RoverFragment_coordinations");//"Body coordinations"
            string tooltip = Localizer.Format("#RT_RoverFragment_coordinations_desc", GameSettings.MODIFIER_KEY.name);//"Hold " +  + " and click on ground to input coordinates"
            ITargetable Target = mFlightComputer.Vessel.targetObject;

            if (GameSettings.MODIFIER_KEY.GetKey() && ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) != MouseClick)) // on lookout for mouse click on body
            {
                MouseClick = Input.GetMouseButton(0) || Input.GetMouseButton(1);
                Vector2 latlon;
                if (MouseClick && RTUtil.CBhit(mFlightComputer.Vessel.mainBody, out latlon))
                {
                    Latitude = latlon.x;
                    Longitude = latlon.y;
                }
            }
            else if (Target != null) // only if target is vessel not world coord
            {
                if (Target.GetType().ToString().Equals("Vessel"))
                {
                    Vessel TargetVessel = Target as Vessel;
                    Latitude = (float) TargetVessel.latitude;
                    Longitude = (float) TargetVessel.longitude;
                    targetTypeString = Localizer.Format("#RT_RoverFragment_TargetVessel");//"Designated Vessel"
                    tooltip = Localizer.Format("#RT_RoverFragment_TargetVessel_desc");//"Drive to this vessel"
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_HDGSteer"), Localizer.Format("#RT_RoverFragment_HDGSteer_desc")));//"Steer: ", "How sharp to turn at max"
                GUILayout.FlexibleSpace();
                GUILayout.Label(new GUIContent(mSteerClamp.ToString("P"), Localizer.Format("#RT_RoverFragment_HDGSteer_desc")));//"How sharp to turn at max"
                GUILayout.Label(new GUIContent("max", Localizer.Format("#RT_RoverFragment_HDGSteer_desc")), GUILayout.Width(40));// "How sharp to turn at max"
            }
            GUILayout.EndHorizontal();

            RTUtil.HorizontalSlider(ref mSteerClamp, 0, 1);

            GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_ModeLabel",targetTypeString), tooltip));//"Mode: "

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_LAT"), Localizer.Format("#RT_RoverFragment_LAT_desc")), GUILayout.Width(50));//"LAT", "Latitude to drive to"
                GUI.SetNextControlName("RC1");
                RTUtil.TextField(ref mLatitude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", Localizer.Format("#RT_RoverFragment_coordinations_desc", GameSettings.MODIFIER_KEY.name)), GUI.skin.textField, GUILayout.Width(40));//"Hold " +  + " and click on ground to input coordinates"
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_LON"), Localizer.Format("#RT_RoverFragment_LON_desc")), GUILayout.Width(50));//"LON", "Longitude to drive to"
                GUI.SetNextControlName("RC2");
                RTUtil.TextField(ref mLongditude, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(°)", Localizer.Format("#RT_RoverFragment_coordinations_desc", GameSettings.MODIFIER_KEY.name)), GUI.skin.textField, GUILayout.Width(40));//"Hold " +  + " and click on ground to input coordinates"
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(new GUIContent(Localizer.Format("#RT_RoverFragment_Speed"), Localizer.Format("#RT_RoverFragment_HDGSpeed_desc")), GUILayout.Width(50));//"Speed", "Speed to maintain"
                GUI.SetNextControlName("RC3");
                RTUtil.TextField(ref mSpeed, GUILayout.Width(50), GUILayout.ExpandWidth(false));
                GUILayout.Label(new GUIContent("(m/s)", Localizer.Format("#RT_RoverFragment_HDGSpeed_desc")), GUI.skin.textField, GUILayout.Width(40));//"Speed to maintain"
            }
            GUILayout.EndHorizontal();


            mLatitude = RTUtil.ConstrictNum(mLatitude);
            mLongditude = RTUtil.ConstrictNum(mLongditude);
            mSpeed = RTUtil.ConstrictNum(mSpeed, false);
        }

    }
}