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

        private ModuleWheel.AlignmentAxis axis;

        public RoverComputer(Vessel v) {
            mVessel = v;

            mThrottlePID = new Legacy.PidController(10, 1e-5F, 1e-5F, 50, -1, 1);
            mWheelPID = new Legacy.PidController(10, 1e-5F, 1e-5F, 50, -1, 1);
        }

        private float RoverHDG {
            get {
                Vector3 dir = mVessel.srf_velocity.normalized;

                switch (axis) {
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

                switch (axis) {
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

        public void InitMode(DriveCommand dc) {

            axis = ModuleWheel.AlignmentAxis.None;
            foreach (Part p in mVessel.parts) {
                if (p.Modules.Contains("ModuleWheel")) {
                    axis = (p.Modules["ModuleWheel"] as ModuleWheel).alignmentAxis;
                    if (axis != ModuleWheel.AlignmentAxis.None)
                        break;
                }
            }
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
