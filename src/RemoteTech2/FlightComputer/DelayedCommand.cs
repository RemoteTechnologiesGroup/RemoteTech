using System;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public enum FlightMode
    {
        Off,
        KillRot,
        AttitudeHold,
        AltitudeHold,
        Rover,
    }

    public enum FlightAttitude
    {
        Prograde,
        Retrograde,
        NormalPlus,
        NormalMinus,
        RadialPlus,
        RadialMinus,
        Surface,
    }

    public enum ReferenceFrame
    {
        Orbit,
        Surface,
        Target,
        North,
        Maneuver,
        World,
    }

    public class DelayedCommand : IComparable<DelayedCommand>
    {
        public double TimeStamp { get; set; }
        public double ExtraDelay { get; set; }
        public AttitudeCommand AttitudeCommand { get; set; }
        public BurnCommand BurnCommand { get; set; }
        public DriveCommand DriveCommand { get; set; }
        public ActionGroupCommand ActionGroupCommand { get; set; }
        public EventCommand EventCommand { get; set; }
        public DelayedCommand CancelCommand { get; set; }

        public int CompareTo(DelayedCommand dc)
        {
            return TimeStamp.CompareTo(dc.TimeStamp);
        }

        public static DelayedCommand Cancel(DelayedCommand dc)
        {
            return new DelayedCommand()
            {
                CancelCommand = dc,
                TimeStamp = RTUtil.GameTime,
            };
        }
    }

    public class DriveCommand
    {
        public enum DriveMode
        {
            Off,
            Turn,
            Distance,
            DistanceHeading,
            Coord
        }

        public float steering { get; set; }
        public float target { get; set; }
        public float target2 { get; set; }
        public float speed { get; set; }
        public DriveMode mode { get; set; }

        public static DelayedCommand Off()
        {
            return new DelayedCommand()
            {
                DriveCommand = new DriveCommand()
                {
                    mode = DriveMode.Off,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand Turn(float steering, float degrees, float speed)
        {
            return new DelayedCommand()
            {
                DriveCommand = new DriveCommand()
                {
                    mode = DriveMode.Turn,
                    steering = steering,
                    target = degrees,
                    speed = speed,

                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand Distance(float distance, float steerClamp, float speed)
        {
            return new DelayedCommand()
            {
                DriveCommand = new DriveCommand()
                {
                    mode = DriveMode.Distance,
                    steering = steerClamp,
                    target = distance,
                    speed = speed,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand DistanceHeading(float distance, float heading, float steerClamp, float speed)
        {
            return new DelayedCommand()
            {
                DriveCommand = new DriveCommand()
                {
                    mode = DriveMode.DistanceHeading,
                    steering = steerClamp,
                    target = distance,
                    target2 = heading,
                    speed = speed,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand Coord(float steerClamp, float latitude, float longitude, float speed)
        {
            return new DelayedCommand()
            {
                DriveCommand = new DriveCommand()
                {
                    mode = DriveMode.Coord,
                    steering = steerClamp,
                    target = latitude,
                    target2 = longitude,
                    speed = speed,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public void GetDescription(StringBuilder s)
        {
            switch (mode)
            {
                case DriveMode.Coord:
                    s.Append("Drive to: ");
                    s.Append(new Vector2(target, target2).ToString("0.000"));
                    s.Append(" @ ");
                    s.Append(RTUtil.FormatSI(Math.Abs(speed), "m/s"));
                    break;
                case DriveMode.Distance:
                    s.Append("Drive: ");
                    s.Append(RTUtil.FormatSI(target, "m"));
                    if (speed > 0)
                        s.Append(" forwards @");
                    else
                        s.Append(" backwards @");
                    s.Append(RTUtil.FormatSI(Math.Abs(speed), "m/s"));
                    break;
                case DriveMode.Turn:
                    s.Append("Turn: ");
                    s.Append(target.ToString("0.0"));
                    if (steering < 0)
                        s.Append("° right @");
                    else
                        s.Append("° left @");
                    s.Append(Math.Abs(steering).ToString("P"));
                    s.Append(" steering");
                    break;
                case DriveMode.DistanceHeading:
                    s.Append("Drive: ");
                    s.Append(RTUtil.FormatSI(target, "m"));
                    s.Append(", Hdg: ");
                    s.Append(target2.ToString("0"));
                    s.Append("° @ ");
                    s.Append(RTUtil.FormatSI(Math.Abs(speed), "m/s"));
                    break;
                case DriveMode.Off:
                    s.Append("Turn rover computer off");
                    break;
            }
        }

    }

    public class BurnCommand
    {
        public float Throttle { get; set; }
        public double Duration { get; set; }
        public double DeltaV { get; set; }

        public static DelayedCommand Off()
        {
            return new DelayedCommand()
            {
                BurnCommand = new BurnCommand()
                {
                    Throttle = Single.NaN,
                    Duration = 0,
                    DeltaV = 0,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand WithDuration(float throttle, double duration)
        {
            return new DelayedCommand()
            {
                BurnCommand = new BurnCommand()
                {
                    Throttle = throttle,
                    Duration = duration,
                    DeltaV = 0,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand WithDeltaV(float throttle, double delta)
        {
            return new DelayedCommand()
            {
                BurnCommand = new BurnCommand()
                {
                    Throttle = throttle,
                    Duration = 0,
                    DeltaV = delta,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }
    }

    public class AttitudeCommand
    {
        public FlightMode Mode { get; set; }
        public FlightAttitude Attitude { get; set; }
        public ReferenceFrame Frame { get; set; }
        public Quaternion Orientation { get; set; }
        public float Altitude { get; set; }

        public static AttitudeCommand WithKillrot(bool include)
        {
            if (include)
                return new AttitudeCommand()
                {
                    Mode = FlightMode.KillRot,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.World,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                };
            else
                return new AttitudeCommand()
                {
                    Mode = FlightMode.Off,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.World,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                };
        }


        public static DelayedCommand Off()
        {
            return new DelayedCommand()
            {
                AttitudeCommand = new AttitudeCommand()
                {
                    Mode = FlightMode.Off,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.World,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand KillRot()
        {
            return new DelayedCommand()
            {
                AttitudeCommand = new AttitudeCommand()
                {
                    Mode = FlightMode.KillRot,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.World,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand ManeuverNode()
        {
            return new DelayedCommand()
            {
                AttitudeCommand = new AttitudeCommand()
                {
                    Mode = FlightMode.AttitudeHold,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.Maneuver,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand WithAttitude(FlightAttitude att, ReferenceFrame frame)
        {
            return new DelayedCommand()
            {
                AttitudeCommand = new AttitudeCommand()
                {
                    Mode = FlightMode.AttitudeHold,
                    Attitude = att,
                    Frame = frame,
                    Orientation = Quaternion.identity,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand WithAltitude(float height)
        {
            return new DelayedCommand()
            {
                AttitudeCommand = new AttitudeCommand()
                {
                    Mode = FlightMode.AltitudeHold,
                    Attitude = FlightAttitude.Prograde,
                    Frame = ReferenceFrame.North,
                    Orientation = Quaternion.identity,
                    Altitude = height,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static DelayedCommand WithSurface(double pitch, double yaw, double roll)
        {
            Quaternion rotation = Quaternion.Euler(new Vector3d(Double.IsNaN(pitch) ? 0 : pitch,
                                                                Double.IsNaN(yaw) ? 0 : -yaw,
                                                                Double.IsNaN(roll) ? 0 : 180 - roll));
            return new DelayedCommand()
            {
                AttitudeCommand = new AttitudeCommand()
                {
                    Mode = FlightMode.AttitudeHold,
                    Attitude = FlightAttitude.Surface,
                    Frame = ReferenceFrame.North,
                    Orientation = rotation,
                    Altitude = Single.NaN,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }
    }

    public class ActionGroupCommand
    {
        public KSPActionGroup ActionGroup { get; private set; }

        public static DelayedCommand Group(KSPActionGroup group)
        {
            return new DelayedCommand()
            {
                ActionGroupCommand = new ActionGroupCommand()
                {
                    ActionGroup = group,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }
    }

    public class EventCommand
    {
        public BaseEvent BaseEvent { get; private set; }

        public static DelayedCommand Event(BaseEvent ev)
        {
            return new DelayedCommand()
            {
                EventCommand = new EventCommand()
                {
                    BaseEvent = ev,
                },
                TimeStamp = RTUtil.GameTime,
            };
        }
    }

    public class DelayedFlightCtrlState : IComparable<DelayedFlightCtrlState>
    {
        public FlightCtrlState State { get; private set; }
        public double TimeStamp { get; set; }

        public DelayedFlightCtrlState(FlightCtrlState fcs)
        {
            State = new FlightCtrlState();
            State.CopyFrom(fcs);
            TimeStamp = RTUtil.GameTime;
        }

        public int CompareTo(DelayedFlightCtrlState dfcs)
        {
            return TimeStamp.CompareTo(dfcs.TimeStamp);
        }
    }
}