using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// DANGER AHEAD, turn back now if you value your sanity!
// This code is kept alive in cruel and artificial manner.
// Bring an empty stomach.

namespace RemoteTech.Legacy {
    public class FlightComputer {
        public struct RoverState {
            public bool Steer,
                reverse;
            public double
                ActTime,
                longitude,
                latitude;
            public float
                Speed,
                Steering,
                Target;
            public Quaternion roverRotation;
        }

        double torqueRAvailable, torquePYAvailable, torqueThrustPYAvailable;

        private int[] rapidToggleX = { 0, 0, 0, 0 }; //Array size determines how many toggles required to trigger the axis rest, init array to 0s
        private int[] rapidToggleY = { 0, 0, 0, 0 }; //Negative values indicate the vector was at the minimum threshold
        private int[] rapidToggleZ = { 0, 0, 0, 0 };

        private int rapidRestX = 0; //How long the axis should rest for if rapid toggle is detected
        private int rapidRestY = 0;
        private int rapidRestZ = 0;
        private int rapidToggleRestFactor = 8; //Factor the axis is divided by if the rapid toggle resting is activated

        private Vector3d integral = Vector3d.zero;
        private Vector3d[] a_avg_act = new Vector3d[2];
        Vector3d prev_err;

        public float Kp = 120.0F;
        public Quaternion Target;
        bool attitideActive = true;
        double attitudeError;

        public Vessel Vessel { get; set; }

        public FlightComputer(Vessel v) {
            Vessel = v;
        }

        public bool roverActive = false;
        public double altitude;
        public RoverState roverState;
        RoverPidController throttlePID;

        public void setRover(RoverState StateIn) {
            throttlePID = new RoverPidController(10, 1e-5F, 1e-5F, 50, 1);
            this.roverState = StateIn;
            altitude = Vector3d.Distance(Vessel.mainBody.position, Vessel.transform.position);
            roverActive = true;
            Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
        }

        public void drive(FlightCtrlState s, Quaternion Target) {
            if (attitideActive) {

                updateAvailableTorque();

                attitudeError = Math.Abs(Vector3d.Angle(Target * Vector3d.forward, Vessel.ReferenceTransform.up));
                // Used in the drive_limit calculation
                double precision = Math.Max(0.5, Math.Min(10.0, (torquePYAvailable + torqueThrustPYAvailable * s.mainThrottle) * 20.0 / MoI.magnitude));

                // Direction we want to be facing
                Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(Vessel.ReferenceTransform.rotation) * Target);


                Vector3d deltaEuler = new Vector3d(
                                                      (delta.eulerAngles.x > 180) ? (delta.eulerAngles.x - 360.0F) : delta.eulerAngles.x,
                                                      -((delta.eulerAngles.y > 180) ? (delta.eulerAngles.y - 360.0F) : delta.eulerAngles.y),
                                                      (delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z
                                                  );

                Vector3d torque = new Vector3d(
                                                      torquePYAvailable + torqueThrustPYAvailable * s.mainThrottle,
                                                      torqueRAvailable,
                                                      torquePYAvailable + torqueThrustPYAvailable * s.mainThrottle
                                              );

                Vector3d inertia = Vector3d.Scale(
                                                      RTUtils.Sign(angularMomentum) * 1.8f,
                                                      Vector3d.Scale(
                                                          Vector3d.Scale(angularMomentum, angularMomentum),
                                                          RTUtils.Invert(Vector3d.Scale(torque, MoI))
                                                      )
                                                 );

                // Determine the current level of error, this is then used to determine what act to apply
                Vector3d err = deltaEuler * Math.PI / 180.0F;
                err += RTUtils.Reorder(inertia, 132);
                err.Scale(Vector3d.Scale(MoI, RTUtils.Invert(torque)));
                prev_err = err;

                // Make sure we are taking into account the correct timeframe we are working with
                integral += err * TimeWarp.fixedDeltaTime;

                // The inital value of act
                // Act is ultimately what determines what the pitch, yaw and roll will be set to
                // We make alterations to act between here and passing it into the pod controls
                Vector3d act = Mathf.Clamp((float)attitudeError, 1.0F, 3.0F) * Kp * err;
                //+ Ki * integral + Kd * deriv; //Ki and Kd are always 0 so they have been commented out

                // The maximum value the controls may be
                float drive_limit = Mathf.Clamp01((float)(err.magnitude * 450 / precision));

                // Reduce z by reduceZfactor, z seems to act far too high in relation to x and y
                act.z = act.z / 18.0F;

                // Reduce all 3 axis to a maximum of drive_limit
                act.x = Mathf.Clamp((float)act.x, drive_limit * -1, drive_limit);
                act.y = Mathf.Clamp((float)act.y, drive_limit * -1, drive_limit);
                act.z = Mathf.Clamp((float)act.z, drive_limit * -1, drive_limit);

                // Met is the time in seconds from take off
                double met = Planetarium.GetUniversalTime() - Vessel.launchTime;

                // Reduce effects of controls after launch and returns them gradually
                // This helps to reduce large wobbles experienced in the first few seconds
                if (met < 3.0F) {
                    act = act * ((met / 3.0F) * (met / 3.0F));
                }

                // Averages out the act with the last few results, determined by the size of the a_avg_act array
                act = RTUtils.averageVector3d(a_avg_act, act);

                // Looks for rapid toggling of each axis and if found, then rest that axis for a while
                // This helps prevents wobbles from getting larger
                rapidRestX = RTUtils.restForPeriod(rapidToggleX, act.x, rapidRestX);
                rapidRestY = RTUtils.restForPeriod(rapidToggleY, act.y, rapidRestY);
                rapidRestZ = RTUtils.restForPeriod(rapidToggleZ, act.z, rapidRestZ);

                // Reduce axis by rapidToggleRestFactor if rapid toggle rest has been triggered
                if (rapidRestX > 0)
                    act.x = act.x / rapidToggleRestFactor;
                if (rapidRestY > 0)
                    act.y = act.y / rapidToggleRestFactor;
                if (rapidRestZ > 0)
                    act.z = act.z / rapidToggleRestFactor;

                // Sets the SetFlightCtrlState for pitch, yaw and roll
                if (!double.IsNaN(act.z))
                    s.roll = Mathf.Clamp((float)(act.z), -1, 1);
                if (!double.IsNaN(act.x))
                    s.pitch = Mathf.Clamp((float)(act.x), -1, 1);
                if (!double.IsNaN(act.y))
                    s.yaw = Mathf.Clamp((float)(act.y), -1, 1);
            }
            if (roverActive) {
                if (roverState.Steer) {
                    if (Quaternion.Angle(roverState.roverRotation, Vessel.ReferenceTransform.rotation) < roverState.Target) {
                        s.wheelThrottle = roverState.reverse ? -throttlePID.Control(roverState.Speed - (float)Vessel.horizontalSrfSpeed) : throttlePID.Control(roverState.Speed - (float)Vessel.horizontalSrfSpeed);
                        s.wheelSteer = roverState.Steering;
                    } else {
                        s.wheelThrottle = 0;
                        s.wheelSteer = 0;
                        roverActive = false;
                        Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                    }
                } else {
                    if ((float)Vector3d.Distance(Vessel.mainBody.position + altitude * Vessel.mainBody.GetSurfaceNVector(roverState.latitude, roverState.longitude), Vessel.transform.position) < roverState.Target) {
                        s.wheelThrottle = roverState.reverse ? -throttlePID.Control(roverState.Speed - (float)Vessel.horizontalSrfSpeed) : throttlePID.Control(roverState.Speed - (float)Vessel.horizontalSrfSpeed);
                        s.wheelSteer = roverState.Steering;
                    } else {
                        s.wheelThrottle = 0;
                        s.wheelSteer = 0;
                        roverActive = false;
                        Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                    }
                }
            }
        }

        public Vector3d velocityVesselSurfaceUnit {
            get {
                return (Vessel.srf_velocity).normalized;
            }
        }

        public Vector3d velocityVesselOrbitNorm {
            get {
                return Vessel.orbit.GetVel().normalized;
            }
        }

        public Vector3d up {
            get {
                return (CoM - Vessel.mainBody.position).normalized;
            }
        }

        public Vector3d CoM {
            get {
                return Vessel.findWorldCenterOfMass();
            }
        }

        public Quaternion rotationSurface {
            get {
                return Quaternion.LookRotation(Vector3d.Exclude(up, (Vessel.mainBody.position + Vessel.mainBody.transform.up * (float)Vessel.mainBody.Radius) - CoM).normalized, up);
            }
        }

        public Vector3d MoI;

        public Vector3d angularMomentum {
            get {
                return new Vector3d((Quaternion.Inverse(Vessel.ReferenceTransform.rotation) * Vessel.rigidbody.angularVelocity).x * MoI.x, (Quaternion.Inverse(Vessel.ReferenceTransform.rotation) * Vessel.rigidbody.angularVelocity).y * MoI.y, (Quaternion.Inverse(Vessel.ReferenceTransform.rotation) * Vessel.rigidbody.angularVelocity).z * MoI.z);
            }
        }

        public void updateAvailableTorque() {
            MoI = Vessel.findLocalMOI(CoM);
            torqueRAvailable = torquePYAvailable = torqueThrustPYAvailable = 0;

            foreach (Part p in Vessel.parts) {
                MoI += p.Rigidbody.inertiaTensor;
                if (((p.State == PartStates.ACTIVE) || ((Staging.CurrentStage > Staging.lastStage) && (p.inverseStage == Staging.lastStage)))) {
                    if (p is LiquidEngine && p.RequestFuel(p, 0, Part.getFuelReqId()) && ((LiquidEngine)p).thrustVectoringCapable) {
                        torqueThrustPYAvailable += Math.Sin(Math.Abs(((LiquidEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    } else if (p is LiquidFuelEngine && p.RequestFuel(p, 0, Part.getFuelReqId()) && ((LiquidFuelEngine)p).thrustVectoringCapable) {
                        torqueThrustPYAvailable += Math.Sin(Math.Abs(((LiquidFuelEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidFuelEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    } else if (p is AtmosphericEngine && p.RequestFuel(p, 0, Part.getFuelReqId()) && ((AtmosphericEngine)p).thrustVectoringCapable) {
                        torqueThrustPYAvailable += Math.Sin(Math.Abs(((AtmosphericEngine)p).gimbalRange) * Math.PI / 180) * ((AtmosphericEngine)p).maximumEnginePower * ((AtmosphericEngine)p).totalEfficiency * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                    if (p.Modules.Contains("ModuleGimbal") && p.Modules.Contains("ModuleEngines")) {
                        torqueThrustPYAvailable += Math.Sin(Math.Abs(((ModuleGimbal)p.Modules["ModuleGimbal"]).gimbalRange) * Math.PI / 180) * ((ModuleEngines)p.Modules["ModuleEngines"]).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }

                if (Vessel.ActionGroups[KSPActionGroup.RCS] && (p is RCSModule || p.Modules.Contains("ModuleRCS"))) {
                    double maxT = 0;
                    for (int i = 0; i < 6; i++) {
                        if (p is RCSModule && ((RCSModule)p).thrustVectors[i] != Vector3.zero) {
                            maxT = Math.Max(maxT, ((RCSModule)p).thrusterPowers[i]);
                        }
                    }
                    torquePYAvailable += maxT * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;

                    foreach (PartModule m in p.Modules) {
                        if (m is ModuleRCS)
                            torquePYAvailable += ((ModuleRCS)m).thrusterPower * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }

                if (p is CommandPod) {
                    torqueRAvailable += Math.Abs(((CommandPod)p).rotPower);
                    torquePYAvailable += Math.Abs(((CommandPod)p).rotPower);
                }
            }
        }

        class RoverPidController {
            public /* private */ float mKp, mKd, mKi;
            private float mOldVal, mOldTime, mOldD;
            private float mClamp;
            private float[] mBuffer = null;
            private int mPtr;
            private float mSum;
            private float mValue;

            public RoverPidController(float Kp, float Ki, float Kd,
                                  int integrationBuffer, float clamp) {
                mKp = Kp;
                mKi = Ki;
                mKd = Kd;
                mClamp = clamp;
                if (integrationBuffer >= 1)
                    mBuffer = new float[integrationBuffer];
                Reset();
            }

            public void Reset() {
                mSum = 0;
                mOldTime = -1;
                mOldD = 0;
                if (mBuffer != null)
                    for (int i = 0; i < mBuffer.Length; i++)
                        mBuffer[i] = 0;
                mPtr = 0;
            }

            public float Control(float v) {
                if (TimeWarp.deltaTime > mOldTime) {
                    if (mOldTime >= 0) {
                        mOldD = (v - mOldVal) / (TimeWarp.deltaTime - mOldTime);

                        float i = v / (TimeWarp.deltaTime - mOldTime);
                        if (mBuffer != null) {
                            mSum -= mBuffer[mPtr];
                            mBuffer[mPtr] = i;
                            mPtr++;
                            if (mPtr >= mBuffer.Length)
                                mPtr = 0;
                        }
                        mSum += i;
                    }

                    mOldTime = TimeWarp.deltaTime;
                    mOldVal = value;
                }

                mValue = mKp * v + mKi * mSum + mKd * mOldD;

                if (mClamp > 0) {
                    if (mValue > mClamp)
                        mValue = mClamp;
                    if (mValue < -mClamp)
                        mValue = -mClamp;
                }

                return mValue;
            }

            public float value {
                get { return mValue; }
            }

            public static implicit operator float(RoverPidController v) {
                return v.mValue;
            }
        }
    }

    public static class RTUtils {
        public static Vector3d Sign(Vector3d vector) {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }

        public static Vector3d Invert(Vector3d vector) {
            return new Vector3d(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }

        public static Vector3d Reorder(Vector3d vector, int order) {
            switch (order) {
                case 123:
                    return new Vector3d(vector.x, vector.y, vector.z);
                case 132:
                    return new Vector3d(vector.x, vector.z, vector.y);
                case 213:
                    return new Vector3d(vector.y, vector.x, vector.z);
                case 231:
                    return new Vector3d(vector.y, vector.z, vector.x);
                case 312:
                    return new Vector3d(vector.z, vector.x, vector.y);
                case 321:
                    return new Vector3d(vector.z, vector.y, vector.x);
            }
            throw new ArgumentException("Invalid order", "order");
        }

        public static Vector3d averageVector3d(Vector3d[] vectorArray, Vector3d newVector) {
            double x = 0.0, y = 0.0, z = 0.0;
            int n = vectorArray.Length;
            int k = 0;

            // Loop through the array to determine average
            // Give more weight to newer items and less weight to older items
            for (int i = 0; i < n; i++) {
                k += i + 1;
                if (i < n - 1) { vectorArray[i] = vectorArray[i + 1]; } else { vectorArray[i] = newVector; }
                x += vectorArray[i].x * (i + 1);
                y += vectorArray[i].y * (i + 1);
                z += vectorArray[i].z * (i + 1);
            }
            return new Vector3d(x / k, y / k, z / k);
        }

        public static int restForPeriod(int[] restArray, double vector, int resting) {
            int n = restArray.Length - 1; //Last elemet in the array, useful in loops and inseting element to the end
            int insertPos = -1; //Position to insert a vector into
            int vectorSign = Mathf.Clamp(Mathf.RoundToInt((float)vector), -1, 1); //Is our vector a negative, 0 or positive
            float threshold = 0.95F; //Vector must be above this to class as hitting the upper limit
            bool aboveThreshold = Mathf.Abs((float)vector) > threshold; //Determines if the input vector is above the threshold

            // Decrease our resting count so we don't rest forever
            if (resting > 0)
                resting--;

            // Move all values in restArray towards 0 by 1, effectly taking 1 frame off the count
            // Negative values indicate the vector was at the minimum threshold
            for (int i = n; i >= 0; i--)
                restArray[i] = restArray[i] - Mathf.Clamp(restArray[i], -1, 1);

            // See if the oldest value has reached 0, if it has move all values 1 to the left
            if (restArray[0] == 0 && restArray[1] != 0) {
                for (int i = 0; i < n - 1; i++)
                    restArray[i] = restArray[i + 1]; //shuffle everything down to the left
                restArray[n] = 0; //item n has just been shifted to the left, so reset value to 0
            }

            // Find our position to insert the vector sign, insertPos will be -1 if no empty position is found
            for (int i = n; i >= 0; i--) {
                if (restArray[i] == 0)
                    insertPos = i;
            }

            // If we found a valid insert position, and the sign is different to the last sign, then insert it
            if (
                aboveThreshold && ( // First make sure we are above the threshold

                    // If in position 0, the sign is always different
                    (insertPos == 0) ||

                    // If in position 1 to n-1, then make sure the previous sign is different
                // We don't want to rest if the axis is simply at the max for a while such as it may be for hard turns
                    (insertPos > 0 && vectorSign != Mathf.Clamp(restArray[insertPos - 1], -1, 1))
                )
               ) // end if aboveThreshold
            {
                // Insert restFrameTolerance and the vectors sign
                restArray[insertPos] = 90 * vectorSign;
            }

            // Determine if the array is full, we are above the threshold, and the sign is different to the last
            if (aboveThreshold && insertPos == -1 && vectorSign != Mathf.Clamp(restArray[n], -1, 1)) {
                // Array is full, remove oldest value to make room for new value
                for (int i = 0; i < n - 1; i++) {
                    restArray[i] = restArray[i + 1];
                }
                // Insert new value
                restArray[n] = 90 * vectorSign;

                // Sets the axis to rest for the length of time 3/4 of the frame difference between the first item and last item
                resting = (int)Math.Ceiling(((Math.Abs(restArray[n]) - Math.Abs(restArray[0])) / 4.0) * 3.0);
            }

            // Returns number of frames to rest for, or 0 for no rest
            return resting;
        }
    }
}