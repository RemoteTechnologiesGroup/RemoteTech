using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    class PidController
    {
        private float
            mKp, mKd, mKi,
            mOldVal,
            mOldD,
            McMin, McMax,
            mSum,
            mValue,
            mErrScalar;

        public PidController(float p, float i, float d, float clampMin, float clampMax)
        {
            mKp = p;
            mKi = i;
            mKd = d;
            McMin = clampMin;
            McMax = clampMax;
            mErrScalar = 0f;
            Reset();
        }

        public PidController(float p, float i, float d, float clampMin, float clampMax, float errorScale)
        {
            mKp = p;
            mKi = i;
            mKd = d;
            McMin = clampMin;
            McMax = clampMax;
            mErrScalar = errorScale;
            Reset();
        }

        public void Reset()
        {
            mSum = mOldVal = mOldD = mValue = 0;
        }

        public void setClamp(float clampMin, float clampMax)
        {
            McMin = clampMin;
            McMax = clampMax;
        }

        public float Control(float v)
        {
            mOldD = (v - mOldVal) / TimeWarp.deltaTime;

            mSum += v / TimeWarp.deltaTime;

            mOldVal = value;

            mValue = mKp * v + mKi * mSum + mKd * mOldD;

            mValue = Mathf.Clamp(mValue, McMin, McMax);

            if (mErrScalar > 0)
            {
                float ErrorScale = Math.Abs(v) / mErrScalar;
                if (ErrorScale > 1)
                    ErrorScale = 1;
                mValue *= ErrorScale;
            }

            return mValue;
        }

        public float value
        {
            get { return mValue; }
        }

        public static implicit operator float(PidController v)
        {
            return v.mValue;
        }
    }

    public class RoverComputer
    {
        private Vessel mVessel;
        private PidController mThrottlePID, mWheelPID;

        private float
            mRoverAlt,
            mRoverLat,
            mRoverLon,
            mTargetLat,
            mTargetLon,
            brakeDist;

        private Quaternion mRoverRot;

        private Vector3 ForwardAxis;

        public float Delta { get; private set; }
        public float DeltaT { get; private set; }

        public RoverComputer(Vessel v)
        {
            mVessel = v;
            mThrottlePID = new PidController(10f, 1e-5F, 1e-5F, -1f, 1f);
            mWheelPID = new PidController(10f, 1e-5F, 1e-5F, -1f, 1f, 5f);
        }

        private float RoverHDG
        {
            get
            {
                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;

                if (ForwardAxis == Vector3.zero)
                    return RTUtil.GetHDG(mVessel.srf_velocity.normalized, up, north);
                else
                    return RTUtil.GetHDG(mVessel.ReferenceTransform.TransformDirection(ForwardAxis), up, north);
            }
        }

        private float TargetHDG
        {
            get
            {
                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;
                return RTUtil.GetHDG((TargetPos - mVessel.CoM).normalized, up, north);
            }
        }

        private Vector3 TargetPos
        {
            get
            {
                return mVessel.mainBody.GetWorldSurfacePosition(mTargetLat, mTargetLon, mVessel.altitude);
            }
        }

        private Vector3 RoverOrigPos
        {
            get
            {
                return mVessel.mainBody.GetWorldSurfacePosition(mRoverLat, mRoverLon, mRoverAlt);
            }
        }

        private float RoverSpeed
        {
            get
            {
                if (ForwardAxis == Vector3.zero)
                    return (float)mVessel.srf_velocity.magnitude;
                else
                    return Vector3.Dot(mVessel.srf_velocity, mVessel.ReferenceTransform.TransformDirection(ForwardAxis));
            }
        }

        public void InitMode(DriveCommand dc)
        {
            ForwardAxis = Vector3.zero;
            mRoverAlt = (float)mVessel.altitude;
            mRoverLat = (float)mVessel.latitude;
            mRoverLon = (float)mVessel.longitude;
            Delta = 0;
            DeltaT = 0;
            brakeDist = 0;

            switch (dc.mode)
            {
                case DriveCommand.DriveMode.Turn:
                    mRoverRot = mVessel.ReferenceTransform.rotation;
                    break;
                case DriveCommand.DriveMode.Distance:
                    mWheelPID.setClamp(-1, 1);
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

        public bool Drive(DriveCommand dc, FlightCtrlState fs)
        {
            if (dc != null)
            {
                if (mVessel.srf_velocity.magnitude > 0.5)
                {
                    float degForward = Mathf.Abs(RTUtil.ClampDegrees90(Vector3.Angle(mVessel.srf_velocity, mVessel.ReferenceTransform.forward)));
                    float degUp = Mathf.Abs(RTUtil.ClampDegrees90(Vector3.Angle(mVessel.srf_velocity, mVessel.ReferenceTransform.up)));
                    float degRight = Mathf.Abs(RTUtil.ClampDegrees90(Vector3.Angle(mVessel.srf_velocity, mVessel.ReferenceTransform.right)));

                    if (degForward < degUp && degForward < degRight)
                        ForwardAxis = Vector3.forward;
                    else if (degRight < degUp && degRight < degForward)
                        ForwardAxis = Vector3.right;
                    else
                        ForwardAxis = Vector3.up;
                }

                switch (dc.mode)
                {
                    case DriveCommand.DriveMode.Turn:
                        return Turn(dc, fs);
                    case DriveCommand.DriveMode.Distance:
                        return Distance(dc, fs);
                    case DriveCommand.DriveMode.DistanceHeading:
                        return DistanceHeading(dc, fs);
                    case DriveCommand.DriveMode.Coord:
                        return Coord(dc, fs);
                }
                return true;
            }
            return true;
        }

        private bool Turn(DriveCommand dc, FlightCtrlState fs)
        {
            Delta = Math.Abs(Quaternion.Angle(mRoverRot, mVessel.ReferenceTransform.rotation));
            DeltaT = Delta / mVessel.angularVelocity.magnitude;
            if (Delta < dc.target)
            {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - RoverSpeed);
                fs.wheelSteer = dc.steering;
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                return true;
            }
        }

        private bool Distance(DriveCommand dc, FlightCtrlState fs)
        {
            float speed = RoverSpeed;
            Delta = Math.Abs(dc.target) - Vector3.Distance(RoverOrigPos, mVessel.CoM);
            DeltaT = Delta / Math.Abs(speed);
            if (Delta > 0)
            {
                fs.wheelThrottle = mThrottlePID.Control(BrakeSpeed(dc.speed, speed) - speed);
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                return true;
            }
        }

        private bool DistanceHeading(DriveCommand dc, FlightCtrlState fs)
        {
            float speed = RoverSpeed;
            Delta = Math.Abs(dc.target) - Vector3.Distance(RoverOrigPos, mVessel.CoM);
            DeltaT = Delta / speed;
            if (Delta > 0)
            {
                fs.wheelThrottle = mThrottlePID.Control(BrakeSpeed(dc.speed, speed) - speed);
                if (ForwardAxis != Vector3.zero)
                    fs.wheelSteer = mWheelPID.Control(RTUtil.AngleBetween(RoverHDG, dc.target2));
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                return true;
            }
        }

        private float BrakeSpeed(float speed, float actualSpeed)
        {
            if (brakeDist == 0)
            {
                if (DeltaT > actualSpeed / 2)
                {
                    return speed;
                }
                else
                    brakeDist = Delta;
            }
            return Math.Max(Math.Min(speed, (float)Math.Sqrt(Delta / brakeDist) * speed), speed / 10);
        }

        private bool Coord(DriveCommand dc, FlightCtrlState fs)
        {
            float
                deg = RTUtil.AngleBetween(RoverHDG, TargetHDG),
                speed = RoverSpeed;

            Delta = Vector3.Distance(mVessel.CoM, TargetPos);
            DeltaT = Delta / speed;

            if (Delta > Math.Abs(deg) / 36)
            {
                fs.wheelThrottle = mThrottlePID.Control(BrakeSpeed(dc.speed, speed) - speed);
                if (ForwardAxis != Vector3.zero)
                    fs.wheelSteer = mWheelPID.Control(deg);
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = DriveCommand.DriveMode.Off;
                return true;
            }
        }
    }
}
