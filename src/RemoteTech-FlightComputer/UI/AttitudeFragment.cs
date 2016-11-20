using System;
using System.Collections;
using System.Globalization;
using RemoteTech.Common.Interfaces.FlightComputer.Commands;
using RemoteTech.Common.UI;
using RemoteTech.Common.Utils;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.FlightComputer.UI
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
        private static RTCore CoreInstance => RTCore.Instance;

        private float Pitch
        {
            get
            {
                float pitch;
                if (!float.TryParse(_pitchString, out pitch))
                {
                    pitch = 0;
                }
                return pitch;
            }
            set { _pitchString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private float Heading
        {
            get
            {
                float heading;
                if (!float.TryParse(_headingString, out heading))
                {
                    heading = 0;
                }
                return heading;
            }
            set { _headingString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private float Roll
        {
            get
            {
                float roll;
                if (!float.TryParse(_rollString, out roll))
                {
                    roll = 0;
                }
                return roll;
            }
            set { _rollString = value.ToString(CultureInfo.InvariantCulture); }
        }

        private double Duration
        {
            get { return TimeUtil.TryParseDuration(_durationString); }
            set { _durationString = TimeUtil.FormatDuration(value); }
        }

        private double DeltaV
        {
            get
            {
                double deltav;
                var input = _durationString.TrimEnd("m/s".ToCharArray());
                if (!_durationString.EndsWith("m/s") || !double.TryParse(input, out deltav))
                {
                    deltav = double.NaN;
                }
                return deltav;
            }
        }

        private FlightAttitude Attitude { get; set; }

        private readonly FlightComputer _flightComputer;
        private readonly Action _onClickQueue;

        private ComputerMode _computerMode;
        private float _throttle;

        private string _pitchString = "90";
        private string _rollString = "90";
        private string _headingString = "90";
        private string _durationString = "0s";

        public AttitudeFragment(FlightComputer fc, Action queue)
        {
            _flightComputer = fc;
            _onClickQueue = queue;
        }

        public void Draw()
        {
            var width3 = 156 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;
            if (Event.current.Equals(Event.KeyboardEvent("return")))
            {
                if (GUI.GetNameOfFocusedControl().StartsWith("rt_phr"))
                {
                    _pitchString = Pitch.ToString(CultureInfo.InvariantCulture);
                    _headingString = Heading.ToString(CultureInfo.InvariantCulture);
                    _rollString = Roll.ToString(CultureInfo.InvariantCulture);
                    if (_flightComputer.InputAllowed)
                    {
                        _computerMode = ComputerMode.Custom;
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
                    var guiTableRow = new GUIStyle(HighLogic.Skin.label);
                    guiTableRow.normal.textColor = Color.white;

                    GuiUtil.FakeStateButton(new GUIContent("KILL", "Kill rotation."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.Kill)), (int)_computerMode, (int)ComputerMode.Kill, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("NODE", "Prograde points in the direction of the first maneuver node."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.Node)), (int)_computerMode, (int)ComputerMode.Node, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("RVEL", "Prograde relative to target velocity."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.TargetVel)), (int)_computerMode, (int)ComputerMode.TargetVel, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GuiUtil.FakeStateButton(new GUIContent("ORB", "Prograde relative to orbital velocity."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.Orbital)), (int)_computerMode, (int)ComputerMode.Orbital, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("SRF", "Prograde relative to surface velocity."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.Surface)), (int)_computerMode, (int)ComputerMode.Surface, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("TGT", "Prograde points directly at target."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.TargetPos)), (int)_computerMode, (int)ComputerMode.TargetPos, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GuiUtil.FakeStateButton(new GUIContent("OFF", "Set Attitude to Off."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.Off)), (int)_computerMode, (int)ComputerMode.Off, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("CUSTOM", "Prograde fixed as pitch, heading, roll relative to north pole."), () => CoreInstance.StartCoroutine(OnModeClick(ComputerMode.Custom)), (int)_computerMode, (int)ComputerMode.Custom, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GuiUtil.FakeStateButton(new GUIContent("GRD\n+", "Orient to Prograde."), () => CoreInstance.StartCoroutine(OnAttitudeClick(FlightAttitude.Prograde)), (int)Attitude, (int)FlightAttitude.Prograde, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("RAD\n+", "Orient to Radial."), () => CoreInstance.StartCoroutine(OnAttitudeClick(FlightAttitude.RadialPlus)), (int)Attitude, (int)FlightAttitude.RadialPlus, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("NRM\n+", "Orient to Normal."), () => CoreInstance.StartCoroutine(OnAttitudeClick(FlightAttitude.NormalPlus)), (int)Attitude, (int)FlightAttitude.NormalPlus, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    GuiUtil.FakeStateButton(new GUIContent("GRD\n-", "Orient to Retrograde."), () => CoreInstance.StartCoroutine(OnAttitudeClick(FlightAttitude.Retrograde)), (int)Attitude, (int)FlightAttitude.Retrograde, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("RAD\n-", "Orient to Anti-radial."), () => CoreInstance.StartCoroutine(OnAttitudeClick(FlightAttitude.RadialMinus)), (int)Attitude, (int)FlightAttitude.RadialMinus, GUILayout.Width(width3));
                    GuiUtil.FakeStateButton(new GUIContent("NRM\n-", "Orient to Anti-normal."), () => CoreInstance.StartCoroutine(OnAttitudeClick(FlightAttitude.NormalMinus)), (int)Attitude, (int)FlightAttitude.NormalMinus, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("PIT:", "Sets pitch."), GUILayout.Width(width3));
                    GuiUtil.RepeatButton("+", () => { Pitch++; });
                    GuiUtil.RepeatButton("-", () => { Pitch--; });
                    GuiUtil.MouseWheelTriggerField(ref _pitchString, "rt_phr1", () => { Pitch++; }, () => { Pitch--; }, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("HDG:", "Sets heading."), GUILayout.Width(width3));
                    GuiUtil.RepeatButton("+", () => { Heading++; });
                    GuiUtil.RepeatButton("-", () => { Heading--; });
                    GuiUtil.MouseWheelTriggerField(ref _headingString, "rt_phr2", () => { Heading++; }, () => { Heading--; }, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("RLL:", "Sets roll."), GUILayout.Width(width3));
                    GuiUtil.RepeatButton("+", () => { Roll++; });
                    GuiUtil.RepeatButton("-", () => { Roll--; });
                    GuiUtil.MouseWheelTriggerField(ref _rollString, "rt_phr3", () => { Roll++; }, () => { Roll--; }, GUILayout.Width(width3));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Throttle: ");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(_throttle.ToString("P"));
                }
                GUILayout.EndHorizontal();

                GuiUtil.HorizontalSlider(ref _throttle, 0, 1);
                GUI.SetNextControlName("rt_burn");
                GuiUtil.TextField(ref _durationString);

                GUILayout.BeginHorizontal();
                {
                    GuiUtil.Button(new GUIContent("BURN", "Example: 125, 125s, 5m20s, 1d6h20m10s, 123m/s."),
                        OnBurnClick, GUILayout.Width(width3));
                    GuiUtil.Button(new GUIContent("EXEC", "Executes next maneuver node."),
                        OnExecClick, GUILayout.Width(width3));
                    GuiUtil.Button(new GUIContent(">>", "Toggles the queue and delay functionality."),
                        _onClickQueue, GUILayout.Width(width3));
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
            if (_flightComputer.InputAllowed)
            {
                _computerMode = (state < 0) ? ComputerMode.Off : state;
                Confirm();
            }
        }

        private IEnumerator OnAttitudeClick(FlightAttitude state)
        {
            yield return null;
            if (_flightComputer.InputAllowed)
            {
                Attitude = (state < 0) ? FlightAttitude.Null : state;
                if (_computerMode == ComputerMode.Off || _computerMode == ComputerMode.Kill || _computerMode == ComputerMode.Node)
                {
                    _computerMode = ComputerMode.Orbital;
                }
                Confirm();
            }
        }

        private void Confirm()
        {
            ICommand newCommand;
            switch (_computerMode)
            {
                case ComputerMode.Off:
                    Attitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.Off();
                    break;
                case ComputerMode.Kill:
                    Attitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.KillRot();
                    break;
                case ComputerMode.Node:
                    Attitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.ManeuverNode();
                    break;
                case ComputerMode.TargetPos:
                    Attitude = (Attitude == FlightAttitude.Null) ? FlightAttitude.Prograde : Attitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.TargetParallel);
                    break;
                case ComputerMode.Orbital:
                    Attitude = (Attitude == FlightAttitude.Null) ? FlightAttitude.Prograde : Attitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.Orbit);
                    break;
                case ComputerMode.Surface:
                    Attitude = (Attitude == FlightAttitude.Null) ? FlightAttitude.Prograde : Attitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.Surface);
                    break;
                case ComputerMode.TargetVel:
                    Attitude = (Attitude == FlightAttitude.Null) ? FlightAttitude.Prograde : Attitude;
                    newCommand =
                        AttitudeCommand.WithAttitude(Attitude, ReferenceFrame.TargetVelocity);
                    break;
                case ComputerMode.Custom:
                    Attitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.WithSurface(Pitch, Heading, Roll);
                    break;

                default:
                    Attitude = FlightAttitude.Null;
                    newCommand = AttitudeCommand.Off();
                    break;
            }
            _flightComputer.Enqueue(newCommand);
        }

        private void OnBurnClick()
        {
            if (!double.IsNaN(DeltaV))
            {
                _flightComputer.Enqueue(BurnCommand.WithDeltaV(_throttle, DeltaV));
            }
            else
            {
                _flightComputer.Enqueue(BurnCommand.WithDuration(_throttle, Duration));
            }
        }

        private void OnExecClick()
        {
            if (_flightComputer.Vessel.patchedConicSolver == null || _flightComputer.Vessel.patchedConicSolver.maneuverNodes.Count == 0) return;
            var cmd = ManeuverCommand.WithNode(0, _flightComputer);
            if (cmd.TimeStamp < TimeUtil.GameTime + _flightComputer.Delay)
            {
                GuiUtil.ScreenMessage("[Flight Computer]: Signal delay is too high to execute this maneuver at the proper time.");
            }
            else
            {
                _flightComputer.Enqueue(cmd, false, false, true);
            }
        }

        /// <summary>
        /// Get the current active FlightMode and map it to the <see cref="ComputerMode"/>.
        /// </summary>
        public void getActiveFlightMode()
        {
            // check the current flight mode
            if (_flightComputer.CurrentFlightMode == null)
            {
                Reset();
                return;
            }

            // get active command
            var mappedCommand = _flightComputer.CurrentFlightMode.MapFlightMode();
            _computerMode = mappedCommand.ComputerMode;
            Attitude = FlightAttitude.Null;

            if(_computerMode == ComputerMode.Orbital || _computerMode == ComputerMode.Surface || _computerMode == ComputerMode.TargetPos || _computerMode == ComputerMode.TargetVel)
                Attitude = mappedCommand.ComputerAttitude;
        }

        /// <summary>
        /// Reset the modes
        /// </summary>
        public void Reset()
        {
            // get active command
            _computerMode = ComputerMode.Off;
            Attitude = FlightAttitude.Null;
        }
    }
}