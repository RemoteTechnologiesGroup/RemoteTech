using System;
using System.Text;
using RemoteTech.Common.Utils;
using UnityEngine;
using RemoteTech.Common.Interfaces.FlightComputer;
using RemoteTech.Common.Interfaces.FlightComputer.Commands;

namespace RemoteTech.FlightComputer.Commands
{

    public class DriveCommand : AbstractCommand, IDriveCommand
    {

        [Persistent] private float _steering;
        [Persistent] private float _target;
        [Persistent] private float _target2;
        [Persistent] private float _speed;
        [Persistent] private DriveMode _mode;

        private bool _abort;
        private RoverComputer _roverComputer;

        public float Steering => _steering;
        public float Target => _target;
        public float Target2 => _target2;
        public float Speed => _speed;

        public DriveMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }


        public override void Abort() { _abort = true; }

        public override bool Pop(IFlightComputer f)
        {
            // TODO this is a patch for keeping compiler errors down; should be reworked in 2.x branch
            var computer = f as FlightComputer;
            if (computer == null)
                return true;

            _roverComputer = computer.RoverComputer;
            _roverComputer.InitMode(this);
            return true;
        }

        public override bool Execute(IFlightComputer f, FlightCtrlState fcs)
        {
            if (_abort) {
                fcs.wheelThrottle = 0.0f;
                f.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                return true;
            }

            // TODO this is a patch for keeping compiler errors down; should be reworked in 2.x branch
            var computer = f as FlightComputer;
            if (computer == null)
                return true;

            return computer.RoverComputer.Drive(this, fcs);
        }

        public static DriveCommand Off()
        {
            return new DriveCommand() {
                _mode = DriveMode.Off,
                TimeStamp = TimeUtil.GameTime
            };
        }

        public static DriveCommand Turn(float steering, float degrees, float speed)
        {
            return new DriveCommand() {
                _mode = DriveMode.Turn,
                _steering = steering,
                _target = degrees,
                _speed = speed,
                TimeStamp = TimeUtil.GameTime
            };
        }

        public static DriveCommand Distance(float distance, float steerClamp, float speed)
        {
            return new DriveCommand() {
                _mode = DriveMode.Distance,
                _steering = steerClamp,
                _target = distance,
                _speed = speed,
                TimeStamp = TimeUtil.GameTime
            };
        }

        public static DriveCommand DistanceHeading(float distance, float heading, float steerClamp, float speed)
        {
            return new DriveCommand() {
                _mode = DriveMode.DistanceHeading,
                _steering = steerClamp,
                _target = distance,
                _target2 = heading,
                _speed = speed,
                TimeStamp = TimeUtil.GameTime
            };
        }

        public static DriveCommand Coord(float steerClamp, float latitude, float longitude, float speed)
        {
            return new DriveCommand() {
                _mode = DriveMode.Coord,
                _steering = steerClamp,
                _target = latitude,
                _target2 = longitude,
                _speed = speed,
                TimeStamp = TimeUtil.GameTime
            };
        }

        public override string Description
        {
            get
            {
                var s = new StringBuilder();
                switch (_mode) {
                    case DriveMode.Coord:
                        s.Append("Drive to: ");
                        s.Append(new Vector2(_target, _target2).ToString("0.000"));
                        s.Append(" @ ");
                        s.Append(FormatUtil.FormatSI(Math.Abs(_speed), "m/s"));
                        if (_roverComputer != null) {
                            s.Append(" (");
                            s.Append(FormatUtil.FormatSI(_roverComputer.Delta, "m"));
                            s.Append(" ");
                            s.Append(TimeUtil.FormatDuration(_roverComputer.DeltaT, false));
                            s.Append(")"); ;
                        }
                        break;
                    case DriveMode.Distance:
                        s.Append("Drive: ");
                        s.Append(FormatUtil.FormatSI(_target, "m"));
                        if (_speed > 0)
                            s.Append(" forwards @");
                        else
                            s.Append(" backwards @");
                        s.Append(FormatUtil.FormatSI(Math.Abs(_speed), "m/s"));
                        if (_roverComputer != null) {
                            s.Append(" (");
                            s.Append(FormatUtil.FormatSI(_roverComputer.Delta, "m"));
                            s.Append(" ");
                            s.Append(TimeUtil.FormatDuration(_roverComputer.DeltaT, false));
                            s.Append(")"); ;
                        }
                        break;
                    case DriveMode.Turn:
                        s.Append("Turn: ");
                        s.Append(_target.ToString("0.0"));
                        if (_steering < 0)
                            s.Append("° right @");
                        else
                            s.Append("° left @");
                        s.Append(Math.Abs(_steering).ToString("P"));
                        s.Append(" Steering");
                        if (_roverComputer != null) {
                            s.Append(" (");
                            s.Append(_roverComputer.Delta.ToString("F2"));
                            s.Append("° ");
                            s.Append(TimeUtil.FormatDuration(_roverComputer.DeltaT, false));
                            s.Append(")"); ;
                        }
                        break;
                    case DriveMode.DistanceHeading:
                        s.Append("Drive: ");
                        s.Append(FormatUtil.FormatSI(_target, "m"));
                        s.Append(", Hdg: ");
                        s.Append(_target2.ToString("0"));
                        s.Append("° @ ");
                        s.Append(FormatUtil.FormatSI(Math.Abs(_speed), "m/s"));
                        if (_roverComputer != null) {
                            s.Append(" (");
                            s.Append(FormatUtil.FormatSI(_roverComputer.Delta, "m"));
                            s.Append(" ");
                            s.Append(TimeUtil.FormatDuration(_roverComputer.DeltaT, false));
                            s.Append(")");
                        }
                        break;
                    case DriveMode.Off:
                        s.Append("Turn rover computer off");
                        break;
                }

                return s + Environment.NewLine + base.Description;
            }
        }

        public override string ShortName => "Drive";
    }
}
