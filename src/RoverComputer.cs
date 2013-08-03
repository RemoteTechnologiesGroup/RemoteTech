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
            mRoverRot;

        private Quaternion RR;

        private float RoverRotation {
            get {
                return mVessel.GetRotationVesselSurface().eulerAngles.y;
            }
        }

        private float TargetDir {
            get {
                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;
                Quaternion rotationSurface = Quaternion.LookRotation(north, up);
                Quaternion rotationTarget = Quaternion.LookRotation((TargetPos - mVessel.CoM).normalized, up);

                return Quaternion.Inverse(Quaternion.Inverse(rotationTarget) * rotationSurface).eulerAngles.y;
            }
        }

        private Vector3 TargetPos {
            get {
                return mVessel.mainBody.GetWorldSurfacePosition(mTargetLat, mTargetLon, mVessel.altitude);
            }
        }

        private float ForwardSpeed {
            get {
                return Vector3.Dot(mVessel.srf_velocity, mVessel.ReferenceTransform.up);
            }
        }

        public RoverComputer(Vessel v) {
            mVessel = v;

            mThrottlePID = new Legacy.PidController(10, 1e-5F, 1e-5F, 50, -1, 1);
            mWheelPID = new Legacy.PidController(10, 1e-5F, 1e-5F, 50, -1, 1);
        }

        public void InitMode(DriveCommand dc) {
            mRoverAlt = Vector3.Distance(mVessel.mainBody.position, mVessel.transform.position);
            mRoverLat = (float)mVessel.latitude;
            mRoverLon = (float)mVessel.longitude;

            switch (dc.mode) {
                case DriveCommand.DriveMode.Turn:
                    RR = mVessel.ReferenceTransform.rotation;
                    break;
                case DriveCommand.DriveMode.Distance:
                    mWheelPID.setClamp(-1, 1);
                    mRoverRot = RoverRotation;
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

        public void Turn(DriveCommand dc, FlightCtrlState fs) {

            if (Math.Abs(Quaternion.Angle(RR, mVessel.ReferenceTransform.rotation)) < dc.target) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - ForwardSpeed);
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
            if (Vector3.Distance(mVessel.mainBody.position + mRoverAlt * mVessel.mainBody.GetSurfaceNVector(mRoverLat, mRoverLon), mVessel.transform.position) < Math.Abs(dc.target)) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - ForwardSpeed);
                fs.wheelSteer = (dc.speed < 0 ? -1 : 1) * mWheelPID.Control(RTUtil.ClampDegrees180(RoverRotation - mRoverRot));
            } else {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                dc = null;
            }
        }

        private void DistanceHeading(DriveCommand dc, FlightCtrlState fs) {
            if (Vector3.Distance(mVessel.mainBody.position + mRoverAlt * mVessel.mainBody.GetSurfaceNVector(mRoverLat, mRoverLon), mVessel.transform.position) < Math.Abs(dc.target)) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - ForwardSpeed);
                fs.wheelSteer = mWheelPID.Control(RTUtil.ClampDegrees180(RoverRotation - dc.target2));
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
                deg = RTUtil.ClampDegrees180(RoverRotation - TargetDir);

            if (dist > Math.Abs(deg) / 5) {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - ForwardSpeed);
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
