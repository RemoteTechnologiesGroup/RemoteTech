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
                        GUILayout.Label(Format(dc), GUI.skin.textField);
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Artificial delay: ");
                    GUI.SetNextControlName("xd");
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
                        s.Append("Mode: Off");
                        break;
                    case FlightMode.KillRot:
                        s.Append("Mode: Kill rotation");
                        break;
                    case FlightMode.AttitudeHold:
                        s.Append("Mode: Hold, ");
                        switch (dc.AttitudeCommand.Attitude) {
                            case FlightAttitude.Prograde:
                                s.Append("Prograde");
                                break;
                            case FlightAttitude.Retrograde:
                                s.Append("Retrograde");
                                break;
                            case FlightAttitude.NormalPlus:
                                s.Append("Normal +");
                                break;
                            case FlightAttitude.NormalMinus:
                                s.Append("Normal -");
                                break;
                            case FlightAttitude.RadialPlus:
                                s.Append("Radial +");
                                break;
                            case FlightAttitude.RadialMinus:
                                s.Append("Radial -");
                                break;
                            case FlightAttitude.Surface:
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.x.ToString("F1"));
                                s.Append(", ");
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.y.ToString("F1"));
                                s.Append(", ");
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.z.ToString("F1"));
                                break;
                        }
                        break;
                    case FlightMode.AltitudeHold:
                        s.Append("Mode: Hold, ");
                        s.Append(RTUtil.FormatSI(dc.AttitudeCommand.Altitude, "m"));
                        break;
                }
            } else if (dc.ActionGroupCommand != null) {
                s.Append("Toggle ");
                s.Append(dc.ActionGroupCommand.ActionGroup.ToString());
            } else if (dc.BurnCommand != null) {
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

            } else if (dc.DriveCommand != null) {
                if (dc.DriveCommand.steering != 0) {
                    s.Append("Turn: ");
                    s.Append(dc.DriveCommand.target.ToString("0.0"));
                    if (dc.DriveCommand.steering < 0)
                        s.Append("° right @");
                    else
                        s.Append("° left @");
                    s.Append(Math.Abs(dc.DriveCommand.steering).ToString("P"));
                    s.Append(" steering");
                } else {
                    s.Append("Drive: ");
                    s.Append(RTUtil.FormatSI(dc.DriveCommand.target, "m"));
                    if (dc.DriveCommand.speed > 0)
                        s.Append(" forwards @");
                    else
                        s.Append(" backwards @");
                    s.Append(RTUtil.FormatSI(Math.Abs(dc.DriveCommand.speed), "m"));
                    s.Append("/s");
                }
            } else if (dc.Event != null) {
                s.Append(dc.Event.BaseEvent.listParent.part.partInfo.title);
                s.Append(": ");
                s.Append(dc.Event.BaseEvent.GUIName);
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
            }
            return s.ToString();
        }
    }
}