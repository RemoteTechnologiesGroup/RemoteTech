using System;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class QueueFragment : IFragment {
        private readonly FlightComputer mFlightComputer;

        private Vector2 mScrollPosition;
        private String mExtraDelay;

        private double Delay {
            get {
                double delay;
                if (!Double.TryParse(mExtraDelay, out delay)) {
                    delay = 0;
                }
                return Math.Max(delay, 0);
            }
            set { mExtraDelay = value.ToString(); }
        }

        public QueueFragment(FlightComputer fc) {
            mFlightComputer = fc;
            Delay = 0;
        }

        public void Draw() {
            if (Event.current.Equals(Event.KeyboardEvent ("return")) &&
                    GUI.GetNameOfFocusedControl() == "xd") {
                mExtraDelay = Delay.ToString();
                mFlightComputer.ExtraDelay = Delay;
            }
            GUILayout.BeginVertical(GUILayout.Width(250));
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, 
                    GUILayout.ExpandHeight(true));
                {
                    foreach (DelayedCommand dc in mFlightComputer) {
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        {
                            GUILayout.Label(Format(dc));
                            RTUtil.Button("x", () => { mFlightComputer.Enqueue(DelayedCommand.Cancel(dc)); }, GUILayout.Width(21));
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Artificial delay: ");
                    GUI.SetNextControlName("xd");
                    GUILayout.Label(mFlightComputer.ExtraDelay.ToString("F2"));
                    RTUtil.TextField(ref mExtraDelay, GUILayout.ExpandWidth(true));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private String Format(DelayedCommand dc) {
            StringBuilder s = new StringBuilder();
            if (dc.AttitudeCommand != null) {
                switch (dc.AttitudeCommand.Mode) {
                    case FlightMode.Off:
                        s.AppendLine("Mode: Off");
                        break;
                    case FlightMode.KillRot:
                        s.AppendLine("Mode: Kill rotation");
                        break;
                    case FlightMode.AttitudeHold:
                        s.Append("Mode: Hold, ");
                        switch (dc.AttitudeCommand.Attitude) {
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
                                s.Append(", ");
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.y.ToString("F1"));
                                s.Append(", ");
                                s.AppendLine(dc.AttitudeCommand.Orientation.eulerAngles.z.ToString("F1"));
                                break;
                        }
                        break;
                    case FlightMode.AltitudeHold:
                        s.Append("Mode: Hold, ");
                        s.AppendLine(RTUtil.FormatSI(dc.AttitudeCommand.Altitude, "m"));
                        break;
                }
            }
            if (dc.ActionGroupCommand != null) {
                s.Append("Toggle ");
                s.AppendLine(dc.ActionGroupCommand.ActionGroup.ToString());
            }
            if (dc.BurnCommand != null) {
                s.Append("Burn ");
                s.Append(dc.BurnCommand.Throttle.ToString("P2"));
                if (dc.BurnCommand.Duration != Single.NaN) {
                    s.Append(", ");
                    s.Append(dc.BurnCommand.Duration.ToString("F2"));
                    s.Append("s");
                }
                if (dc.BurnCommand.DeltaV != Single.NaN) {
                    s.Append(", ");
                    s.Append(dc.BurnCommand.DeltaV.ToString("F2"));
                    s.Append("m/s");
                }
                s.AppendLine();
            }
            if (dc.DriveCommand != null) {
                dc.DriveCommand.GetDescription(s);
                s.AppendLine();
            }
            if (dc.EventCommand != null) {
                s.Append(dc.EventCommand.BaseEvent.listParent.part.partInfo.title);
                s.Append(": ");
                s.AppendLine(dc.EventCommand.BaseEvent.GUIName);
            }
            if (dc.CancelCommand != null) {
                s.AppendLine("Cancelling a command");
            }

            double delay = Math.Max(dc.TimeStamp - RTUtil.GetGameTime(), 0);
            if (delay > 0 || dc.ExtraDelay > 0) {
                s.AppendLine();
                s.Append("Signal delay: ");
                s.Append(delay.ToString("F2"));
                s.Append("s");
                if (dc.ExtraDelay > 0) {
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