using System;
using System.Linq;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.FlightComputer
{
    public static class FlightCore
    {
        public static void HoldAttitude(FlightCtrlState fs, FlightComputer f, ReferenceFrame frame, FlightAttitude attitude, Quaternion extra)
        {
            var v = f.Vessel;
            var forward = Vector3.zero;
            var up = Vector3.zero;
            bool ignoreRoll = false;

            switch (frame)
            {
                case ReferenceFrame.Orbit:
                    ignoreRoll = true;
                    forward = v.GetObtVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;

                case ReferenceFrame.Surface:
                    ignoreRoll = true;
                    forward = v.GetSrfVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;

                case ReferenceFrame.North:
                    up = (v.mainBody.position - v.CoM);
                    forward = Vector3.ProjectOnPlane(v.mainBody.position + v.mainBody.transform.up * (float)v.mainBody.Radius - v.CoM, up);
                    break;

                case ReferenceFrame.Maneuver:
                    ignoreRoll = true;
                    if (f.Vessel.patchedConicSolver.maneuverNodes.Count != 0)
                    {
                        forward = f.Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(v.orbit);
                        up = (v.mainBody.position - v.CoM);
                    }
                    else
                    {
                        forward = v.GetObtVelocity();
                        up = (v.mainBody.position - v.CoM);
                    }
                    break;

                case ReferenceFrame.TargetVelocity:
                    // f.DelayedTarget may be any ITargetable, including a planet
                    // Velocity matching only makes sense for vessels and part modules
                    // Can test for Vessel but not PartModule, so instead test that it's not the third case (CelestialBody)
                    if (f.DelayedTarget != null && !(f.DelayedTarget is CelestialBody))
                    {
                        forward = v.GetObtVelocity() - f.DelayedTarget.GetObtVelocity();
                        up = (v.mainBody.position - v.CoM);
                    }
                    else
                    {
                        up = (v.mainBody.position - v.CoM);
                        forward = v.GetObtVelocity();
                    }
                    break;

                case ReferenceFrame.TargetParallel:
                    if (f.DelayedTarget != null && !(f.DelayedTarget is CelestialBody))
                    {
                        forward = f.DelayedTarget.GetTransform().position - v.CoM;
                        up = (v.mainBody.position - v.CoM);
                    }
                    else
                    {
                        up = (v.mainBody.position - v.CoM);
                        forward = v.GetObtVelocity();
                    }
                    break;
            }
            Vector3.OrthoNormalize(ref forward, ref up);
            Quaternion rotationReference = Quaternion.LookRotation(forward, up);
            
            switch (attitude)
            {
                case FlightAttitude.Prograde:
                    break;

                case FlightAttitude.Retrograde:
                    rotationReference = rotationReference * Quaternion.AngleAxis(180, Vector3.up);
                    break;

                case FlightAttitude.NormalPlus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.up);
                    break;

                case FlightAttitude.NormalMinus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.down);
                    break;

                case FlightAttitude.RadialPlus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.right);
                    break;

                case FlightAttitude.RadialMinus:
                    rotationReference = rotationReference * Quaternion.AngleAxis(90, Vector3.left);
                    break;

                case FlightAttitude.Surface:
                    rotationReference = rotationReference * extra;
                    break;
            }
            HoldOrientation(fs, f, rotationReference, ignoreRoll);
        }

        public static void HoldOrientation(FlightCtrlState fs, FlightComputer f, Quaternion target, bool ignoreRoll = false)
        {
            f.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            SteeringHelper.SteerShipToward(target, fs, f, ignoreRoll);
        }

        /// <summary>
        /// Checks the needed propellant of an engine. Its always true if infinite fuel is activ
        /// </summary>
        /// <param name="propellants">Propellant for an engine</param>
        /// <returns>True if there are enough propellant to perform</returns>
        public static bool hasPropellant(System.Collections.Generic.List<Propellant> propellants)
        {
            if (CheatOptions.InfinitePropellant) return true;

            foreach (var props in propellants)
            {
                var total = props.totalResourceCapacity;
                var require = props.currentRequirement;
                // check the total capacity and the required amount of proppelant
                if (total <= 0 || require > total)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the total thrust of all activated, not flamed out engines.
        /// </summary>
        /// <param name="v">Current vessel</param>
        /// <returns>Total thrust in kN</returns>
        public static double GetTotalThrust(Vessel v)
        {
            double thrust = 0.0;

            foreach (var pm in v.parts.SelectMany(p => p.FindModulesImplementing<ModuleEngines>()))
            {
                // Notice: flameout is only true if you try to perform with this engine not before
                if (!pm.EngineIgnited || pm.flameout) continue;
                // check for the needed propellant before changing the total thrust
                if (!FlightCore.hasPropellant(pm.propellants)) continue;
                thrust += (double)pm.maxThrust * (pm.thrustPercentage / 100);
            }

            return thrust;
        }
    }

    public static class SteeringHelper
    {
        /// <summary>
        /// Automatically guides the ship to face a desired orientation
        /// </summary>
        /// <param name="target">The desired orientation</param>
        /// <param name="c">The FlightCtrlState for the current vessel.</param>
        /// <param name="fc">The flight computer carrying out the slew</param>
        /// <param name="ignoreRoll">[optional] to ignore the roll</param>
        public static void SteerShipToward(Quaternion target, FlightCtrlState c, FlightComputer fc, bool ignoreRoll)
        {
            // Add support for roll-less targets later -- Starstrider42
            bool fixedRoll = !ignoreRoll;
            Vessel vessel = fc.Vessel;
            Vector3d momentOfInertia = vessel.MOI;
            Transform vesselReference = vessel.GetTransform();
            Vector3d torque = GetTorque(vessel, c.mainThrottle);

            // -----------------------------------------------
            // Copied from MechJeb master on 18.04.2016 with some modifications to adapt to RemoteTech

            Vector3d _axisControl = new Vector3d();
            _axisControl.x = true ? 1 : 0;
            _axisControl.y = true ? 1 : 0;
            _axisControl.z = fixedRoll ? 1 : 0;

            Vector3d inertia = Vector3d.Scale(
                new Vector3d(vessel.angularMomentum.x, vessel.angularMomentum.y, vessel.angularMomentum.z).Sign(),
                Vector3d.Scale(
                    Vector3d.Scale(vessel.angularMomentum, vessel.angularMomentum),
                    Vector3d.Scale(torque, momentOfInertia).Invert()
                    )
                );

            Vector3d TfV = new Vector3d(0.3, 0.3, 0.3);

            double kpFactor = 3;
            double kiFactor = 6;
            double kdFactor = 0.5;
            double kWlimit = 0.15;
            double deadband = 0.0001;

            Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselReference.rotation) * target);

            Vector3d deltaEuler = delta.DeltaEuler();

            // ( MoI / available torque ) factor:
            Vector3d NormFactor = Vector3d.Scale(momentOfInertia, torque.Invert()).Reorder(132);

            // Find out the real shorter way to turn were we want to.
            // Thanks to HoneyFox
            Vector3d tgtLocalUp = vesselReference.rotation.Inverse() * target * Vector3d.forward;
            Vector3d curLocalUp = Vector3d.up;

            double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
            Vector2d rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
            rotDirection = rotDirection.normalized * turnAngle / 180.0;

            // And the lowest roll
            // Thanks to Crzyrndm
            Vector3 normVec = Vector3.Cross(target * Vector3.forward, vesselReference.up);
            Quaternion targetDeRotated = Quaternion.AngleAxis((float)turnAngle, normVec) * target;
            float rollError = Vector3.Angle(vesselReference.right, targetDeRotated * Vector3.right) * Math.Sign(Vector3.Dot(targetDeRotated * Vector3.right, vesselReference.forward));

            var error = new Vector3d(
                -rotDirection.y * Math.PI,
                rotDirection.x * Math.PI,
                rollError * Mathf.Deg2Rad
                );

            error.Scale(_axisControl);

            Vector3d err = error + inertia.Reorder(132) / 2d;
            err = new Vector3d(
                Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));

            err.Scale(NormFactor);

            // angular velocity:
            Vector3d omega;
            omega.x = vessel.angularVelocity.x;
            omega.y = vessel.angularVelocity.z; // y <=> z
            omega.z = vessel.angularVelocity.y; // z <=> y
            omega.Scale(NormFactor);

            //if (Tf_autoTune)
            //    tuneTf(torque);

            Vector3d invTf = TfV.Invert();
            fc.pid.Kd = kdFactor * invTf;

            fc.pid.Kp = (1 / (kpFactor * Math.Sqrt(2))) * fc.pid.Kd;
            fc.pid.Kp.Scale(invTf);

            fc.pid.Ki = (1 / (kiFactor * Math.Sqrt(2))) * fc.pid.Kp;
            fc.pid.Ki.Scale(invTf);

            fc.pid.intAccum = fc.pid.intAccum.Clamp(-5, 5);

            // angular velocity limit:
            var Wlimit = new Vector3d(Math.Sqrt(NormFactor.x * Math.PI * kWlimit),
                                       Math.Sqrt(NormFactor.y * Math.PI * kWlimit),
                                       Math.Sqrt(NormFactor.z * Math.PI * kWlimit));

            Vector3d pidAction = fc.pid.Compute(err, omega, Wlimit);

            // deadband
            pidAction.x = Math.Abs(pidAction.x) >= deadband ? pidAction.x : 0.0;
            pidAction.y = Math.Abs(pidAction.y) >= deadband ? pidAction.y : 0.0;
            pidAction.z = Math.Abs(pidAction.z) >= deadband ? pidAction.z : 0.0;

            // low pass filter,  wf = 1/Tf:
            Vector3d act = fc.lastAct;
            act.x += (pidAction.x - fc.lastAct.x) * (1.0 / ((TfV.x / TimeWarp.fixedDeltaTime) + 1.0));
            act.y += (pidAction.y - fc.lastAct.y) * (1.0 / ((TfV.y / TimeWarp.fixedDeltaTime) + 1.0));
            act.z += (pidAction.z - fc.lastAct.z) * (1.0 / ((TfV.z / TimeWarp.fixedDeltaTime) + 1.0));
            fc.lastAct = act;

            // end MechJeb import
            //---------------------------------------

            float precision = Mathf.Clamp((float)(torque.x * 20f / momentOfInertia.magnitude), 0.5f, 10f);
            float driveLimit = Mathf.Clamp01((float)(err.magnitude * 380.0f / precision));

            act.x = Mathf.Clamp((float)act.x, -driveLimit, driveLimit);
            act.y = Mathf.Clamp((float)act.y, -driveLimit, driveLimit);
            act.z = Mathf.Clamp((float)act.z, -driveLimit, driveLimit);

            c.roll = Mathf.Clamp((float)(c.roll + act.z), -driveLimit, driveLimit);
            c.pitch = Mathf.Clamp((float)(c.pitch + act.x), -driveLimit, driveLimit);
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -driveLimit, driveLimit);
        }

        public static Vector3d SwapYZ(Vector3d input)
        {
            return new Vector3d(input.x, input.z, input.y);
        }

        public static Vector3d Pow(Vector3d vector, float exponent)
        {
            return new Vector3d(Math.Pow(vector.x, exponent), Math.Pow(vector.y, exponent), Math.Pow(vector.z, exponent));
        }

        // Copied from MechJeb master on June 27, 2014
        private class Matrix3x3
        {
            //row index, then column index
            private readonly double[,] e = new double[3, 3];

            public double this[int i, int j]
            {
                get { return e[i, j]; }
                set { e[i, j] = value; }
            }

            public static Vector3d operator *(Matrix3x3 m, Vector3d v)
            {
                Vector3d ret = Vector3d.zero;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        ret[i] += m.e[i, j] * v[j];
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// Returns the torque the ship can exert around its center of mass
        /// </summary>
        /// <returns>The torque in N m, around the (pitch, roll, yaw) axes.</returns>
        /// <param name="vessel">The ship whose torque should be measured.</param>
        /// <param name="thrust">The ship's throttle setting, on a scale of 0 to 1.</param>
        public static Vector3d GetTorque(Vessel vessel, float thrust)
        {
            // Do everything in vessel coordinates
            var centerOfMass = vessel.CoM;

            // Don't assume any particular symmetry for the vessel
            double pitch = 0, roll = 0, yaw = 0;

            foreach (Part part in vessel.parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    if (!module.isEnabled)
                        continue;

                    var reactionWheelModule = module as ModuleReactionWheel;
                    var rcsModule = module as ModuleRCS;
                    if (reactionWheelModule != null && reactionWheelModule.wheelState == ModuleReactionWheel.WheelState.Active)
                    {
                        pitch += reactionWheelModule.PitchTorque;
                        roll += reactionWheelModule.RollTorque;
                        yaw += reactionWheelModule.YawTorque;
                    }
                        // Is there a more direct way to see if RCS is enabled? module.isEnabled doesn't work...
                    else if (rcsModule != null && vessel.ActionGroups[KSPActionGroup.RCS])
                    {
                        var vesselTransform = vessel.GetTransform();
                        foreach (Transform thruster in rcsModule.thrusterTransforms)
                        {
                            // Avoids problems with part.Rigidbody.centerOfMass; should also give better
                            //  support for RCS units integrated into larger parts
                            Vector3d thrusterOffset = vesselTransform.InverseTransformPoint(thruster.position) - centerOfMass;
                            /* Code by sarbian, shamelessly copied from MechJeb */
                            Vector3d thrusterThrust = vesselTransform.InverseTransformDirection(-thruster.up.normalized) * rcsModule.thrusterPower;
                            Vector3d thrusterTorque = Vector3.Cross(thrusterOffset, thrusterThrust);
                            /* end sarbian's code */

                            // This overestimates the usable torque, but that doesn't change the final behavior much
                            pitch += (float)Math.Abs(thrusterTorque.x);
                            roll += (float)Math.Abs(thrusterTorque.y);
                            yaw += (float)Math.Abs(thrusterTorque.z);
                        }
                    }
                }

                Vector3d gimbal = GetThrustTorque(part, vessel) * thrust;
                pitch += gimbal.x;
                roll += gimbal.y;
                yaw += gimbal.z;
            }

            return new Vector3d(pitch, roll, yaw);
        }

        /// <summary>
        /// Returns the maximum torque the ship can exert by gimbaling its engines while at full throttle
        /// </summary>
        /// <returns>The torque in N m, around the (pitch, roll, yaw) axes.</returns>
        /// <param name="p">The part providing the torque. Need not be an engine.</param>
        /// <param name="vessel">The vessel to which the torque is applied.</param>
        public static Vector3d GetThrustTorque(Part p, Vessel vessel)
        {
            double result = 0.0;
            foreach (ModuleGimbal gimbal in p.Modules.OfType<ModuleGimbal>())
            {
                if (gimbal.gimbalLock)
                    continue;

                // Standardize treatment of ModuleEngines and ModuleEnginesFX; 
                //      IEngineStatus doesn't have the needed fields
                double maxThrust = 0.0;
                // Assume exactly one module of EITHER type ModuleEngines or ModuleEnginesFX exists in `p`
                bool engineFound = false;
                {
                    ModuleEngines engine = p.Modules.OfType<ModuleEngines>().FirstOrDefault();
                    if (engine != null)
                    {
                        if (!engine.isOperational)
                            continue;
                        engineFound = true;
                        maxThrust = engine.maxThrust;
                    }
                }
                // Dummy ModuleGimbal, does nothing
                if (!engineFound)
                    continue;

                double gimbalRadians = Math.Sin(Math.Abs(gimbal.gimbalRange) * Math.PI / 180);
                result = gimbalRadians * maxThrust * (p.Rigidbody.worldCenterOfMass - vessel.CoM).magnitude;
            }

            // Better to overestimate roll torque than to underestimate it... calculate properly later
            return new Vector3d(result, result, result);
        }

        private static Vector3d ReduceAngles(Vector3d input)
        {
            return new Vector3d(
                  (input.x > 180f) ? (input.x - 360f) : input.x,
                  (input.y > 180f) ? (input.y - 360f) : input.y,
                  (input.z > 180f) ? (input.z - 360f) : input.z
                  );
        }

        public static Vector3d Sign(Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }
    }
}