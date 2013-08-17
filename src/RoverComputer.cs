using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    class RoverComputer {
        private Vessel mVessel;
        private Legacy.PidController mThrottlePID, mWheelPID;

        private float
            mRoverAlt,
            mRoverLat,
            mRoverLon,
            mTargetLat,
            mTargetLon,
            mKeepHDG;

        private Quaternion mRoverRot;

        private ModuleWheel.AlignmentAxis mAxis;

        public RoverComputer(Vessel v) {
            mVessel = v;

            mThrottlePID = new Legacy.PidController(10, 1e-5F, 1e-5F, 50, -1, 1);
            mWheelPID = new Legacy.PidController(10, 1e-5F, 1e-5F, 50, -1, 1);

        }

        private float RoverHDG {
            get {
                Vector3 dir = mVessel.srf_velocity.normalized;

                switch (mAxis) {
                    case ModuleWheel.AlignmentAxis.Forward:
                        dir = mVessel.ReferenceTransform.forward;
                        break;
                    case ModuleWheel.AlignmentAxis.Right:
                        dir = mVessel.ReferenceTransform.right;
                        break;
                    case ModuleWheel.AlignmentAxis.Up:
                        dir = mVessel.ReferenceTransform.up;
                        break;
                }

                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;
                return RTUtil.GetHDG(dir, up, north);
            }
        }

        private float TargetHDG {
            get {
                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;
                return RTUtil.GetHDG((TargetPos - mVessel.CoM).normalized, up, north);
            }
        }

        private Vector3 TargetPos {
            get {
                return mVessel.mainBody.GetWorldSurfacePosition(mTargetLat, mTargetLon, mVessel.altitude);
            }
        }

        private Vector3 RoverOrigPos {
            get {
                return mVessel.mainBody.GetWorldSurfacePosition(mRoverLat, mRoverLon, mRoverAlt);
            }
        }

        private float RoverSpeed {
            get {

                switch (mAxis) {
                    case ModuleWheel.AlignmentAxis.None:
                        return (float)mVessel.srf_velocity.magnitude;
                    case ModuleWheel.AlignmentAxis.Forward:
                        return Vector3.Dot(mVessel.srf_velocity, mVessel.ReferenceTransform.forward);
                    case ModuleWheel.AlignmentAxis.Right:
                        return Vector3.Dot(mVessel.srf_velocity, mVessel.ReferenceTransform.right);
                    case ModuleWheel.AlignmentAxis.Up:
                        return Vector3.Dot(mVessel.srf_velocity, mVessel.ReferenceTransform.up);
                }

                return (float)mVessel.srf_velocity.magnitude;
            }
        }

        private void UpdateAxis() {
            mAxis = ModuleWheel.AlignmentAxis.None;
            foreach (Part p in mVessel.parts) {
                if (p.Modules.Contains("ModuleWheel")) {
                    float a = 0, b = 0, c = 0;

                    ModuleWheel.ControlAxis controlAxis = (p.Modules["ModuleWheel"] as ModuleWheel).controlAxis;
                    switch (controlAxis) {
                        case ModuleWheel.ControlAxis.Forward:
                            a = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.forward, p.transform.forward));
                            b = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.right, p.transform.forward));
                            c = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.up, p.transform.forward));
                            break;
                        case ModuleWheel.ControlAxis.Right:
                            a = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.forward, p.transform.right));
                            b = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.right, p.transform.right));
                            c = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.up, p.transform.right));
                            break;
                        case ModuleWheel.ControlAxis.Up:
                            a = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.forward, p.transform.up));
                            b = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.right, p.transform.up));
                            c = Math.Abs(Vector3.Dot(mVessel.ReferenceTransform.up, p.transform.up));
                            break;
                    }

                    if (a > c) {
                        if (a > b)
                            mAxis = ModuleWheel.AlignmentAxis.Forward;
                        else
                            mAxis = ModuleWheel.AlignmentAxis.Right;
                    } else {
                        if (c > b)
                            mAxis = ModuleWheel.AlignmentAxis.Up;
                        else
                            mAxis = ModuleWheel.AlignmentAxis.Right;
                    }

                    if (mAxis != ModuleWheel.AlignmentAxis.None)
                        break;
                }
            }
        }

        public void InitMode(DriveCommand dc) {

            UpdateAxis();

            mRoverAlt = (float)mVessel.altitude;
            mRoverLat = (float)mVessel.latitude;
            mRoverLon = (float)mVessel.longitude;

            switch (dc.mode) {
                case DriveCommand.DriveMode.Turn:
                    mRoverRot = mVessel.ReferenceTransform.rotation;
                    break;
                case DriveCommand.DriveMode.Distance:
                    mWheelPID.setClamp(-1, 1);
                    mKeepHDG = RoverHDG;
                    break;
                case DriveCommand.DriveMode.DistanceHeading:
                    mWheelPID.setClamp(-dc.steering, dc.steering);
                    break;
                case DriveCommand.DriveMode.Coord:
                    mWheelPID.setClamp(-dc.steering, dc.steering);
                    mTargetLat = dc.target;
                    mTargetLon = dc.target2;
                    break;
            }
            mThrottlePID.Reset();
            mWheelPID.Reset();
            mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
        }

        public void Drive(DriveCommand dc, FlightCtrlState fs) {
            if (dc != null) {
                switch (dc.mode) {
                    case DriveCommand.DriveMode.Turn:
                        Turn(dc, fs);
                        break;
                    case DriveCommand.DriveMode.Distance:
                        Distance(dc, fs);
                        break;
                    case DriveCommand.DriveMode.DistanceHeading:
                        DistanceHeading(dc, fs);
                        break;
                    case DriveCommand.DriveMode.Coord:
                        Coord(dc, fs);
                        break;
                }
            }
        }

        private void Turn(DriveCommand dc, FlightCtrlState fs) {
            ModuleWheel m = new ModuleWheel();
            if (Math.Abs(Quaternion.Angle(mRoverRot, mVessel.ReferenceTransform.rotation)) < dc.target) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - RoverSpeed);
                fs.wheelSteer = dc.steering;
            } else {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                dc = null;
            }
        }

        private void Distance(DriveCommand dc, FlightCtrlState fs) {
            if (Vector3.Distance(RoverOrigPos, mVessel.CoM) < Math.Abs(dc.target)) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - RoverSpeed);
                fs.wheelSteer = (dc.speed < 0 ? -1 : 1) * mWheelPID.Control(RTUtil.ClampDegrees180(RoverHDG - mKeepHDG));
            } else {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                dc = null;
            }
        }

        private void DistanceHeading(DriveCommand dc, FlightCtrlState fs) {
            if (Vector3.Distance(RoverOrigPos, mVessel.CoM) < Math.Abs(dc.target)) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - RoverSpeed);
                fs.wheelSteer = mWheelPID.Control(RTUtil.ClampDegrees180(RoverHDG - dc.target2));
            } else {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                dc = null;
            }
        }

        private void Coord(DriveCommand dc, FlightCtrlState fs) {
            float
                dist = Vector3.Distance(mVessel.CoM, TargetPos),
                deg = RTUtil.ClampDegrees180(RoverHDG - TargetHDG);

            if (dist > Math.Abs(deg) / 36) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - RoverSpeed);
                fs.wheelSteer = mWheelPID.Control(deg);
            } else {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                dc = null;
            }
        }
    }
}
