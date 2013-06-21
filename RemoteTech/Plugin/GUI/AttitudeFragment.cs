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
            mRoll = "90";
            mHeading = "90";
            mDuration = "0";
        }

        public void Draw() {
            GUILayout.BeginVertical();
            {
                RTUtil.GroupButton(3, MODE_TEXT, ref mModeState);
                GUILayout.Space(5);
                RTUtil.GroupButton(3, ATTITUDE_TEXT, ref mAttitudeState);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Pitch: ");
                    RTUtil.Button("+", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.Button("-", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.TextField(ref mPitch, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Head: ");
                    RTUtil.Button("+", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.Button("-", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.TextField(ref mHeading, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Roll: ");
                    RTUtil.Button("+", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.Button("-", () => { }, GUILayout.ExpandWidth(false));
                    RTUtil.TextField(ref mRoll, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Throttle: ");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(mThrottleState.ToString("F1") + "%");
                }
                GUILayout.EndHorizontal();
                RTUtil.HorizontalSlider(ref mThrottleState, 0, 100);
                RTUtil.TextField(ref mDuration);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("UPD", OnClickUpdate);
                    GUILayout.FlexibleSpace();
                    RTUtil.Button("Q", mOnClickQueue);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        void OnClickUpdate() {
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
                    fc.Attitude = FlightAttitude.Prograde;
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
            }

            float pitch;
            if (!Single.TryParse(mPitch, out pitch)) {
                pitch = 0;
            }
            mPitch = pitch.ToString();

            float heading;
            if (!Single.TryParse(mHeading, out heading)) {
                heading = 0;
            }
            mHeading = heading.ToString();

            float roll;
            if (!Single.TryParse(mRoll, out roll)) {
                roll = 0;
            }
            mRoll = roll.ToString();

            fc.Direction = new Vector3(pitch, heading, roll);

            mSatellite.FlightComputer.Enqueue(fc);
        }
    }
}