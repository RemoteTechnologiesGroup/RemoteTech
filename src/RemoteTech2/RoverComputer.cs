using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{

    class PidController
    {
        public /* private */ float mKp, mKd, mKi;
        private float mOldVal, mOldTime, mOldD;
        private float McMin, McMax;
        private float[] mBuffer = null;
        private int mPtr;
        private float mSum;
        private float mValue;

        public PidController(float Kp, float Ki, float Kd,
                              int integrationBuffer, float clampMin, float clampMax)
        {
            mKp = Kp;
            mKi = Ki;
            mKd = Kd;
            McMin = clampMin;
            McMax = clampMax;
            if (integrationBuffer >= 1)
                mBuffer = new float[integrationBuffer];
            Reset();
        }

        public void Reset()
        {
            mSum = 0;
            mOldTime = -1;
            mOldD = 0;
            if (mBuffer != null)
                for (int i = 0; i < mBuffer.Length; i++)
                    mBuffer[i] = 0;
            mPtr = 0;
        }

        public void setClamp(float clampMin, float clampMax)
        {
            McMin = clampMin;
            McMax = clampMax;
        }

        public float Control(float v)
        {
            if (Time.fixedTime > mOldTime)
            {
                if (mOldTime >= 0)
                {
                    mOldD = (v - mOldVal) / (Time.fixedTime - mOldTime);

                    float i = v / (Time.fixedTime - mOldTime);
                    if (mBuffer != null)
                    {
                        mSum -= mBuffer[mPtr];
                        mBuffer[mPtr] = i;
                        mPtr++;
                        if (mPtr >= mBuffer.Length)
                            mPtr = 0;
                    }
                    mSum += i;
                }

                mOldTime = Time.fixedTime;
                mOldVal = value;
            }

            mValue = mKp * v + mKi * mSum + mKd * mOldD;


            if (mValue > McMax)
                mValue = McMax;
            if (mValue < McMin)
                mValue = McMin;


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
            mKeepHDG;

        private Quaternion mRoverRot;

        private Vector3 ForwardAxis;

        public RoverComputer(Vessel v)
        {
            mVessel = v;
            mThrottlePID = new PidController(10f, 1e-5F, 1e-5F, 50, -1f, 1f);
            mWheelPID = new PidController(10f, 1e-5F, 1e-5F, 50, -1f, 1f);
        }

        private float RoverHDG
        {
            get
            {
                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;
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
                return Vector3.Dot(mVessel.srf_velocity, mVessel.ReferenceTransform.TransformDirection(ForwardAxis));
            }
        }

        public void InitMode(DriveCommand dc)
        {

            ForwardAxis = Vector3.zero;
            mRoverAlt = (float)mVessel.altitude;
            mRoverLat = (float)mVessel.latitude;
            mRoverLon = (float)mVessel.longitude;

            switch (dc.mode)
            {
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

        public bool Drive(DriveCommand dc, FlightCtrlState fs)
        {
            if (dc != null)
            {
                if (ForwardAxis == Vector3.zero && mVessel.srf_velocity.magnitude > 0.1)
                {
                    float degForward = Vector3.Angle(mVessel.srf_velocity.normalized, mVessel.ReferenceTransform.forward);
                    float degUp = Vector3.Angle(mVessel.srf_velocity.normalized, mVessel.ReferenceTransform.up);
                    float degRight = Vector3.Angle(mVessel.srf_velocity.normalized, mVessel.ReferenceTransform.right);

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
            if (Math.Abs(Quaternion.Angle(mRoverRot, mVessel.ReferenceTransform.rotation)) < dc.target)
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
            if (Vector3.Distance(RoverOrigPos, mVessel.CoM) < Math.Abs(dc.target))
            {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - RoverSpeed);
                fs.wheelSteer = (dc.speed < 0 ? -1 : 1) * mWheelPID.Control(RTUtil.ClampDegrees180(RoverHDG - mKeepHDG));
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
            if (Vector3.Distance(RoverOrigPos, mVessel.CoM) < Math.Abs(dc.target))
            {
                fs.wheelThrottle = mThrottlePID.Control(dc.speed - RoverSpeed);
                if (ForwardAxis != Vector3.zero)
                    fs.wheelSteer = mWheelPID.Control(RTUtil.ClampDegrees180(RoverHDG - dc.target2));
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

        private float BrakeSpeed(float speed, float actualSpeed, float distance)
        {
            float time = distance / speed;
            if (time > 5)
                return speed;

            return Math.Max(speed * time / 5, speed / 10);
        }

        private bool Coord(DriveCommand dc, FlightCtrlState fs)
        {
            float
                dist = Vector3.Distance(mVessel.CoM, TargetPos),
                deg = RTUtil.ClampDegrees180(RoverHDG - TargetHDG),
                speed = RoverSpeed;

            if (dist > Math.Abs(deg) / 36)
            {
                fs.wheelThrottle = mThrottlePID.Control(BrakeSpeed(dc.speed, speed, dist) - speed);
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
