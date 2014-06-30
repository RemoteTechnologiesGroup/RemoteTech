using System;
using System.Text;
using UnityEngine;

namespace RemoteTech
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

        public float steering { get; set; }
        public float target { get; set; }
        public float target2 { get; set; }
        public float speed { get; set; }
        public DriveMode mode { get; set; }

        private bool mAbort;

        public override void Abort() { mAbort = true; }

        public override bool Pop(FlightComputer f)
        {
            f.mRoverComputer.InitMode(this);
            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs)
        {
            if (mAbort)
            {
                fcs.wheelThrottle = 0.0f;
                return true;
            }

            return f.mRoverComputer.Drive(this, fcs);
        }

        public static DriveCommand Off()
        {
            return new DriveCommand()
            {
                mode = DriveMode.Off,
                TimeStamp = RTUtil.GameTime
            };
        }

        public static DriveCommand Turn(float steering, float degrees, float speed)
        {
            return new DriveCommand()
            {
                mode = DriveMode.Turn,
                steering = steering,
                target = degrees,
                speed = speed,
                TimeStamp = RTUtil.GameTime
            };
        }

        public static DriveCommand Distance(float distance, float steerClamp, float speed)
        {
            return new DriveCommand()
            {
                mode = DriveMode.Distance,
                steering = steerClamp,
                target = distance,
                speed = speed,
                TimeStamp = RTUtil.GameTime
            };
        }

        public static DriveCommand DistanceHeading(float distance, float heading, float steerClamp, float speed)
        {
            return new DriveCommand()
            {
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
            return new DriveCommand()
            {
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

                return s.ToString() + Environment.NewLine + base.Description;
            }
        }

    }
}
