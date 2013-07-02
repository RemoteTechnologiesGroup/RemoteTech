using System;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public enum FlightMode {
        Off,
        KillRot,
        AttitudeHold,
        AltitudeHold,
    }

    public enum FlightAttitude {
        Prograde,
        Retrograde,
        NormalPlus,
        NormalMinus,
        RadialPlus,
        RadialMinus,
        Surface,
    }

    public enum ReferenceFrame {
        Orbit,
        Surface,
        Target,
        North,
        Maneuver,
        World,
    }

    public class DelayedCommand {
        public double TimeStamp { get; set; }
        public double ExtraDelay { get; set; }

        public int CompareTo(DelayedCommand dc) {
            return TimeStamp.CompareTo(dc.TimeStamp);
        }
    }

    public class FlightCommand : DelayedCommand {
        public FlightMode Mode { get; set; }
        public FlightAttitude Attitude { get; set; }
        public ReferenceFrame Frame { get; set; }
        public Vector3 Direction { get; set; }

        public float Throttle { get; set; }
        public double Duration { get; set; }
        public double DeltaV { get; set; }

        public float AltitudeHold { get; set; }

        public FlightCommand(double effectiveTime) {
            Mode = FlightMode.Off;
            Attitude = FlightAttitude.Prograde;
            Frame = ReferenceFrame.Orbit;
            Direction = Vector3.zero;
            Throttle = Single.NaN;
            Duration = Double.NaN;
            DeltaV = Double.NaN;
            AltitudeHold = Single.NaN;

            TimeStamp = effectiveTime;
            ExtraDelay = 0;
        }

        public override String ToString() {
            StringBuilder s = new StringBuilder("Mode: ");
            switch (Mode) {
                case FlightMode.Off:
                    s.Append("Off");
                    break;
                case FlightMode.KillRot:
                    s.Append("Kill Rotation");
                    break;
                case FlightMode.AttitudeHold:
                    s.Append("Attitude, ");
                    switch (Attitude) {
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
                            s.Append("Surface (");
                            s.Append(Direction.x.ToString("F0"));
                            s.Append(", ");
                            s.Append(Direction.y.ToString("F0"));
                            s.Append(", ");
                            s.Append(Direction.z.ToString("F0"));
                            s.Append(")");
                            break;
                    }
                    break;
                case FlightMode.AltitudeHold:
                    s.Append("Altitude, ");
                    s.Append(RTUtil.FormatSI(AltitudeHold, "m"));
                    break;
            }
            if (Throttle > 0) {
                s.AppendLine();
                s.Append("Burn (");
                s.Append(Throttle.ToString("P"));
                s.Append(") for ");
                s.Append(RTUtil.FormatDuration(Duration));
            }
            double time = TimeStamp - RTUtil.GetGameTime();
            if (time > 0) {
                s.AppendLine();
                s.Append("Signal delay: ");
                s.Append(RTUtil.FormatDuration(time));
                if(ExtraDelay > 0) {
                    s.Append(" with extra delay ");
                    s.Append(RTUtil.FormatDuration(ExtraDelay));
                }
            } else if (ExtraDelay > 0) {
                s.AppendLine();
                s.Append("Delay: ");
                s.Append(RTUtil.FormatDuration(ExtraDelay));
            }
            return s.ToString();
        }
    }
}