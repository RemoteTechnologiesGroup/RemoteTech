using System;
using System.Collections.Generic;
using RemoteTech.Common.Utils;
using UnityEngine;
using RemoteTech.Common.Interfaces.FlightComputer;

namespace RemoteTech.FlightComputer.Commands
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
        Null,
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
        TargetVelocity,
        TargetParallel,
        North,
        Maneuver,
        World,
    }

    public class AttitudeCommand : AbstractCommand
    {
        public static readonly Dictionary<FlightMode, string> FormatMode = new Dictionary<FlightMode, string>() 
        {
            { FlightMode.Off,          "Mode: Off" },
            { FlightMode.KillRot,      "Mode: Kill rotation" },
            { FlightMode.AttitudeHold, "Mode: Hold {0} {1}" },
            { FlightMode.AltitudeHold, "Mode: Hold {0}" },
            { FlightMode.Rover,        "" },
        };

        public static readonly Dictionary<FlightAttitude, string> FormatAttitude = new Dictionary<FlightAttitude, string>() 
        {
            { FlightAttitude.Prograde,    "Prograde" },
            { FlightAttitude.Retrograde,  "Retrograde" },
            { FlightAttitude.RadialMinus, "Radial -" },
            { FlightAttitude.RadialPlus,  "Radial +" },
            { FlightAttitude.NormalMinus, "Normal -" },
            { FlightAttitude.NormalPlus,  "Normal +" },
            { FlightAttitude.Surface,     "Direction" },
        };

        public static readonly Dictionary<ReferenceFrame, string> FormatReference = new Dictionary<ReferenceFrame, string>() 
        {
            { ReferenceFrame.Orbit,          "OBT" },
            { ReferenceFrame.Surface,        "SRF" },
            { ReferenceFrame.TargetVelocity, "RVEL" },
            { ReferenceFrame.TargetParallel, "TGT" },
            { ReferenceFrame.North,          "North" },
            { ReferenceFrame.Maneuver,       "Maneuver" },
            { ReferenceFrame.World,          "World" },
        };

        [Persistent] public FlightMode Mode;
        [Persistent] public FlightAttitude Attitude;
        [Persistent] public ReferenceFrame Frame;
        [Persistent] public Quaternion Orientation;
        [Persistent] public float Altitude;

        public override int Priority => 0;

        public override string Description => ShortName + Environment.NewLine + base.Description;

        public override string ShortName
        {
            get
            {
                string res;
                switch (Mode)
                {
                    default: res = FormatMode[Mode]; break;
                    case FlightMode.AltitudeHold: res = string.Format(FormatMode[Mode], FormatUtil.FormatSI(Altitude, "m")); break;
                    case FlightMode.AttitudeHold:
                        if (Attitude == FlightAttitude.Surface)
                        {
                            res = string.Format(FormatMode[Mode], Orientation.eulerAngles.x.ToString("F1") + "°, " +
                                                                   (360 - Orientation.eulerAngles.y).ToString("F1") + "°, " +
                                                                   FormatUtil.Format360To180(180 - Orientation.eulerAngles.z).ToString("F1") + "°", "");
                            break;
                        }
                        res = string.Format(FormatMode[Mode], FormatReference[Frame], FormatAttitude[Attitude]);
                        break;
                }
                return res;
            }
        }

        private bool mAbort;

        public override bool Pop(IFlightComputer f)
        {
            if (Mode == FlightMode.KillRot)
            {
                Orientation = f.Vessel.transform.rotation;
            }
            return true;
        }

        public override bool Execute(IFlightComputer f, FlightCtrlState fcs)
        {
            if (mAbort)
            {
                Mode = FlightMode.Off;
                mAbort = false;
            }

            switch (Mode)
            {
                case FlightMode.Off:
                    break;
                case FlightMode.KillRot:
                    FlightCore.HoldOrientation(fcs, f, Orientation * Quaternion.AngleAxis(90, Vector3.left));
                    break;
                case FlightMode.AttitudeHold:
                    FlightCore.HoldAttitude(fcs, f, Frame, Attitude, Orientation);
                    break;
                case FlightMode.AltitudeHold:
                    break;
            }

            return false;
        }

        public override void Abort() { mAbort = true; }

        public static AttitudeCommand Off()
        {
            return new AttitudeCommand()
            {
                Mode = FlightMode.Off,
                Attitude = FlightAttitude.Prograde,
                Frame = ReferenceFrame.World,
                Orientation = Quaternion.identity,
                Altitude = float.NaN,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static AttitudeCommand KillRot()
        {
            return new AttitudeCommand()
            {
                Mode = FlightMode.KillRot,
                Attitude = FlightAttitude.Prograde,
                Frame = ReferenceFrame.World,
                Orientation = Quaternion.identity,
                Altitude = float.NaN,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static AttitudeCommand ManeuverNode(double timetoexec = 0)
        {
            return new AttitudeCommand()
            {
                Mode = FlightMode.AttitudeHold,
                Attitude = FlightAttitude.Prograde,
                Frame = ReferenceFrame.Maneuver,
                Orientation = Quaternion.identity,
                Altitude = float.NaN,
                TimeStamp = (Math.Abs(timetoexec) < float.Epsilon) ? TimeUtil.GameTime : timetoexec,
            };
        }

        public static AttitudeCommand WithAttitude(FlightAttitude att, ReferenceFrame frame)
        {
            return new AttitudeCommand()
            {
                Mode = FlightMode.AttitudeHold,
                Attitude = att,
                Frame = frame,
                Orientation = Quaternion.identity,
                Altitude = float.NaN,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static AttitudeCommand WithAltitude(float height)
        {
            return new AttitudeCommand()
            {
                Mode = FlightMode.AltitudeHold,
                Attitude = FlightAttitude.Prograde,
                Frame = ReferenceFrame.North,
                Orientation = Quaternion.identity,
                Altitude = height,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static AttitudeCommand WithSurface(double pitch, double yaw, double roll)
        {
            var rotation = Quaternion.Euler(new Vector3d(double.IsNaN(pitch) ? 0 : pitch,
                                                                double.IsNaN(yaw) ? 0 : -yaw,
                                                                double.IsNaN(roll) ? 0 : 180 - roll));
            return new AttitudeCommand()
            {
                Mode = FlightMode.AttitudeHold,
                Attitude = FlightAttitude.Surface,
                Frame = ReferenceFrame.North,
                Orientation = rotation,
                Altitude = float.NaN,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        /// <summary>
        /// Convert this AttitudeCommand values to a ComputerMode
        /// </summary>
        public ComputerModeMapper MapFlightMode()
        {
            var computerMode = new ComputerModeMapper();
            computerMode.MapFlightMode(Mode,Attitude,Frame);

            return computerMode;
        }
    }
}
