using System;
using UnityEngine;

namespace RemoteTech.FlightComputer
{
    public class RoverComputer
    {
        private const float driveLimit = 1.0f;
        private const double minRadarAlt = 2.0; // in meters
        private double Kp, Ki, Kd;

        private Vessel mVessel;
        private PIDLoop throttlePID;
        private PIDLoop steerPID;
        private PIDController pidController;

        private float
            mRoverAlt,
            mRoverLat,
            mRoverLon,
            mTargetLat,
            mTargetLon;

        private Quaternion mRoverRot;
        private Vector3 ForwardAxis;
        public Quaternion targetRotation;

        public float Delta { get; private set; }
        public float DeltaT { get; private set; }

        private float RoverHDG
        {
            get
            {
                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;

                if (ForwardAxis == Vector3.zero)
                    return RTUtil.GetHeading(mVessel.srf_velocity.normalized, up, north);
                else
                    return RTUtil.GetHeading(mVessel.ReferenceTransform.TransformDirection(ForwardAxis), up, north);
            }
        }

        private float TargetHDG
        {
            get
            {
                Vector3d up = (mVessel.CoM - mVessel.mainBody.position).normalized;
                Vector3d north = Vector3d.Exclude(up, (mVessel.mainBody.position + mVessel.mainBody.transform.up * (float)mVessel.mainBody.Radius) - mVessel.CoM).normalized;
                return RTUtil.GetHeading((TargetPos - mVessel.CoM).normalized, up, north);
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

        public RoverComputer(FlightComputer fc, double kp, double ki, double kd)
        {
            throttlePID = new PIDLoop(1, 0, 0);
            steerPID = new PIDLoop(1, 0, 0);

            pidController = fc.PIDController; //don't think of putting second copy of PID here
            this.Kp = kp;
            this.Ki = ki;
            this.Kd = kd;
        }

        public void SetVessel(Vessel v)
        {
            mVessel = v;
        }

        public void InitMode(RemoteTech.FlightComputer.Commands.DriveCommand dc)
        {
            if (mVessel == null)
            {
                RTLog.Verbose("Vessel is null!");
                return;
            }

            ForwardAxis = Vector3.zero;
            mRoverAlt = (float) mVessel.altitude;
            mRoverLat = (float) mVessel.latitude;
            mRoverLon = (float) mVessel.longitude;
            Delta = 0;
            DeltaT = 0;

            /* Explanation on targetRotation
             * Quaternion.Euler(x,y,z) - Returns a rotation that rotates z degrees around the z axis, 
             *                           x degrees around the x axis, and y degrees around the y axis
             *                           in that order.
             * 
             * Unity Q.Euler(0,0,0) isn't matched to KSP's rotation "(0,0,0)" (-90, varying UP-axis, 90) so need to 
             * match the target rotation to the KSP's rotation.
             * 
             * Rover-specific rotation is Forward (y) to HDG 0, Up (x) to North and
             * Right (z) to East.
             */
            const float KSPRotXAxis = -90f, KSPRotZAxis = 90f;
            double AngleFromUpAxis = Mathf.Rad2Deg * Math.Atan(-mVessel.upAxis.z / -mVessel.upAxis.x);
            float KSPRotYAxis = (float) -AngleFromUpAxis;

            switch (dc.mode)
            {
                case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Turn:
                    mRoverRot = mVessel.ReferenceTransform.rotation;
                    break;
                case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Distance:
                    targetRotation = Quaternion.Euler(KSPRotXAxis, KSPRotYAxis, KSPRotZAxis);
                    break;
                case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.DistanceHeading:
                    targetRotation = Quaternion.Euler(KSPRotXAxis - dc.target2, KSPRotYAxis, KSPRotZAxis);
                    break;
                case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Coord:
                    mTargetLat = dc.target;
                    mTargetLon = dc.target2;
                    targetRotation = Quaternion.Euler(KSPRotXAxis - TargetHDG, KSPRotYAxis, KSPRotZAxis);
                    break;
            }

            pidController.setPIDParameters(Kp, Ki, Kd);
            throttlePID.ResetI();
            steerPID.ResetI();

            mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
        }

        public bool Drive(RemoteTech.FlightComputer.Commands.DriveCommand dc, FlightCtrlState fs)
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
                    case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Turn:
                        return Turn(dc, fs);
                    case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Distance:
                        return Distance(dc, fs);
                    case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.DistanceHeading:
                        return DistanceHeading(dc, fs);
                    case RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Coord:
                        return Coord(dc, fs);
                }
                return true;
            }
            return true;
        }

        private bool Turn(RemoteTech.FlightComputer.Commands.DriveCommand dc, FlightCtrlState fs)
        {
            Delta = Math.Abs(Quaternion.Angle(mRoverRot, mVessel.ReferenceTransform.rotation));
            DeltaT = Delta / mVessel.GetComponent<Rigidbody>().angularVelocity.magnitude;

            if (Delta < dc.target)
            {
                fs.wheelThrottle = (float)throttlePID.Update(RoverSpeed, dc.speed, -1.0, 1.0);
                fs.wheelSteer = dc.steering;
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Off;
                return true;
            }
        }

        private bool Distance(RemoteTech.FlightComputer.Commands.DriveCommand dc, FlightCtrlState fs)
        {
            Delta = Math.Abs(dc.target) - Vector3.Distance(RoverOrigPos, mVessel.CoM);
            DeltaT = Delta / Math.Abs(RoverSpeed);

            if (Delta > 0)
            {
                fs.wheelThrottle = (float)throttlePID.Update(RoverSpeed, dc.speed, -1.0, 1.0);

                if (mVessel.radarAltitude > minRadarAlt)
                {
                    Vector3d actuation = pidController.GetActuation(targetRotation);
                    fs.pitch = Mathf.Clamp((float)actuation.x, -driveLimit, driveLimit);
                    fs.roll = Mathf.Clamp((float)actuation.y, -driveLimit, driveLimit);
                }
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Off;
                return true;
            }
        }

        private bool DistanceHeading(RemoteTech.FlightComputer.Commands.DriveCommand dc, FlightCtrlState fs)
        {
            Delta = Math.Abs(dc.target) - Vector3.Distance(RoverOrigPos, mVessel.CoM);
            DeltaT = Delta / RoverSpeed;

            if (Delta > 0)
            {
                fs.wheelThrottle = (float)throttlePID.Update(RoverSpeed, dc.speed, -1.0, 1.0);
                if (ForwardAxis != Vector3.zero)
                {
                    Vector3d actuation = pidController.GetActuation(targetRotation);
                    float steeringOutput = (float)-steerPID.Update(RTUtil.AngleBetween(RoverHDG, dc.target2), 0, -1.0, 1.0);
                    
                    if (mVessel.radarAltitude > minRadarAlt)
                    {
                        fs.pitch = Mathf.Clamp((float)actuation.x, -driveLimit, driveLimit);
                        fs.roll = Mathf.Clamp((float)actuation.y, -driveLimit, driveLimit);
                    }
                    fs.yaw = Mathf.Clamp((float)actuation.z, -driveLimit, driveLimit); //keep if u want jet car
                    fs.wheelSteer = SmoothenWheelSteering(RTUtil.AngleBetween(RoverHDG, dc.target2), steeringOutput, -dc.steering, dc.steering);
                }
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Off;
                return true;
            }
        }

        private bool Coord(RemoteTech.FlightComputer.Commands.DriveCommand dc, FlightCtrlState fs)
        {
            Delta = Vector3.Distance(mVessel.CoM, TargetPos);
            DeltaT = Delta / RoverSpeed;

            if (!TargetOvershoot()) // keep driving until we are starting to pass the target.
            {
                fs.wheelThrottle = (float)throttlePID.Update(RoverSpeed, dc.speed, -1.0, 1.0);
                if (ForwardAxis != Vector3.zero)
                {
                    Vector3d actuation = pidController.GetActuation(targetRotation);
                    float steeringOutput = (float) -steerPID.Update(RTUtil.AngleBetween(RoverHDG, TargetHDG), -1.0, 1.0);

                    if (mVessel.radarAltitude > minRadarAlt)
                    {
                        fs.pitch = Mathf.Clamp((float)actuation.x, -driveLimit, driveLimit);
                        fs.roll = Mathf.Clamp((float)actuation.y, -driveLimit, driveLimit);
                    }
                    fs.yaw = Mathf.Clamp((float)actuation.z, -driveLimit, driveLimit); //keep if u want jet car
                    fs.wheelSteer = SmoothenWheelSteering(RTUtil.AngleBetween(RoverHDG, TargetHDG), steeringOutput, -dc.steering, dc.steering);
                }
                return false;
            }
            else
            {
                fs.wheelThrottle = 0;
                fs.wheelSteer = 0;
                mVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                dc.mode = RemoteTech.FlightComputer.Commands.DriveCommand.DriveMode.Off;
                return true;
            }
        }

        private bool TargetOvershoot()
        {
            if (Delta > 1) // if we are more than one meter away from the target, do not check for overshoot.
                return false;
            else
                return RTUtil.AngleBetween(RoverHDG, TargetHDG) < 90; // if we are less than 1 meter from the target and the error angle between target and rover HDG is > 90, we can assume that we are overshooting the target and should stop.
        }

        private float SmoothenWheelSteering(float angleBetweenHDGs, float outputSteer, float maxLeftSteerLimit, float maxRightSteerLimit)
        {
            angleBetweenHDGs = Math.Abs(angleBetweenHDGs);

            if (angleBetweenHDGs <= 1)
            {
                outputSteer = Mathf.Clamp(outputSteer, -0.005f, 0.005f);
            }
            else if (angleBetweenHDGs <= 3)
            {
                outputSteer = Mathf.Clamp(outputSteer, -0.01f, 0.01f);
            }
            else if (angleBetweenHDGs <= 10)
            {
                outputSteer = Mathf.Clamp(outputSteer, -0.05f, 0.05f);
            }
            else if (angleBetweenHDGs <= 20)
            {
                outputSteer = Mathf.Clamp(outputSteer, -0.1f, 0.1f);
            }
            else if (angleBetweenHDGs <= 30)
            {
                outputSteer = Mathf.Clamp(outputSteer, -0.4f, 0.4f);
            }
            else
            {
                outputSteer = Mathf.Clamp(outputSteer, maxLeftSteerLimit, maxRightSteerLimit);
            }

            return outputSteer;
        }
    }
}
