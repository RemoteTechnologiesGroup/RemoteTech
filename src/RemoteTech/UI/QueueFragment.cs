using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RemoteTech.FlightComputer.Commands;
using UnityEngine;
using KSP.Localization;

namespace RemoteTech.UI
{
    public class QueueFragment : IFragment
    {
        private readonly FlightComputer.FlightComputer mFlightComputer;

        private Vector2 mScrollPosition;
        private String mExtraDelay;

        private double Delay
        {
            get { return RTUtil.TryParseDuration(mExtraDelay); }
            set { mExtraDelay = value.ToString(); }
        }

        private GUIContent Status
        {
            get
            {
                var tooltip = new List<String>();
                var status = new List<String>();
                if ((mFlightComputer.Status & FlightComputer.FlightComputer.State.NoConnection) == FlightComputer.FlightComputer.State.NoConnection)
                {
                    status.Add(Localizer.Format("#RT_QueueFragment_statu1"));//"Connection Error"
                    tooltip.Add(Localizer.Format("#RT_QueueFragment_statu1_desc"));//"Cannot queue commands"
                }
                if ((mFlightComputer.Status & FlightComputer.FlightComputer.State.OutOfPower) == FlightComputer.FlightComputer.State.OutOfPower)
                {
                    status.Add(Localizer.Format("#RT_QueueFragment_statu2"));//"Out of Power"
                    tooltip.Add(Localizer.Format("#RT_QueueFragment_statu2_desc"));//"Commands can be missed"
                    tooltip.Add(Localizer.Format("#RT_QueueFragment_statu2_desc2"));//"Timers halt"
                }
                if ((mFlightComputer.Status & FlightComputer.FlightComputer.State.NotMaster) == FlightComputer.FlightComputer.State.NotMaster)
                {
                    status.Add(Localizer.Format("#RT_QueueFragment_statu3"));//"Slave"
                    tooltip.Add(Localizer.Format("#RT_QueueFragment_statu3_desc"));//"Has no control"
                }
                if ((mFlightComputer.Status & FlightComputer.FlightComputer.State.Packed) == FlightComputer.FlightComputer.State.Packed)
                {
                    status.Add(Localizer.Format("#RT_QueueFragment_statu4"));//"Packed"
                    tooltip.Add(Localizer.Format("#RT_QueueFragment_statu4_desc"));//"Frozen"
                }
                if (mFlightComputer.Status == FlightComputer.FlightComputer.State.Normal)
                {
                    status.Add(Localizer.Format("#RT_QueueFragment_statu5"));//"All systems nominal"
                    tooltip.Add(Localizer.Format("#RT_QueueFragment_statu5_desc"));//"None"
                }
                return new GUIContent(Localizer.Format("#RT_QueueFragment_statuText", String.Join(", ", status.ToArray())),
                   Localizer.Format("#RT_QueueFragment_statuText_desc", String.Join("; ", tooltip.ToArray())));//"Status: " +  + ".""Effects: " +  + "."
            }
        }

        public QueueFragment(FlightComputer.FlightComputer fc)
        {
            mFlightComputer = fc;
            Delay = 0;
        }

        public void Draw()
        {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl() == "rt_xd")
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
                                    RTUtil.Button(new GUIContent("v", Localizer.Format("#RT_QueueFragment_Setsignaldelay", RTUtil.FormatDuration(c.Delay + c.ExtraDelay + getBurnTime(c), false))), () => RTCore.Instance.StartCoroutine(onClickAddExtraDelayFromQueuedCommand(c)), GUILayout.Width(21), GUILayout.Height(21));//"Set the signal delay right after this - Current: {0}"
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
                    GUILayout.Label(new GUIContent(Localizer.Format("#RT_QueueFragment_Signaldelay", RTUtil.FormatDuration(mFlightComputer.TotalDelay)), Localizer.Format("#RT_QueueFragment_Signaldelay_desc")));//"Delay (+ signal): " + "Total delay including signal delay."
                    GUILayout.FlexibleSpace();
                    GUI.SetNextControlName("rt_xd");
                    RTUtil.TextField(ref mExtraDelay, GUILayout.Width(45));
                    RTUtil.Button(new GUIContent(">", Localizer.Format("#RT_QueueFragment_AddExtraDelay_desc")), () => RTCore.Instance.StartCoroutine(onClickAddExtraDelay()), GUILayout.Width(21), GUILayout.Height(21));//"Add extra signal delay - Example: 125, 125s, 5m20s, 1d6h20m10s"
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

            mExtraDelay = RTUtil.FormatDuration(c.Delay + c.ExtraDelay + getBurnTime(c), false);
            RTCore.Instance.StartCoroutine(onClickAddExtraDelay());
        }

        /// <summary>
        /// Get the burn time from the ManeuverCommand or BurnCommand
        /// </summary>
        /// <param name="c">Current ocmmand</param>
        /// <returns>Max burn time</returns>
        public double getBurnTime(ICommand c)
        {
            if (c is ManeuverCommand || c is BurnCommand)
            {
                double burnTime = (c is ManeuverCommand) ? ((ManeuverCommand)c).getMaxBurnTime(mFlightComputer) : ((BurnCommand)c).getMaxBurnTime(mFlightComputer);

                return burnTime;
            }
            return 0;
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