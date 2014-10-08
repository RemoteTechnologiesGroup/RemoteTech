using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public class QueueFragment : IFragment
    {
        private readonly FlightComputer mFlightComputer;

        private Vector2 mScrollPosition;
        private String mExtraDelay;

        private double Delay
        {
            get
            {
                TimeSpan delay;
                if (!RTUtil.TryParseDuration(mExtraDelay, out delay))
                {
                    delay = new TimeSpan();
                }
                return Math.Max(delay.TotalSeconds, 0);
            }
            set { mExtraDelay = value.ToString(); }
        }

        private GUIContent Status
        {
            get
            {
                var tooltip = new List<String>();
                var status = new List<String>();
                if ((mFlightComputer.Status & FlightComputer.State.NoConnection) == FlightComputer.State.NoConnection)
                {
                    status.Add("Connection Error");
                    tooltip.Add("Cannot queue commands");
                }
                if ((mFlightComputer.Status & FlightComputer.State.OutOfPower) == FlightComputer.State.OutOfPower)
                {
                    status.Add("Out of Power");
                    tooltip.Add("Commands can be missed");
                    tooltip.Add("Timers halt");
                }
                if ((mFlightComputer.Status & FlightComputer.State.NotMaster) == FlightComputer.State.NotMaster)
                {
                    status.Add("Slave");
                    tooltip.Add("Has no control");
                }
                if ((mFlightComputer.Status & FlightComputer.State.Packed) == FlightComputer.State.Packed)
                {
                    status.Add("Packed");
                    tooltip.Add("Frozen");
                }
                if (mFlightComputer.Status == FlightComputer.State.Normal)
                {
                    status.Add("All systems nominal");
                    tooltip.Add("None");
                }
                return new GUIContent("Status: " + String.Join(", ", status.ToArray()) + ".",
                    "Effects: " + String.Join("; ", tooltip.ToArray()) + ".");
            }
        }

        public QueueFragment(FlightComputer fc)
        {
            mFlightComputer = fc;
            Delay = 0;
        }

        public void Draw()
        {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl() == "xd")
            {
                RTCore.Instance.StartCoroutine(onClickAddExtraDelay());
            }
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(250));
                {
                    {
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        {
                            var s = new StringBuilder();
                            foreach (var c in mFlightComputer.ActiveCommands)
                            {
                                s.Append(c.Description);
                            }
                            GUILayout.Label(s.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
                            GUILayout.FlexibleSpace();
                            RTUtil.Button("x", () => RTCore.Instance.StartCoroutine(OnClickReset()), GUILayout.Width(21), GUILayout.Height(21));
                        }
                        GUILayout.EndHorizontal();

                        foreach (var c in mFlightComputer.QueuedCommands)
                        {
                            GUILayout.BeginHorizontal(GUI.skin.box);
                            {
                                GUILayout.Label(c.Description);
                                GUILayout.FlexibleSpace();
                                GUILayout.BeginVertical();
                                {
                                    RTUtil.Button("x", () => RTCore.Instance.StartCoroutine(OnClickCancel(c)), GUILayout.Width(21), GUILayout.Height(21));
                                    RTUtil.Button(new GUIContent("v", string.Format("Set the signal delay right after this - Current: {0}", RTUtil.FormatDuration(c.Delay + c.ExtraDelay))), () => RTCore.Instance.StartCoroutine(onClickAddExtraDelayFromQueuedCommand(c)), GUILayout.Width(21), GUILayout.Height(21));
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
                    GUILayout.Label(new GUIContent("Delay (+ signal): " + RTUtil.FormatDuration(mFlightComputer.TotalDelay), "Total delay including signal delay."));
                    GUILayout.FlexibleSpace();
                    GUI.SetNextControlName("xd");
                    RTUtil.TextField(ref mExtraDelay, GUILayout.Width(45));
                    RTUtil.Button(new GUIContent(">", "Add extra signal delay - Example: 125, 125s, 5m20s, 1d6h20m10s"), () => RTCore.Instance.StartCoroutine(onClickAddExtraDelay()), GUILayout.Width(21), GUILayout.Height(21));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public IEnumerator onClickAddExtraDelay()
        {
            yield return null;
            mFlightComputer.TotalDelay = Delay;
        }

        public IEnumerator onClickAddExtraDelayFromQueuedCommand(ICommand c)
        {
            yield return null;

            mExtraDelay = RTUtil.FormatDuration(c.Delay + c.ExtraDelay);
            RTCore.Instance.StartCoroutine(onClickAddExtraDelay());
        }

        public IEnumerator OnClickCancel(ICommand c)
        {
            yield return null;
            mFlightComputer.Enqueue(CancelCommand.WithCommand(c));
        }

        public IEnumerator OnClickReset()
        {
            yield return null;
            mFlightComputer.Enqueue(CancelCommand.ResetActive());
        }
    }
}