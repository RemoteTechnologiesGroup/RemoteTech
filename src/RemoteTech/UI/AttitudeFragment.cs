﻿using System;
using System.Collections;
using RemoteTech.FlightComputer.Commands;
using UnityEngine;

namespace RemoteTech.UI
{
    public enum ComputerMode
    {
        Off,
        Kill,
        Node,
        TargetPos,
        Orbital,
        Surface,
        TargetVel,
        Custom
    }

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
            get { return RTUtil.TryParseDuration(mDuration); }
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

        private FlightAttitude Attitude { get { return mAttitude; } }

        private FlightComputer.FlightComputer mFlightComputer;
        private Action mOnClickQueue;

        private ComputerMode mMode;
        private FlightAttitude mAttitude;
        private float mThrottle;

        private String mPitch = "90";
        private String mRoll = "90";
        private String mHeading = "90";
        private String mDuration = "0s";

        public AttitudeFragment(FlightComputer.FlightComputer fc, Action queue)
        {
            mFlightComputer = fc;
            mOnClickQueue = queue;
        }

        public void Draw()
        {
            float width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;
            if (Event.current.Equals(Event.KeyboardEvent("return")))
            {
                if (GUI.GetNameOfFocusedControl().StartsWith("rt_phr"))
                {
                    mPitch = Pitch.ToString();
                    mHeading = Heading.ToString();
                    mRoll = Roll.ToString();
                    if (mFlightComputer.InputAllowed)
                    {
                        mMode = ComputerMode.Custom;
                        Confirm();
                    }
                }
                else if (GUI.GetNameOfFocusedControl() == "rt_burn")
                {
                    OnBurnClick();
                }
            }
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent("KILL", "Kill rotation."), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Kill)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("NODE", "Prograde points in the direction of the first maneuver node."), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Node)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("RVEL", "Prograde relative to target velocity."), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.TargetVel)), GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent("ORB", "Prograde relative to orbital velocity."), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Orbital)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("SRF", "Prograde relative to surface velocity."), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Surface)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("TGT", "Prograde points directly at target."), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.TargetPos)), GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                RTUtil.Button(new GUIContent("CUSTOM", "Prograde fixed as pitch, heading, roll relative to north pole."), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Custom)), GUILayout.ExpandWidth(true));
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent("GRD\n+", "Orient to Prograde."), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.Prograde)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("RAD\n+", "Orient to Radial."), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.RadialPlus)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("NRM\n+", "Orient to Normal."), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.NormalPlus)), GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent("GRD\n-", "Orient to Retrograde."), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.Retrograde)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("RAD\n-", "Orient to Anti-radial."), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.RadialMinus)), GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("NRM\n-", "Orient to Anti-normal."), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.NormalMinus)), GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("PIT:", "Sets pitch."), GUILayout.Width(width3));
                    RTUtil.Button("+", () => Pitch++);
                    RTUtil.Button("-", () => Pitch--);
                    GUI.SetNextControlName("rt_phr1");
                    RTUtil.TextField(ref mPitch, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("HDG:", "Sets heading."), GUILayout.Width(width3));
                    RTUtil.Button("+", () => Heading++);
                    RTUtil.Button("-", () => Heading--);
                    GUI.SetNextControlName("rt_phr2");
                    RTUtil.TextField(ref mHeading, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("RLL:", "Sets roll."), GUILayout.Width(width3));
                    RTUtil.Button("+", () => Roll++);
                    RTUtil.Button("-", () => Roll--);
                    GUI.SetNextControlName("rt_phr3");
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
                GUI.SetNextControlName("rt_burn");
                RTUtil.TextField(ref mDuration);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent("BURN", "Example: 125, 125s, 5m20s, 1d6h20m10s, 123m/s."),
                        OnBurnClick, GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent("EXEC", "Executes next maneuver node."),
                        OnExecClick, GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent(">>", "Toggles the queue and delay functionality."),
                        mOnClickQueue, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        // Called by RTUtil.Button
        // General-purpose function has to represent enums as integers
        private IEnumerator OnModeClick(ComputerMode state)
        {
            yield return null;
            if (mFlightComputer.InputAllowed)
            {
                mMode = (state < 0) ? ComputerMode.Off : state;
                Confirm();
            }
        }

        private IEnumerator OnAttitudeClick(FlightAttitude state)
        {
            yield return null;
            if (mFlightComputer.InputAllowed)
            {
                mAttitude = (state < 0) ? FlightAttitude.Null : state;
                if (mMode == ComputerMode.Off || mMode == ComputerMode.Kill || mMode == ComputerMode.Node)
                {
                    mMode = ComputerMode.Orbital;
                }
                Confirm();
            }
        }

        private void Confirm()
        {
            ICommand newCommand;
            switch (mMode)
            {
                default: 
                case ComputerMode.Off:
                    mAttitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.Off();
                    break;
                case ComputerMode.Kill:
                    mAttitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.KillRot();
                    break;
                case ComputerMode.Node:
                    mAttitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.ManeuverNode();
                    break;
                case ComputerMode.TargetPos:
                    mAttitude = (mAttitude == FlightAttitude.Null) ? FlightAttitude.Prograde : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.TargetParallel);
                    break;
                case ComputerMode.Orbital:
                    mAttitude = (mAttitude == FlightAttitude.Null) ? FlightAttitude.Prograde : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.Orbit);
                    break;
                case ComputerMode.Surface:
                    mAttitude = (mAttitude == FlightAttitude.Null) ? FlightAttitude.Prograde : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.Surface);
                    break;
                case ComputerMode.TargetVel:
                    mAttitude = (mAttitude == FlightAttitude.Null) ? FlightAttitude.Prograde : mAttitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.TargetVelocity);
                    break;
                case ComputerMode.Custom:
                    mAttitude = FlightAttitude.Null;
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

        private void OnExecClick()
        {
            if (mFlightComputer.Vessel.patchedConicSolver == null || mFlightComputer.Vessel.patchedConicSolver.maneuverNodes.Count == 0) return;
            var cmd = ManeuverCommand.WithNode(0, mFlightComputer);
            if (cmd.TimeStamp < RTUtil.GameTime + mFlightComputer.Delay)
            {
                RTUtil.ScreenMessage("[Flight Computer]: Signal delay is too high to execute this maneuver at the proper time.");
            }
            else
            {
                mFlightComputer.Enqueue(cmd, false, false, true);
            }
        }
    }
}