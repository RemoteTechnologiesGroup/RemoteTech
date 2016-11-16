using System;
using RemoteTech.Common;
using RemoteTech.Common.Utils;
using UnityEngine;

namespace RemoteTech.FlightComputer
{
    public class RoverComputer
    {
        private Vessel _vessel;
        private readonly RoverPIDController _throttlePid;
        private readonly RoverPIDController _wheelPid;

        private float _roverAltitude;
        private float _roverLatitude;
        private float _roverLongitude;
        private float _targetLatitude;
        private float _targetLongitude;
        private float _brakeDistance;

        private Quaternion _roverRotation;

        private Vector3 _forwardAxis;

        public float Delta { get; private set; }
        public float DeltaT { get; private set; }

        public RoverComputer()
        {
            _throttlePid = new RoverPIDController(10f, 1e-5F, 1e-5F, -1f, 1f);
            _wheelPid = new RoverPIDController(10f, 1e-5F, 1e-5F, -1f, 1f, 5f);
        }

        public void SetVessel(Vessel v)
        {
            _vessel = v;
        }

        private float RoverHDG
        {
            get
            {
                var up = (_vessel.CoM - _vessel.mainBody.position).normalized;
                var north = Vector3d.Exclude(up, (_vessel.mainBody.position + _vessel.mainBody.transform.up * (float)_vessel.mainBody.Radius) - _vessel.CoM).normalized;

                if (_forwardAxis == Vector3.zero)
                    return GetHeading(_vessel.srf_velocity.normalized, up, north);

                return GetHeading(_vessel.ReferenceTransform.TransformDirection(_forwardAxis), up, north);
            }
        }

        private float TargetHDG
        {
            get
            {
                var up = (_vessel.CoM - _vessel.mainBody.position).normalized;
                var north = Vector3d.Exclude(up, (_vessel.mainBody.position + _vessel.mainBody.transform.up * (float)_vessel.mainBody.Radius) - _vessel.CoM).normalized;
                return GetHeading((TargetPos - _vessel.CoM).normalized, up, north);
            }
        }

        private Vector3 TargetPos => _vessel.mainBody.GetWorldSurfacePosition(_targetLatitude, _targetLongitude, _vessel.altitude);

        private Vector3 RoverOrigPos => _vessel.mainBody.GetWorldSurfacePosition(_roverLatitude, _roverLongitude, _roverAltitude);

        private float RoverSpeed
        {
            get
            {
                if (_forwardAxis == Vector3.zero)
                    return (float)_vessel.srf_velocity.magnitude;

                return Vector3.Dot(_vessel.srf_velocity, _vessel.ReferenceTransform.TransformDirection(_forwardAxis));
            }
        }

        public void InitMode(Commands.DriveCommand dc)
        {
            if (_vessel == null) {
                RTLog.Notify("RoverComputer.InitMode(): vessel is null!", RTLogLevel.LVL4);
                return;
            }

            _forwardAxis = Vector3.zero;
            _roverAltitude = (float)_vessel.altitude;
            _roverLatitude = (float)_vessel.latitude;
            _roverLongitude = (float)_vessel.longitude;
            Delta = 0;
            DeltaT = 0;
            _brakeDistance = 0;

            switch (dc.mode) {
                case Commands.DriveCommand.DriveMode.Turn:
                    _roverRotation = _vessel.ReferenceTransform.rotation;
                    break;
                case Commands.DriveCommand.DriveMode.Distance:
                    _wheelPid.setClamp(-1, 1);
                    break;
                case Commands.DriveCommand.DriveMode.DistanceHeading:
                    _wheelPid.setClamp(-dc.steering, dc.steering);
                    break;
                case Commands.DriveCommand.DriveMode.Coord:
                    _wheelPid.setClamp(-dc.steering, dc.steering);
                    _targetLatitude = dc.target;
                    _targetLongitude = dc.target2;
                    break;
            }
            _throttlePid.Reset();
            _wheelPid.Reset();
            _vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
        }

        public bool Drive(Commands.DriveCommand dc, FlightCtrlState fs)
        {
            if (dc == null)
                return true;

            if (_vessel.srf_velocity.magnitude > 0.5) {
                var degForward = Mathf.Abs(ClampUtil.ClampDegrees90(Vector3.Angle(_vessel.srf_velocity, _vessel.ReferenceTransform.forward)));
                var degUp = Mathf.Abs(ClampUtil.ClampDegrees90(Vector3.Angle(_vessel.srf_velocity, _vessel.ReferenceTransform.up)));
                var degRight = Mathf.Abs(ClampUtil.ClampDegrees90(Vector3.Angle(_vessel.srf_velocity, _vessel.ReferenceTransform.right)));

                if (degForward < degUp && degForward < degRight)
                    _forwardAxis = Vector3.forward;
                else if (degRight < degUp && degRight < degForward)
                    _forwardAxis = Vector3.right;
                else
                    _forwardAxis = Vector3.up;
            }

            switch (dc.mode) {
                case Commands.DriveCommand.DriveMode.Turn:
                    return Turn(dc, fs);
                case Commands.DriveCommand.DriveMode.Distance:
                    return Distance(dc, fs);
                case Commands.DriveCommand.DriveMode.DistanceHeading:
                    return DistanceHeading(dc, fs);
                case Commands.DriveCommand.DriveMode.Coord:
                    return Coord(dc, fs);
            }
            return true;
        }

        private bool Turn(Commands.DriveCommand dc, FlightCtrlState fs)
        {
            Delta = Math.Abs(Quaternion.Angle(_roverRotation, _vessel.ReferenceTransform.rotation));
            DeltaT = Delta / _vessel.GetComponent<Rigidbody>().angularVelocity.magnitude;
            if (Delta < dc.target)
            {
                fs.wheelThrottle = _throttlePid.Control(dc.speed - RoverSpeed);
                fs.wheelSteer = dc.steering;
                return false;
            }

            fs.wheelThrottle = 0;
            fs.wheelSteer = 0;
            _vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            dc.mode = Commands.DriveCommand.DriveMode.Off;
            return true;
        }

        private bool Distance(Commands.DriveCommand dc, FlightCtrlState fs)
        {
            var speed = RoverSpeed;
            Delta = Math.Abs(dc.target) - Vector3.Distance(RoverOrigPos, _vessel.CoM);
            DeltaT = Delta / Math.Abs(speed);
            if (Delta > 0)
            {
                fs.wheelThrottle = _throttlePid.Control(BrakeSpeed(dc.speed, speed) - speed);
                return false;
            }

            fs.wheelThrottle = 0;
            fs.wheelSteer = 0;
            _vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            dc.mode = Commands.DriveCommand.DriveMode.Off;
            return true;
        }

        private bool DistanceHeading(Commands.DriveCommand dc, FlightCtrlState fs)
        {
            var speed = RoverSpeed;
            Delta = Math.Abs(dc.target) - Vector3.Distance(RoverOrigPos, _vessel.CoM);
            DeltaT = Delta / speed;
            if (Delta > 0)
            {
                fs.wheelThrottle = _throttlePid.Control(BrakeSpeed(dc.speed, speed) - speed);
                if (_forwardAxis != Vector3.zero)
                    fs.wheelSteer = _wheelPid.Control(ClampUtil.AngleBetween(RoverHDG, dc.target2));
                return false;
            }

            fs.wheelThrottle = 0;
            fs.wheelSteer = 0;
            _vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            dc.mode = Commands.DriveCommand.DriveMode.Off;
            return true;
        }

        public static float GetHeading(Vector3 dir, Vector3 up, Vector3 north)
        {
            return Quaternion.Inverse(Quaternion.Inverse(Quaternion.LookRotation(dir, up)) * Quaternion.LookRotation(north, up)).eulerAngles.y;
        }

        private float BrakeSpeed(float speed, float actualSpeed)
        {
            if (Math.Abs(_brakeDistance) < float.Epsilon)
            {
                if (DeltaT > actualSpeed / 2)
                {
                    return speed;
                }

                _brakeDistance = Delta;
            }
            return Math.Max(Math.Min(speed, (float)Math.Sqrt(Delta / _brakeDistance) * speed), speed / 10);
        }

        private bool Coord(Commands.DriveCommand dc, FlightCtrlState fs)
        {
            var deg = ClampUtil.AngleBetween(RoverHDG, TargetHDG);
            var speed = RoverSpeed;

            Delta = Vector3.Distance(_vessel.CoM, TargetPos);
            DeltaT = Delta / speed;

            if (Delta > Math.Abs(deg) / 36)
            {
                fs.wheelThrottle = _throttlePid.Control(BrakeSpeed(dc.speed, speed) - speed);
                if (_forwardAxis != Vector3.zero)
                    fs.wheelSteer = _wheelPid.Control(deg);
                return false;
            }

            fs.wheelThrottle = 0;
            fs.wheelSteer = 0;
            _vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
            dc.mode = Commands.DriveCommand.DriveMode.Off;
            return true;
        }
    }
}
