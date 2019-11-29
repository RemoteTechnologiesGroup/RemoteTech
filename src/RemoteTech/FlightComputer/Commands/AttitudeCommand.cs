using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

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
        public static readonly Dictionary<FlightMode, String> FormatMode = new Dictionary<FlightMode, String>() 
        {
            { FlightMode.Off,        Localizer.Format("#RT_AttitudeCommand_Off") },//"Mode: Off"
            { FlightMode.KillRot,        Localizer.Format("#RT_AttitudeCommand_Killrotation") },//"Mode: Kill rotation"
            { FlightMode.AttitudeHold,        Localizer.Format("#RT_AttitudeCommand_Hold") +"{0} {1}" },//"Mode: Hold"
            { FlightMode.AltitudeHold,         Localizer.Format("#RT_AttitudeCommand_Hold") +"{0}" },//"Mode: Hold"
            { FlightMode.Rover,        "" },
        };

        public static readonly Dictionary<FlightAttitude, String> FormatAttitude = new Dictionary<FlightAttitude, String>() 
        {
            { FlightAttitude.Prograde,    Localizer.Format("#RT_AttitudeCommand_Prograde") },//"Prograde"
            { FlightAttitude.Retrograde,  Localizer.Format("#RT_AttitudeCommand_Retrograde") },//"Retrograde"
            { FlightAttitude.RadialMinus, Localizer.Format("#RT_AttitudeCommand_RadialMinus") },//"Radial -"
            { FlightAttitude.RadialPlus,  Localizer.Format("#RT_AttitudeCommand_RadialPlus") },//"Radial +"
            { FlightAttitude.NormalMinus, Localizer.Format("#RT_AttitudeCommand_NormalMinus") },//"Normal -"
            { FlightAttitude.NormalPlus,  Localizer.Format("#RT_AttitudeCommand_NormalPlus") },//"Normal +"
            { FlightAttitude.Surface,     Localizer.Format("#RT_AttitudeCommand_Direction") },//"Direction"
        };

        public static readonly Dictionary<ReferenceFrame, String> FormatReference = new Dictionary<ReferenceFrame, String>() 
        {
            { ReferenceFrame.Orbit,          Localizer.Format("#RT_AttitudeCommand_Orbit") },//"OBT"
            { ReferenceFrame.Surface,        Localizer.Format("#RT_AttitudeCommand_Surface") },//"SRF"
            { ReferenceFrame.TargetVelocity, Localizer.Format("#RT_AttitudeCommand_TargetVelocity") },//"RVEL"
            { ReferenceFrame.TargetParallel, Localizer.Format("#RT_AttitudeCommand_TargetParallel") },//"TGT"
            { ReferenceFrame.North,          Localizer.Format("#RT_AttitudeCommand_North") },//"North"
            { ReferenceFrame.Maneuver,       Localizer.Format("#RT_AttitudeCommand_Maneuver") },//"Maneuver"
            { ReferenceFrame.World,          Localizer.Format("#RT_AttitudeCommand_World") },//"World"
        };

        [Persistent] public FlightMode Mode;
        [Persistent] public FlightAttitude Attitude;
        [Persistent] public ReferenceFrame Frame;
        [Persistent] public Quaternion Orientation;
        [Persistent] public float Altitude;

        public override int Priority { get { return 0; } }

        public override string Description
        {
            get
            {
                return ShortName + Environment.NewLine + base.Description;
            }
        }
        public override string ShortName
        {
            get
            {
                String res = "";
                switch (Mode)
                {
                    default: res = FormatMode[Mode]; break;
                    case FlightMode.AltitudeHold: res = String.Format(FormatMode[Mode], RTUtil.FormatSI(Altitude, "m")); break;
                    case FlightMode.AttitudeHold:
                        if (Attitude == FlightAttitude.Surface)
                        {
                            res = String.Format(FormatMode[Mode], Orientation.eulerAngles.x.ToString("F1") + "°, " +
                                                                   (360 - Orientation.eulerAngles.y).ToString("F1") + "°, " +
                                                                   RTUtil.Format360To180(180 - Orientation.eulerAngles.z).ToString("F1") + "°", "");
                            break;
                        }
                        res = String.Format(FormatMode[Mode], FormatReference[Frame], FormatAttitude[Attitude]);
                        break;
                }
                return res;
            }
        }

        private bool mAbort;

        public override bool Pop(FlightComputer f)
        {
            if (Mode == FlightMode.KillRot)
            {
                Orientation = f.Vessel.transform.rotation;
            }
            f.PIDController.setPIDParameters(FlightComputer.PIDKp, FlightComputer.PIDKi, FlightComputer.PIDKd);
            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs)
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
                Altitude = Single.NaN,
                TimeStamp = RTUtil.GameTime,
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
                Altitude = Single.NaN,
                TimeStamp = RTUtil.GameTime,
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
                Altitude = Single.NaN,
                TimeStamp = (timetoexec == 0) ? RTUtil.GameTime:timetoexec,
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
                Altitude = Single.NaN,
                TimeStamp = RTUtil.GameTime,
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
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static AttitudeCommand WithSurface(double pitch, double yaw, double roll)
        {
            Quaternion rotation = Quaternion.Euler(new Vector3d(Double.IsNaN(pitch) ? 0 : pitch,
                                                                Double.IsNaN(yaw) ? 0 : -yaw,
                                                                Double.IsNaN(roll) ? 0 : 180 - roll));
            return new AttitudeCommand()
            {
                Mode = FlightMode.AttitudeHold,
                Attitude = FlightAttitude.Surface,
                Frame = ReferenceFrame.North,
                Orientation = rotation,
                Altitude = Single.NaN,
                TimeStamp = RTUtil.GameTime,
            };
        }

        /// <summary>
        /// Convert this AttitudeCommand values to a ComputerMode
        /// </summary>
        public SimpleTypes.ComputerModeMapper mapFlightMode()
        {
            SimpleTypes.ComputerModeMapper computerMode = new SimpleTypes.ComputerModeMapper();
            computerMode.mapFlightMode(Mode,Attitude,Frame);

            return computerMode;
        }
    }
}
