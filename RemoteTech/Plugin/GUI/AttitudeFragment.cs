using System;
using UnityEngine;

namespace RemoteTech {
    public class AttitudeFragment : IFragment {
        private static String[] MODE_TEXT = new String[] {
            "OFF", "KILL", "NOD",
            "ORB", "SRF",  "TGT",
        };

        private static String[] ATTITUDE_TEXT = new String[] {
            "GRD\n+", "RAD\n+", "NRM\n+",
            "GRD\n-", "RAD\n-", "NRM\n-",
        };

        private float Pitch {
            get {
                float pitch;
                if (!Single.TryParse(mPitch, out pitch)) {
                    pitch = 0;
                }
                return pitch;
            }
            set { mPitch = value.ToString(); }
        }

        private float Heading {
            get {
                float heading;
                if (!Single.TryParse(mHeading, out heading)) {
                    heading = 0;
                }
                return heading;
            }
            set { mHeading = value.ToString(); }
        }

        private float Roll {
            get {
                float roll;
                if (!Single.TryParse(mRoll, out roll)) {
                    roll = 0;
                }
                return roll;
            }
            set { mRoll = value.ToString(); }
        }

        private double Duration {
            get {
                TimeSpan duration;
                if (!RTUtil.TryParseDuration(mDuration, out duration)) {
                    duration = new TimeSpan();
                }
                return duration.TotalSeconds;
            }
            set { mDuration = RTUtil.FormatDuration(value); }
        }

        private VesselSatellite mSatellite;
        private OnClick mOnClickQueue;

        private int mModeState;
        private int mAttitudeState;
        private float mThrottleState;

        private String mPitch;
        private String mRoll;
        private bool mRollEnabled;
        private String mHeading;
        private String mDuration;

        public AttitudeFragment(VesselSatellite vs, OnClick queue) {
            mSatellite = vs;
            mOnClickQueue = queue;
            mPitch = "90";
            mRoll = "0";
            mHeading = "90";
            mDuration = "0";
        }

        public void Draw() {
            GUILayout.BeginVertical(GUILayout.Width(150));
            {
                RTUtil.GroupButton(3, MODE_TEXT, ref mModeState);
                RTUtil.StateButton("SURFACE", mModeState == 6, (state) => mModeState = 6);
                GUILayout.Space(5);
                RTUtil.GroupButton(3, ATTITUDE_TEXT, ref mAttitudeState);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("PIT: ");
                    RTUtil.Button("+", () => Pitch++, GUILayout.Width(20));
                    RTUtil.Button("-", () => Pitch--, GUILayout.Width(20));
                    RTUtil.TextField(ref mPitch, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("HDG: ");
                    RTUtil.Button("+", () => Heading++, GUILayout.Width(20));
                    RTUtil.Button("-", () => Heading--, GUILayout.Width(20));
                    RTUtil.TextField(ref mHeading, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("RLL: ");
                    RTUtil.Button("+", () => Roll++, GUILayout.Width(20));
                    RTUtil.Button("-", () => Roll--, GUILayout.Width(20));
                    RTUtil.TextField(ref mRoll, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Throttle: ");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(mThrottleState.ToString("P"));
                }
                GUILayout.EndHorizontal();

                RTUtil.HorizontalSlider(ref mThrottleState, 0, 1);
                RTUtil.TextField(ref mDuration);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("SEND", OnClickUpdate);
                    GUILayout.FlexibleSpace();
                    RTUtil.Button("Q", mOnClickQueue);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        void OnClickUpdate() {
            if (!mSatellite.Connection.Exists) return;
            FlightCommand fc = new FlightCommand(RTUtil.GetGameTime() + 
                                                                mSatellite.Connection.Delay);
            switch (mAttitudeState) {
                default:
                    fc.Attitude = FlightAttitude.Prograde;
                    break;
                case 1:
                    fc.Attitude = FlightAttitude.RadialPlus;
                    break;
                case 2:
                    fc.Attitude = FlightAttitude.NormalPlus;
                    break;
                case 3:
                    fc.Attitude = FlightAttitude.Retrograde;
                    break;
                case 4:
                    fc.Attitude = FlightAttitude.RadialMinus;
                    break;
                case 5:
                    fc.Attitude = FlightAttitude.NormalMinus;
                    break;
            }

            switch (mModeState) {
                default:
                    fc.Mode = FlightMode.Off;
                    fc.Frame = ReferenceFrame.World;
                    break;
                case 1:
                    fc.Mode = FlightMode.KillRot;
                    fc.Frame = ReferenceFrame.World;
                    break;
                case 2:
                    fc.Mode = FlightMode.AttitudeHold;
                    fc.Frame = ReferenceFrame.Maneuver;
                    break;
                case 3:
                    fc.Mode = FlightMode.AttitudeHold;
                    fc.Frame = ReferenceFrame.Orbit;
                    break;
                case 4:
                    fc.Mode = FlightMode.AttitudeHold;
                    fc.Frame = ReferenceFrame.Surface;
                    break;
                case 5:
                    fc.Mode = FlightMode.AttitudeHold;
                    fc.Frame = ReferenceFrame.Target;
                    break;
                case 6:
                    fc.Mode = FlightMode.AttitudeHold;
                    fc.Frame = ReferenceFrame.North;
                    fc.Attitude = FlightAttitude.Surface;
                    break;
            }

            fc.Direction = new Vector3(Pitch, Heading, Roll);
                             
            fc.Throttle = mThrottleState;
            fc.Duration = Duration;
            mSatellite.FlightComputer.Enqueue(fc);
        }
    }
}