using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RemoteTech.Common.UI;
using RemoteTech.Common.Utils;
using RemoteTech.FlightComputer.Commands;
using UnityEngine;

namespace RemoteTech.FlightComputer.UI
{
    public class QueueFragment : IFragment
    {
        private readonly FlightComputer _flightComputer;

        private Vector2 _scrollPosition;
        private string _extraDelay;

        private double Delay
        {
            get { return TimeUtil.TryParseDuration(_extraDelay); }
            set { _extraDelay = value.ToString(CultureInfo.InvariantCulture); }
        }

        private GUIContent Status
        {
            get
            {
                var tooltip = new List<string>();
                var status = new List<string>();
                if ((_flightComputer.Status & FlightComputer.State.NoConnection) == FlightComputer.State.NoConnection)
                {
                    status.Add("Connection Error");
                    tooltip.Add("Cannot queue commands");
                }
                if ((_flightComputer.Status & FlightComputer.State.OutOfPower) == FlightComputer.State.OutOfPower)
                {
                    status.Add("Out of Power");
                    tooltip.Add("Commands can be missed");
                    tooltip.Add("Timers halt");
                }
                if ((_flightComputer.Status & FlightComputer.State.NotMaster) == FlightComputer.State.NotMaster)
                {
                    status.Add("Slave");
                    tooltip.Add("Has no control");
                }
                if ((_flightComputer.Status & FlightComputer.State.Packed) == FlightComputer.State.Packed)
                {
                    status.Add("Packed");
                    tooltip.Add("Frozen");
                }
                if (_flightComputer.Status == FlightComputer.State.Normal)
                {
                    status.Add("All systems nominal");
                    tooltip.Add("None");
                }

                var guiText = $"Status: {string.Join(", ", status.ToArray())}.";
                var guiTooltip = $"Effects: {string.Join("; ", tooltip.ToArray())}.";

                return new GUIContent(guiText, guiTooltip);
            }
        }

        public QueueFragment(FlightComputer fc)
        {
            _flightComputer = fc;
            Delay = 0;
        }

        public void Draw()
        {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl() == "rt_xd")
            {
                FlightGlobals.fetch.StartCoroutine(OnClickAddExtraDelay());
            }
            GUILayout.BeginVertical();
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(250));
                {
                    {
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        {
                            var s = new StringBuilder();
                            foreach (var c in _flightComputer.ActiveCommands)
                            {
                                s.Append(c.Description);
                            }
                            GUILayout.Label(s.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
                            GUILayout.FlexibleSpace();
                            GuiUtil.Button("x", () => FlightGlobals.fetch.StartCoroutine(OnClickReset()), GUILayout.Width(21), GUILayout.Height(21));
                        }
                        GUILayout.EndHorizontal();

                        foreach (var c in _flightComputer.QueuedCommands)
                        {
                            GUILayout.BeginHorizontal(GUI.skin.box);
                            {
                                GUILayout.Label(c.Description);
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                {
                                    GuiUtil.Button("x", () => FlightGlobals.fetch.StartCoroutine(OnClickCancel(c)), GUILayout.Width(21), GUILayout.Height(21));
                                    var delay = TimeUtil.FormatDuration(c.Delay + c.ExtraDelay + GetBurnTime(c), false);
                                    GuiUtil.Button(
                                        new GUIContent("v", $"Set the signal delay right after this - Current: {delay}"), 
                                        () => FlightGlobals.fetch.StartCoroutine(OnClickAddExtraDelayFromQueuedCommand(c)), GUILayout.Width(21), GUILayout.Height(21));
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Label(Status);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("Delay (+ signal): " + TimeUtil.FormatDuration(_flightComputer.TotalDelay), "Total delay including signal delay."));
                    GUILayout.FlexibleSpace();
                    GUI.SetNextControlName("rt_xd");
                    GuiUtil.TextField(ref _extraDelay, GUILayout.Width(45));
                    GuiUtil.Button(
                        new GUIContent(">", "Add extra signal delay - Example: 125, 125s, 5m20s, 1d6h20m10s"), 
                        () => FlightGlobals.fetch.StartCoroutine(OnClickAddExtraDelay()), GUILayout.Width(21), GUILayout.Height(21));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public IEnumerator OnClickAddExtraDelay()
        {
            yield return null;
            _flightComputer.TotalDelay = Delay;
        }

        public IEnumerator OnClickAddExtraDelayFromQueuedCommand(ICommand c)
        {
            yield return null;

            _extraDelay = TimeUtil.FormatDuration(c.Delay + c.ExtraDelay + GetBurnTime(c), false);
            FlightGlobals.fetch.StartCoroutine(OnClickAddExtraDelay());
        }

        /// <summary>
        /// Get the burn time from the ManeuverCommand or BurnCommand
        /// </summary>
        /// <param name="c">Current command</param>
        /// <returns>Max burn time</returns>
        public double GetBurnTime(ICommand c)
        {
            //TODO : make an interface for commands having a getMaxBurnTime() method and get rid of those checks and casts.
            if (c is ManeuverCommand || c is BurnCommand)
            {
                double burnTime = (c is ManeuverCommand) ? ((ManeuverCommand)c).getMaxBurnTime(_flightComputer) : ((BurnCommand)c).getMaxBurnTime(_flightComputer);

                return burnTime;
            }
            return 0;
        }

        public IEnumerator OnClickCancel(ICommand c)
        {
            yield return null;
            _flightComputer.Enqueue(CancelCommand.WithCommand(c));
        }

        public IEnumerator OnClickReset()
        {
            yield return null;
            _flightComputer.Enqueue(CancelCommand.ResetActive());
        }
    }
}