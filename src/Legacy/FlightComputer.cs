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
        private Legacy.VesselState mVesselState;
        private Legacy.PIDControllerV mPid;
        private double lastResetRoll = 0;
        private double drive_factor = 100000;
        private double Tf = 0.1;
        private Vector3d lastAct = Vector3d.zero;
        public double stress;

        public double Kp = 10000;
        public double Ki = 0;
        public double Kd = 800;
        public double Ki_limit = 1000000;

        public FlightComputer() {
            mVesselState = new VesselState();
            mPid = new PIDControllerV(Kp, Ki, Kd, Ki_limit, -Ki_limit);
        }

        public void HoldOrientation(FlightCtrlState s, Vessel v, Quaternion orientation, bool roll) {
            mVesselState.Update(v);
            // Used in the killRot activation calculation and drive_limit calculation
            double precision = Math.Max(0.5, Math.Min(10.0, (Math.Min(mVesselState.torqueAvailable.x, mVesselState.torqueAvailable.z) + mVesselState.torqueThrustPYAvailable * s.mainThrottle) * 20.0 / mVesselState.MoI.magnitude));

            // Reset the PID controller during roll to keep pitch and yaw errors
            // from accumulating on the wrong axis.
            double rollDelta = Mathf.Abs((float)(mVesselState.vesselRoll - lastResetRoll));
            if (rollDelta > 180)
                rollDelta = 360 - rollDelta;
            if (rollDelta > 5) {
                mPid.Reset();
                lastResetRoll = mVesselState.vesselRoll;
            }

            // Direction we want to be facing
            Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(v.ReferenceTransform.rotation) * orientation);

            Vector3d deltaEuler = new Vector3d(
                                                    (delta.eulerAngles.x > 180) ? (delta.eulerAngles.x - 360.0F) : delta.eulerAngles.x,
                                                    -((delta.eulerAngles.y > 180) ? (delta.eulerAngles.y - 360.0F) : delta.eulerAngles.y),
                                                    (delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z
                                                );

            Vector3d torque = new Vector3d(
                                                    mVesselState.torqueAvailable.x + mVesselState.torqueThrustPYAvailable * s.mainThrottle,
                                                    mVesselState.torqueAvailable.y,
                                                    mVesselState.torqueAvailable.z + mVesselState.torqueThrustPYAvailable * s.mainThrottle
                                            );

            Vector3d inertia = Vector3d.Scale(
                                                    mVesselState.angularMomentum.Sign(),
                                                    Vector3d.Scale(
                                                        Vector3d.Scale(mVesselState.angularMomentum, mVesselState.angularMomentum),
                                                        Vector3d.Scale(torque, mVesselState.MoI).Invert()
                                                    )
                                                );
            Vector3d err = deltaEuler * Math.PI / 180.0F;
            err += inertia.Reorder(132);
            err.Scale(Vector3d.Scale(mVesselState.MoI, torque.Invert()).Reorder(132));

            Vector3d act = mPid.Compute(err);

            float drive_limit = Mathf.Clamp01((float)(err.magnitude * drive_factor / precision));

            act.x = Mathf.Clamp((float)act.x, drive_limit * -1, drive_limit);
            act.y = Mathf.Clamp((float)act.y, drive_limit * -1, drive_limit);
            act.z = Mathf.Clamp((float)act.z, drive_limit * -1, drive_limit);

            act = lastAct + (act - lastAct) * (TimeWarp.fixedDeltaTime / Tf);

            SetFlightCtrlState(act, deltaEuler, s, v, precision, drive_limit);

            act = new Vector3d(s.pitch, s.yaw, s.roll);
            lastAct = act;

            stress = Math.Abs(act.x) + Math.Abs(act.y) + Math.Abs(act.z);
        }

        public void HoldAltitude(FlightCtrlState fs, Vessel v, float altitude) {
            
        }

        private void SetFlightCtrlState(Vector3d act, Vector3d deltaEuler, FlightCtrlState s, Vessel vessel, double precision, float drive_limit) {
            bool userCommandingPitchYaw = false;
            bool userCommandingRoll = false;

            if (!userCommandingRoll) {
                if (!double.IsNaN(act.z))
                    s.roll = Mathf.Clamp((float)(act.z), -drive_limit, drive_limit);
            }

            if (!userCommandingPitchYaw) {
                if (!double.IsNaN(act.x))
                    s.pitch = Mathf.Clamp((float)(act.x), -drive_limit, drive_limit);
                if (!double.IsNaN(act.y))
                    s.yaw = Mathf.Clamp((float)(act.y), -drive_limit, drive_limit);
            }
        }
    }    

    public class PIDControllerV : IConfigNode {
        public Vector3d prevError, intAccum;
        public double Kp, Ki, Kd, max, min;

        public PIDControllerV(double Kp = 0, double Ki = 0, double Kd = 0, double max = double.MaxValue, double min = double.MinValue) {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error) {
            intAccum += error * TimeWarp.fixedDeltaTime;
            Vector3d action = (Kp * error) + (Ki * intAccum) + (Kd * (error - prevError) / TimeWarp.fixedDeltaTime);
            Vector3d clamped = new Vector3d(Math.Max(min, Math.Min(max, action.x)), Math.Max(min, Math.Min(max, action.y)), Math.Max(min, Math.Min(max, action.z)));
            if (Math.Abs((clamped - action).magnitude) > 0.01) {
                intAccum -= error * TimeWarp.fixedDeltaTime;
            }
            prevError = error;

            return action;
        }

        public void Reset() {
            prevError = intAccum = Vector3d.zero;
        }

        public void Load(ConfigNode node) {
            if (node.HasValue("Kp")) {
                Kp = Convert.ToDouble(node.GetValue("Kp"));
            }
            if (node.HasValue("Ki")) {
                Ki = Convert.ToDouble(node.GetValue("Ki"));
            }
            if (node.HasValue("Kd")) {
                Kd = Convert.ToDouble(node.GetValue("Kd"));
            }
        }

        public void Save(ConfigNode node) {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
    }
}