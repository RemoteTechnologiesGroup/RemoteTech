using System;
using UnityEngine;

namespace RemoteTech {
    public class AttitudeFragment : IFragment {
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

        private FlightAttitude Attitude {
            get {
                switch (mAttitude) {
                    default:
                        return FlightAttitude.Prograde;
                    case 2:
                        return FlightAttitude.RadialPlus;
                    case 3:
                        return FlightAttitude.NormalPlus;
                    case 4:
                        return FlightAttitude.Retrograde;
                    case 5:
                        return FlightAttitude.RadialMinus;
                    case 6:
                        return FlightAttitude.NormalMinus;
                }
            }
        }

        private FlightComputer mFlightComputer;
        private OnClick mOnClickQueue;

        private int mMode;
        private int mAttitude;
        private float mThrottle;

        private String mPitch = "90";
        private String mRoll = "0";
        private String mHeading = "90";
        private String mDuration = "0s";
        private bool mRollEnabled = false;

        public AttitudeFragment(FlightComputer fc, OnClick queue) {
            mFlightComputer = fc;
            mOnClickQueue = queue;
        }

        public void Draw() {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    RTUtil.StateButton("KILL", mMode, 1, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("NODE", mMode, 2, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("SURF", mMode, 3, OnModeClick, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.StateButton("ORB", mMode, 4, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("SRF", mMode, 5, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("TGT", mMode, 6, OnModeClick, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.StateButton("GRD\n+", mAttitude, 1, OnAttitudeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("RAD\n+", mAttitude, 2, OnAttitudeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("NRM\n+", mAttitude, 3, OnAttitudeClick, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.StateButton("GRD\n-", mAttitude, 4, OnAttitudeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("RAD\n-", mAttitude, 5, OnAttitudeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("NRM\n-", mAttitude, 6, OnAttitudeClick, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("PIT: ", GUILayout.Width(50));
                    RTUtil.Button("+", () => Pitch++, GUILayout.Width(20));
                    RTUtil.Button("-", () => Pitch--, GUILayout.Width(20));
                    RTUtil.TextField(ref mPitch, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("HDG: ", GUILayout.Width(50));
                    RTUtil.Button("+", () => Heading++, GUILayout.Width(20));
                    RTUtil.Button("-", () => Heading--, GUILayout.Width(20));
                    RTUtil.TextField(ref mHeading, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("RLL: ", GUILayout.Width(50));
                    RTUtil.Button("+", () => Roll++, GUILayout.Width(20));
                    RTUtil.Button("-", () => Roll--, GUILayout.Width(20));
                    RTUtil.TextField(ref mRoll, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Throttle: ");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(mThrottle.ToString("P"));
                }
                GUILayout.EndHorizontal();

                RTUtil.HorizontalSlider(ref mThrottle, 0, 1);
                RTUtil.TextField(ref mDuration);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("Burn", OnBurnClick);
                    GUILayout.FlexibleSpace();
                    RTUtil.Button("Queue", mOnClickQueue);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void OnModeClick(int state) {
            if (!mFlightComputer.InputAllowed)
                return;
            mMode = (state < 0) ? 0 : state;
            SendAttitudeCommand();
        }

        private void OnAttitudeClick(int state) {
            if (!mFlightComputer.InputAllowed)
                return;
            mAttitude = (state < 0) ? 0 : state;
            if (mMode < 4) {
                mMode = 4;
            }
            SendAttitudeCommand();
        }

        private void SendAttitudeCommand() {
            DelayedCommand newCommand;
            switch (mMode) {
                default: // Off
                    mAttitude = 0;
                    newCommand = AttitudeCommand.Off();
                    break;
                case 1: // Killrot
                    mAttitude = 0;
                    newCommand = AttitudeCommand.KillRot();
                    break;
                case 2: // Node
                    mAttitude = 0;
                    newCommand = AttitudeCommand.ManeuverNode();
                    break;
                case 3: // Pitch, heading, roll
                    mAttitude = 0;
                    newCommand = AttitudeCommand.WithSurface(Pitch, Heading, Roll);
                    break;
                case 4: // Orbital reference
                    mAttitude = (mAttitude == 0) ? 1 : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.Orbit);
                    break;
                case 5: // Surface reference
                    mAttitude = (mAttitude == 0) ? 1 : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.Surface);
                    break;
                case 6: // Target reference
                    mAttitude = (mAttitude == 0) ? 1 : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.Target);
                    break;
            }
            mFlightComputer.Enqueue(newCommand);
        }

        private void OnBurnClick() {
            mFlightComputer.Enqueue(BurnCommand.WithDuration(mThrottle, Duration));
        }
    }
}