using System;
using System.Text;
using UnityEngine;

namespace RemoteTech.FlightComputer.Commands
{
    public class DriveCommand : AbstractCommand
    {
        public enum DriveMode
        {
            Off,
            Turn,
            Distance,
            DistanceHeading,
            Coord
        }

        [Persistent] public float steering;
        [Persistent] public float target;
        [Persistent] public float target2;
        [Persistent] public float speed;
        [Persistent] public DriveMode mode;

        private bool mAbort;
        private RoverComputer mRoverComputer;

        public override void Abort() { mAbort = true; }

        public override bool Pop(FlightComputer f)
        {
            mRoverComputer = f.RoverComputer;
            mRoverComputer.InitMode(this);
            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs)
        {
            if (mAbort) {
                fcs.wheelThrottle = 0.0f;
                f.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                return true;
            }

            return f.RoverComputer.Drive(this, fcs);
        }

        public static DriveCommand Off()
        {
            return new DriveCommand() {
                mode = DriveMode.Off,
                TimeStamp = RTUtil.GameTime
            };
        }

        public static DriveCommand Turn(float steering, float degrees, float speed)
        {
            return new DriveCommand() {
                mode = DriveMode.Turn,
                steering = steering,
                target = degrees,
                speed = speed,
                TimeStamp = RTUtil.GameTime
            };
        }

        public static DriveCommand Distance(float distance, float steerClamp, float speed)
        {
            return new DriveCommand() {
                mode = DriveMode.Distance,
                steering = steerClamp,
                target = distance,
                speed = speed,
                TimeStamp = RTUtil.GameTime
            };
        }

        public static DriveCommand DistanceHeading(float distance, float heading, float steerClamp, float speed)
        {
            return new DriveCommand() {
                mode = DriveMode.DistanceHeading,
                steering = steerClamp,
                target = distance,
                target2 = heading,
                speed = speed,
                TimeStamp = RTUtil.GameTime
            };
        }

        public static DriveCommand Coord(float steerClamp, float latitude, float longitude, float speed)
        {
            return new DriveCommand() {
                mode = DriveMode.Coord,
                steering = steerClamp,
                target = latitude,
                target2 = longitude,
                speed = speed,
                TimeStamp = RTUtil.GameTime
            };
        }

        public override String Description
        {
            get
            {
                StringBuilder s = new StringBuilder();
                switch (mode) {
                    case DriveMode.Coord:
                        s.Append("Drive to: ");
                        s.Append(new Vector2(target, target2).ToString("0.000"));
                        s.Append(" @ ");
                        s.Append(RTUtil.FormatSI(Math.Abs(speed), "m/s"));
                        if (mRoverComputer != null) {
                            s.Append(" (");
                            s.Append(RTUtil.FormatSI(mRoverComputer.Delta, "m"));
                            s.Append(" ");
                            s.Append(RTUtil.FormatDuration(mRoverComputer.DeltaT, false));
                            s.Append(")"); ;
                        }
                        break;
                    case DriveMode.Distance:
                        s.Append("Drive: ");
                        s.Append(RTUtil.FormatSI(target, "m"));
                        if (speed > 0)
                            s.Append(" forwards @");
                        else
                            s.Append(" backwards @");
                        s.Append(RTUtil.FormatSI(Math.Abs(speed), "m/s"));
                        if (mRoverComputer != null) {
                            s.Append(" (");
                            s.Append(RTUtil.FormatSI(mRoverComputer.Delta, "m"));
                            s.Append(" ");
                            s.Append(RTUtil.FormatDuration(mRoverComputer.DeltaT, false));
                            s.Append(")"); ;
                        }
                        break;
                    case DriveMode.Turn:
                        s.Append("Turn: ");
                        s.Append(target.ToString("0.0"));
                        if (steering < 0)
                            s.Append("° right @");
                        else
                            s.Append("° left @");
                        s.Append(Math.Abs(steering).ToString("P"));
                        s.Append(" Steering");
                        if (mRoverComputer != null) {
                            s.Append(" (");
                            s.Append(mRoverComputer.Delta.ToString("F2"));
                            s.Append("° ");
                            s.Append(RTUtil.FormatDuration(mRoverComputer.DeltaT, false));
                            s.Append(")"); ;
                        }
                        break;
                    case DriveMode.DistanceHeading:
                        s.Append("Drive: ");
                        s.Append(RTUtil.FormatSI(target, "m"));
                        s.Append(", Hdg: ");
                        s.Append(target2.ToString("0"));
                        s.Append("° @ ");
                        s.Append(RTUtil.FormatSI(Math.Abs(speed), "m/s"));
                        if (mRoverComputer != null) {
                            s.Append(" (");
                            s.Append(RTUtil.FormatSI(mRoverComputer.Delta, "m"));
                            s.Append(" ");
                            s.Append(RTUtil.FormatDuration(mRoverComputer.DeltaT, false));
                            s.Append(")");
                        }
                        break;
                    case DriveMode.Off:
                        s.Append("Turn rover computer off");
                        break;
                }

                return s.ToString() + Environment.NewLine + base.Description;
            }
        }

        public override string ShortName
        {
            get { return "Drive"; }
        }

    }
}
