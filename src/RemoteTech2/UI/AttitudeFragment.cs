using System;
using UnityEngine;

namespace RemoteTech
{
    public class AttitudeFragment : IFragment
    {
        private float Pitch
        {
            get
            {
                float pitch;
                if (!Single.TryParse(mPitch, out pitch))
                {
                    pitch = 0;
                }
                return pitch;
            }
            set { mPitch = value.ToString(); }
        }

        private float Heading
        {
            get
            {
                float heading;
                if (!Single.TryParse(mHeading, out heading))
                {
                    heading = 0;
                }
                return heading;
            }
            set { mHeading = value.ToString(); }
        }

        private float Roll
        {
            get
            {
                float roll;
                if (!Single.TryParse(mRoll, out roll))
                {
                    roll = 0;
                }
                return roll;
            }
            set { mRoll = value.ToString(); }
        }

        private double Duration
        {
            get
            {
                TimeSpan duration;
                if (!RTUtil.TryParseDuration(mDuration, out duration))
                {
                    duration = new TimeSpan();
                }
                return duration.TotalSeconds;
            }
            set { mDuration = RTUtil.FormatDuration(value); }
        }

        private double DeltaV
        {
            get
            {
                double deltav;
                String input = mDuration.TrimEnd("m/s".ToCharArray());
                if (!mDuration.EndsWith("m/s") || !Double.TryParse(input, out deltav))
                {
                    deltav = Double.NaN;
                }
                return deltav;
            }
        }

        private FlightAttitude Attitude
        {
            get
            {
                switch (mAttitude)
                {
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
        private Action mOnClickQueue;

        private int mMode;
        private int mAttitude;
        private float mThrottle;

        private String mPitch = "90";
        private String mRoll = "0";
        private String mHeading = "90";
        private String mDuration = "0s";

        public AttitudeFragment(FlightComputer fc, Action queue)
        {
            mFlightComputer = fc;
            mOnClickQueue = queue;
        }

        public void Draw()
        {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;
            if (Event.current.Equals(Event.KeyboardEvent("return")))
            {
                if (GUI.GetNameOfFocusedControl().StartsWith("phr"))
                {
                    mPitch = Pitch.ToString();
                    mHeading = Heading.ToString();
                    mRoll = Roll.ToString();
                    if (mFlightComputer.InputAllowed)
                    {
                        mMode = 7;
                        Confirm();
                    }
                }
                else if (GUI.GetNameOfFocusedControl() == "burn")
                {
                    OnBurnClick();
                }
            }
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    RTUtil.StateButton("KILL", mMode, 1, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("NODE", mMode, 2, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("PAR", mMode, 3, OnModeClick, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.StateButton("ORB", mMode, 4, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("SRF", mMode, 5, OnModeClick, GUILayout.Width(width3));
                    RTUtil.StateButton("TGT", mMode, 6, OnModeClick, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                RTUtil.StateButton("CUSTOM", mMode, 7, OnModeClick, GUILayout.ExpandWidth(true));
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
                    GUILayout.Label("PIT:", GUILayout.Width(width3));
                    RTUtil.Button("+", () => Pitch++);
                    RTUtil.Button("-", () => Pitch--);
                    GUI.SetNextControlName("phr1");
                    RTUtil.TextField(ref mPitch, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("HDG:", GUILayout.Width(width3));
                    RTUtil.Button("+", () => Heading++);
                    RTUtil.Button("-", () => Heading--);
                    GUI.SetNextControlName("phr2");
                    RTUtil.TextField(ref mHeading, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("RLL:", GUILayout.Width(width3));
                    RTUtil.Button("+", () => Roll++);
                    RTUtil.Button("-", () => Roll--);
                    GUI.SetNextControlName("phr3");
                    RTUtil.TextField(ref mRoll, GUILayout.Width(width3));
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
                GUI.SetNextControlName("burn");
                RTUtil.TextField(ref mDuration);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("BRN", OnBurnClick, GUILayout.Width(width3));
                    GUILayout.FlexibleSpace();
                    RTUtil.Button("Q", mOnClickQueue, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void OnModeClick(int state)
        {
            if (!mFlightComputer.InputAllowed)
                return;
            mMode = (state < 0) ? 0 : state;
            Confirm();
        }

        private void OnAttitudeClick(int state)
        {
            if (!mFlightComputer.InputAllowed)
                return;
            mAttitude = (state < 0) ? 0 : state;
            if (mMode < 4)
            {
                mMode = 4;
            }
            Confirm();
        }

        private void Confirm()
        {
            DelayedCommand newCommand;
            switch (mMode)
            {
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
                case 3: // Target Parallel
                    mAttitude = (mAttitude == 0) ? 1 : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.TargetParallel);
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
                case 6: // Target Velocity
                    mAttitude = (mAttitude == 0) ? 1 : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.TargetVelocity);
                    break;
                case 7: // Custom Surface Heading
                    mAttitude = 0;
                    newCommand = AttitudeCommand.WithSurface(Pitch, Heading, Roll);
                    break;
            }
            mFlightComputer.Enqueue(newCommand);
        }

        private void OnBurnClick()
        {
            if (!Double.IsNaN(DeltaV))
            {
                mFlightComputer.Enqueue(BurnCommand.WithDeltaV(mThrottle, DeltaV));
            }
            else
            {
                mFlightComputer.Enqueue(BurnCommand.WithDuration(mThrottle, Duration));
            }
        }
    }
}