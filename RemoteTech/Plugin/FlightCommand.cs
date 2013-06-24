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

    public class FlightCommand : IComparable<FlightCommand> {
        public FlightMode Mode { get; set; }
        public FlightAttitude Attitude { get; set; }
        public ReferenceFrame Frame { get; set; }
        public Vector3 Direction { get; set; }

        public float Throttle { get; set; }
        public double Duration { get; set; }
        public double DeltaV { get; set; }

        public float AltitudeHold { get; set; }

        public double EffectiveFrom { get; set; }

        public FlightCommand(double effectiveTime) {
            Mode = FlightMode.Off;
            Attitude = FlightAttitude.Prograde;
            Frame = ReferenceFrame.Orbit;
            Direction = Vector3.zero;
            Throttle = Single.NaN;
            Duration = Double.NaN;
            DeltaV = Double.NaN;
            AltitudeHold = Single.NaN;
            EffectiveFrom = effectiveTime;
        }

        public int CompareTo(FlightCommand fc) {
            return EffectiveFrom.CompareTo(fc.EffectiveFrom);
        }

        public override String ToString() {
            StringBuilder s = new StringBuilder("Mode: ");
            switch (Mode) {
                case FlightMode.Off:
                    s.AppendLine("Off");
                    break;
                case FlightMode.KillRot:
                    s.AppendLine("Kill Rotation");
                    break;
                case FlightMode.AttitudeHold:
                    s.Append("Attitude, ");
                    switch (Attitude) {
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
                            s.Append("Surface (");
                            s.Append(Direction.x.ToString("F0"));
                            s.Append(", ");
                            s.Append(Direction.y.ToString("F0"));
                            s.Append(", ");
                            s.Append(Direction.z.ToString("F0"));
                            s.AppendLine(")");
                            break;
                    }
                    break;
                case FlightMode.AltitudeHold:
                    s.Append("Altitude, ");
                    s.Append(RTUtil.FormatSI(AltitudeHold, "m"));
                    break;
            }
            if (Throttle > 0) {
                s.Append("Burn (");
                s.Append(Throttle.ToString("P"));
                s.Append(") for ");
                s.Append(Duration.ToString("F1"));
                s.AppendLine("s");
            }
            double time = EffectiveFrom - RTUtil.GetGameTime();
            if (time > 0) {
                s.Append("Active in: ");
                s.Append(time.ToString("F2"));
                s.AppendLine("s");
            }
            return s.ToString().TrimEnd('\n');
        }
    }
}