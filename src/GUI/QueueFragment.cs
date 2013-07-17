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
                return delay;
            }
            set { mExtraDelay = value.ToString(); }
        }

        public QueueFragment(FlightComputer fc) {
            mFlightComputer = fc;
            mExtraDelay = "0";
        }

        public void Draw() {
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
                        s.Append("Mode: Hold attitude, ");
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
                                s.Append("Surface: ");
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.x.ToString("F2"));
                                s.Append(", ");
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.y.ToString("F2"));
                                s.Append(", ");
                                s.Append(dc.AttitudeCommand.Orientation.eulerAngles.z.ToString("F2"));
                                break;
                        }
                        break;
                    case FlightMode.AltitudeHold:
                        s.Append("Mode: Hold attitude, ");
                        s.Append(RTUtil.FormatSI(dc.AttitudeCommand.Altitude, "m"));
                        break;
                }
            } else if (dc.ActionGroupCommand != null) {
                s.Append("Toggle ");
                s.Append(dc.ActionGroupCommand.ActionGroup.ToString());
            } else if (dc.BurnCommand != null) {
                s.Append("Burn ");
                s.Append(dc.BurnCommand.Throttle.ToString("P2"));
                s.Append(", ");
                s.Append(dc.BurnCommand.Duration.ToString("F2"));
                s.Append("s");
                s.Append(", ");
                s.Append(dc.BurnCommand.DeltaV.ToString("F2"));
                s.Append("m/s");
            }

            double delay = dc.TimeStamp - RTUtil.GetGameTime();
            if (delay > 0) {
                s.AppendLine();
                s.Append("Active in: ");
                s.Append(delay.ToString("F2"));
                s.Append("s");
            }
            return s.ToString();
        }
    }
}