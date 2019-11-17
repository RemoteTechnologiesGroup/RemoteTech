using System;
using System.Collections;
using RemoteTech.FlightComputer.Commands;
using UnityEngine;
using KSP.Localization;

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
                    GUIStyle guiTableRow = new GUIStyle(HighLogic.Skin.label);
                    guiTableRow.normal.textColor = Color.white;

                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_KILL"), Localizer.Format("#RT_AttitudeFragment_KILL_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Kill)), (int)mMode, (int)ComputerMode.Kill, GUILayout.Width(width3));//"KILL""Kill rotation."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_NODE"), Localizer.Format("#RT_AttitudeFragment_NODE_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Node)), (int)mMode, (int)ComputerMode.Node, GUILayout.Width(width3));//"NODE""Prograde points in the direction of the first maneuver node."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_RVEL"), Localizer.Format("#RT_AttitudeFragment_RVEL_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.TargetVel)), (int)mMode, (int)ComputerMode.TargetVel, GUILayout.Width(width3));//"RVEL""Prograde relative to target velocity."
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_ORB"), Localizer.Format("#RT_AttitudeFragment_ORB_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Orbital)), (int)mMode, (int)ComputerMode.Orbital, GUILayout.Width(width3));//"ORB""Prograde relative to orbital velocity."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_SRF"), Localizer.Format("#RT_AttitudeFragment_SRF_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Surface)), (int)mMode, (int)ComputerMode.Surface, GUILayout.Width(width3));//"SRF""Prograde relative to surface velocity."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_TGT"), Localizer.Format("#RT_AttitudeFragment_TGT_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.TargetPos)), (int)mMode, (int)ComputerMode.TargetPos, GUILayout.Width(width3));//"TGT""Prograde points directly at target."
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_OFF"), Localizer.Format("#RT_AttitudeFragment_OFF_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Off)), (int)mMode, (int)ComputerMode.Off, GUILayout.Width(width3));//"OFF""Set Attitude to Off."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_CUSTOM"), Localizer.Format("#RT_AttitudeFragment_CUSTOM_desc")), () => RTCore.Instance.StartCoroutine(OnModeClick(ComputerMode.Custom)), (int)mMode, (int)ComputerMode.Custom, GUILayout.ExpandWidth(true));//"CUSTOM""Prograde fixed as pitch, heading, roll relative to north pole."
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_Prograde"), Localizer.Format("#RT_AttitudeFragment_Prograde_desc")), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.Prograde)), (int)mAttitude, (int)FlightAttitude.Prograde, GUILayout.Width(width3));//"GRD\n+""Orient to Prograde."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_RadialPlus"), Localizer.Format("#RT_AttitudeFragment_RadialPlus_desc")), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.RadialPlus)), (int)mAttitude, (int)FlightAttitude.RadialPlus, GUILayout.Width(width3));//"RAD\n+""Orient to Radial."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_NormalPlus"), Localizer.Format("#RT_AttitudeFragment_NormalPlus_desc")), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.NormalPlus)), (int)mAttitude, (int)FlightAttitude.NormalPlus, GUILayout.Width(width3));//"NRM\n+""Orient to Normal."
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_Retrograde"), Localizer.Format("#RT_AttitudeFragment_Retrograde_desc")), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.Retrograde)), (int)mAttitude, (int)FlightAttitude.Retrograde, GUILayout.Width(width3));//"GRD\n-""Orient to Retrograde."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_RadialMinus"), Localizer.Format("#RT_AttitudeFragment_RadialMinus_desc")), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.RadialMinus)), (int)mAttitude, (int)FlightAttitude.RadialMinus, GUILayout.Width(width3));//"RAD\n-""Orient to Anti-radial."
                    RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_AttitudeFragment_NormalMinus"), Localizer.Format("#RT_AttitudeFragment_NormalMinus_desc")), () => RTCore.Instance.StartCoroutine(OnAttitudeClick(FlightAttitude.NormalMinus)), (int)mAttitude, (int)FlightAttitude.NormalMinus, GUILayout.Width(width3));//"NRM\n-""Orient to Anti-normal."
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_AttitudeFragment_PIT"), Localizer.Format("#RT_AttitudeFragment_PIT_desc")), GUILayout.Width(width3));//"PIT:""Sets pitch."
                    RTUtil.RepeatButton("+", () => { Pitch++; });
                    RTUtil.RepeatButton("-", () => { Pitch--; });
                    RTUtil.MouseWheelTriggerField(ref mPitch, "rt_phr1", () => { Pitch++; }, () => { Pitch--; }, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_AttitudeFragment_HDG"), Localizer.Format("#RT_AttitudeFragment_HDG_desc")), GUILayout.Width(width3));//"HDG:""Sets heading."
                    RTUtil.RepeatButton("+", () => { Heading++; });
                    RTUtil.RepeatButton("-", () => { Heading--; });
                    RTUtil.MouseWheelTriggerField(ref mHeading, "rt_phr2", () => { Heading++; }, () => { Heading--; }, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_AttitudeFragment_RLL"), Localizer.Format("#RT_AttitudeFragment_RLL_desc")), GUILayout.Width(width3));//"RLL:""Sets roll."
                    RTUtil.RepeatButton("+", () => { Roll++; });
                    RTUtil.RepeatButton("-", () => { Roll--; });
                    RTUtil.MouseWheelTriggerField(ref mRoll, "rt_phr3", () => { Roll++; }, () => { Roll--; }, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(Localizer.Format("#RT_AttitudeFragment_Throttle"));//"Throttle: "
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(mThrottle.ToString("P"));
                }
                GUILayout.EndHorizontal();

                RTUtil.HorizontalSlider(ref mThrottle, 0, 1);
                GUI.SetNextControlName("rt_burn");
                RTUtil.TextField(ref mDuration);

                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button(new GUIContent(Localizer.Format("#RT_AttitudeFragment_BURN"), Localizer.Format("#RT_AttitudeFragment_BURN_desc")),//"BURN""Example: 125, 125s, 5m20s, 1d6h20m10s, 123m/s."
                        OnBurnClick, GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent(Localizer.Format("#RT_AttitudeFragment_EXEC"), Localizer.Format("#RT_AttitudeFragment_EXEC_desc")),//"EXEC""Executes next and subsequent maneuver nodes."
                        OnExecClick, GUILayout.Width(width3));
                    RTUtil.Button(new GUIContent(">>", Localizer.Format("#RT_AttitudeFragment_Queue_desc")),//"Toggles the queue and delay functionality."
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
                RTUtil.ScreenMessage(Localizer.Format("#RT_FC_msg5"));//"[Flight Computer]: Signal delay is too high to execute this maneuver at the proper time."
            }
            else
            {
                mFlightComputer.Enqueue(cmd, false, false, true);

                //check for subsequent nodes
                int numSubsequentNodes = mFlightComputer.Vessel.patchedConicSolver.maneuverNodes.Count - 1;
                if (numSubsequentNodes >= 1)
                {
                    for (int nodeIndex = 1; nodeIndex <= numSubsequentNodes; nodeIndex++)
                    {
                        mFlightComputer.Enqueue(ManeuverCommand.WithNode(nodeIndex, mFlightComputer), false, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// Get the current active FlightMode and map it to the Computermode
        /// </summary>
        public void getActiveFlightMode()
        {
            // check the current flight mode
            if (mFlightComputer.CurrentFlightMode == null)
            {
                Reset();
                return;
            }

            // get active command
            SimpleTypes.ComputerModeMapper mappedCommand = mFlightComputer.CurrentFlightMode.mapFlightMode();
            mMode = mappedCommand.computerMode;
            mAttitude = FlightAttitude.Null;

            if(mMode == ComputerMode.Orbital || mMode == ComputerMode.Surface || mMode == ComputerMode.TargetPos || mMode == ComputerMode.TargetVel)
                mAttitude = mappedCommand.computerAttitude;
        }

        /// <summary>
        /// Reset the modes
        /// </summary>
        public void Reset()
        {
            // get active command
            mMode = ComputerMode.Off;
            mAttitude = FlightAttitude.Null;
        }
    }
}