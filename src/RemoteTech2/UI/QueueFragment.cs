using System;
using System.Text;
using System.Collections;
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

        public QueueFragment(FlightComputer fc)
        {
            mFlightComputer = fc;
            Delay = 0;
        }

        public void Draw()
        {
            if (Event.current.Equals(Event.KeyboardEvent("return")) && GUI.GetNameOfFocusedControl() == "xd")
            {
                mFlightComputer.TotalDelay = Delay;
            }
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(250));
                {
                    foreach (DelayedCommand dc in mFlightComputer)
                    {
                        var text = Format(dc);
                        if (!String.IsNullOrEmpty(text))
                        {
                            GUILayout.BeginHorizontal(GUI.skin.box);
                            {
                                GUILayout.Label(text);
                                GUILayout.FlexibleSpace();
                                RTUtil.Button("x", () => { RTCore.Instance.StartCoroutine(OnClickCancel(dc)); }, GUILayout.Width(21), GUILayout.Height(21));
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Set total delay: " + mFlightComputer.TotalDelay.ToString("F2"));
                    GUILayout.FlexibleSpace();
                    GUI.SetNextControlName("xd");
                    RTUtil.TextField(ref mExtraDelay, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public IEnumerator OnClickCancel(DelayedCommand dc)
        {
            yield return null;
            mFlightComputer.Enqueue(DelayedCommand.Cancel(dc));
        }

        private String Format(DelayedCommand dc)
        {
            StringBuilder s = new StringBuilder();
            if (dc.AttitudeCommand != null)
            {
                switch (dc.AttitudeCommand.Mode)
                {
                    case FlightMode.Off:
                        s.AppendLine("Mode: Off");
                        break;
                    case FlightMode.KillRot:
                        s.AppendLine("Mode: Kill rotation");
                        break;
                    case FlightMode.AttitudeHold:
                        s.Append("Mode: Hold ");
                        switch (dc.AttitudeCommand.Frame)
                        {
                            case ReferenceFrame.Maneuver:
                                s.Append("Maneuver ");
                                break;
                            case ReferenceFrame.Orbit:
                                s.Append("OBT ");
                                break;
                            case ReferenceFrame.Surface:
                                s.Append("SRF ");
                                break;
                            case ReferenceFrame.TargetVelocity:
                                s.Append("TGT ");
                                break;
                            case ReferenceFrame.TargetParallel:
                                s.Append("PAR ");
                                break;
                        }
                        switch (dc.AttitudeCommand.Attitude)
                        {
                            case FlightAttitude.Prograde:
                                s.AppendLine("Prograde");
                                break;
                            case FlightAttitude.Retrograde:
                                s.AppendLine("Retrograde");
                                break;
                            case FlightAttitude.NormalPlus:
                                s.AppendLine("Normal +");
                                break;
                            case FlightAttitude.NormalMinus:
                                s.AppendLine("Normal -");
                                break;
                            case FlightAttitude.RadialPlus:
                                s.AppendLine("Radial +");
                                break;
                            case FlightAttitude.RadialMinus:
                                s.AppendLine("Radial -");
                                break;
                            case FlightAttitude.Surface:
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.x.ToString("F1"));
                                s.Append("°, ");
                                s.Append((360 - dc.AttitudeCommand.Orientation.eulerAngles.y).ToString("F1"));
                                s.Append("°, ");
                                s.Append(RTUtil.Format360To180(180 - dc.AttitudeCommand.Orientation.eulerAngles.z).ToString("F1"));
                                s.AppendLine("°");
                                break;
                        }
                        break;
                    case FlightMode.AltitudeHold:
                        s.Append("Mode: Hold ");
                        s.AppendLine(RTUtil.FormatSI(dc.AttitudeCommand.Altitude, "m"));
                        break;
                }
            }
            if (dc.ActionGroupCommand != null)
            {
                s.Append("Toggle ");
                s.AppendLine(dc.ActionGroupCommand.ActionGroup.ToString());
            }
            if (dc.BurnCommand != null)
            {
                s.Append("Burn ");
                s.Append(dc.BurnCommand.Throttle.ToString("P2"));
                if (dc.BurnCommand.Duration != Single.NaN)
                {
                    s.Append(", ");
                    s.Append(dc.BurnCommand.Duration.ToString("F2"));
                    s.Append("s");
                }
                if (dc.BurnCommand.DeltaV != Single.NaN)
                {
                    s.Append(", ");
                    s.Append(dc.BurnCommand.DeltaV.ToString("F2"));
                    s.Append("m/s");
                }
                s.AppendLine();
            }
            if (dc.EventCommand != null)
            {
                s.Append(dc.EventCommand.BaseEvent.listParent.part.partInfo.title);
                s.Append(": ");
                s.AppendLine(dc.EventCommand.BaseEvent.GUIName);
            }
            if (dc.CancelCommand != null)
            {
                s.AppendLine("Cancelling a command");
            }
            if (dc.TargetCommand != null)
            {
                s.Append("Target: ");
                s.AppendLine(dc.TargetCommand.Target != null ? dc.TargetCommand.Target.GetName() : "None");
            }

            if (s.ToString().Equals("")) return "";

            double delay = Math.Max(dc.TimeStamp - RTUtil.GameTime, 0);
            if (delay > 0 || dc.ExtraDelay > 0)
            {
                s.Append("Signal delay: ");
                s.Append(delay.ToString("F2"));
                s.Append("s");
                if (dc.ExtraDelay > 0)
                {
                    s.Append(" (+");
                    s.Append(dc.ExtraDelay.ToString("F2"));
                    s.Append("s)");
                }
                s.AppendLine();
            }
            return s.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}