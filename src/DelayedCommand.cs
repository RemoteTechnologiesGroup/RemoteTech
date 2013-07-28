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

    public class DelayedCommand : IComparable<DelayedCommand> {
        public double TimeStamp { get; set; }
        public double ExtraDelay { get; set; }
        public AttitudeCommand AttitudeCommand { get; set; }
        public BurnCommand BurnCommand { get; set; }
        public ActionGroupCommand ActionGroupCommand { get; set; }
        public EventCommand Event { get; set; }

        public int CompareTo(DelayedCommand dc) {
            return TimeStamp.CompareTo(dc.TimeStamp);
        }
    }

    public class BurnCommand {
        public float Throttle { get; set; }
        public double Duration { get; set; }
        public double DeltaV { get; set; }

        public static DelayedCommand Off() {
            return new DelayedCommand() {
                BurnCommand = new BurnCommand() {
                    Throttle = Single.NaN,
                    Duration = 0,
                    DeltaV = 0,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }

        public static DelayedCommand WithDuration(float throttle, double duration) {
            return new DelayedCommand() {
                BurnCommand = new BurnCommand() {
                    Throttle = throttle,
                    Duration = duration,
                    DeltaV = 0,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }

        public static DelayedCommand WithDeltaV(float throttle, double delta) {
            return new DelayedCommand() {
                BurnCommand = new BurnCommand() {
                    Throttle = throttle,
                    Duration = 0,
                    DeltaV = delta,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }
    }

    public class AttitudeCommand {
        public FlightMode Mode { get; set; }
        public FlightAttitude Attitude { get; set; }
        public ReferenceFrame Frame { get; set; }
        public Quaternion Orientation { get; set; }
        public float Altitude { get; set; }

        public static DelayedCommand Off() {
            return new DelayedCommand() {
                AttitudeCommand = new AttitudeCommand() {
                    Mode = FlightMode.Off,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.World,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }

        public static DelayedCommand KillRot() {
            return new DelayedCommand() {
                AttitudeCommand = new AttitudeCommand() {
                    Mode = FlightMode.KillRot,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.World,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }

        public static DelayedCommand ManeuverNode() {
            return new DelayedCommand() {
                AttitudeCommand = new AttitudeCommand() {
                    Mode = FlightMode.AttitudeHold,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.Maneuver,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }

        public static DelayedCommand WithAttitude(FlightAttitude att, ReferenceFrame frame) {
            return new DelayedCommand() {
                AttitudeCommand = new AttitudeCommand() {
                    Mode = FlightMode.AttitudeHold,
                    Attitude = att,
                    Frame = frame,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }

        public static DelayedCommand WithAltitude(float height) {
            return new DelayedCommand() {
                AttitudeCommand = new AttitudeCommand() {
                    Mode = FlightMode.AltitudeHold,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.North,
                    Orientation = Quaternion.identity,
                    Altitude = height,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }

        public static DelayedCommand WithSurface(double pitch, double yaw, double roll) {
            Quaternion rotation = Quaternion.Euler(new Vector3d(Double.IsNaN(pitch) ? 0 : pitch,
                                                                Double.IsNaN(yaw) ? 0 : yaw,
                                                                Double.IsNaN(roll) ? 0 : roll));
            return new DelayedCommand() {
                AttitudeCommand = new AttitudeCommand() {
                    Mode = FlightMode.AttitudeHold,
                    Attitude = FlightAttitude.Surface,
                    Frame = ReferenceFrame.North,
                    Orientation = rotation,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GetGameTime()
            };
        }
    }

    public class ActionGroupCommand {
        public KSPActionGroup ActionGroup { get; private set; }

        public static DelayedCommand Group(KSPActionGroup group) {
            return new DelayedCommand() {
                ActionGroupCommand = new ActionGroupCommand() {
                    ActionGroup = group,
                },
                TimeStamp = RTUtil.GetGameTime(),
            };
        }
    }

    public class EventCommand {
        public BaseEvent BaseEvent { get; private set; }

        public static DelayedCommand Event(BaseEvent ev) {
            return new DelayedCommand() {
                Event = new EventCommand() {
                    BaseEvent = ev,
                },
                TimeStamp = RTUtil.GetGameTime(),
            };
        }
    }

    public class DelayedFlightCtrlState : IComparable<DelayedFlightCtrlState> {
        public FlightCtrlState State { get; private set; }
        public double TimeStamp { get; set; }

        public DelayedFlightCtrlState(FlightCtrlState fcs) {
            State = new FlightCtrlState();
            State.CopyFrom(fcs);
            TimeStamp = RTUtil.GetGameTime();
        }

        public int CompareTo(DelayedFlightCtrlState dfcs) {
            return TimeStamp.CompareTo(dfcs.TimeStamp);
        }
    }
}