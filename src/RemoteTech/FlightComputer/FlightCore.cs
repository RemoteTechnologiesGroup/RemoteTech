using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public static class FlightCore
    {
        public static void HoldAttitude(FlightCtrlState fs, FlightComputer f, ReferenceFrame frame, FlightAttitude attitude, Quaternion extra)
        {
            var v = f.Vessel;
            var forward = Vector3.zero;
            var up = Vector3.zero;
            switch (frame)
            {
                case ReferenceFrame.Orbit:
                    forward = v.GetObtVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;

                case ReferenceFrame.Surface:
                    forward = v.GetSrfVelocity();
                    up = (v.mainBody.position - v.CoM);
                    break;

                case ReferenceFrame.North:
                    up = (v.mainBody.position - v.CoM);
                    forward = Vector3.Exclude(up,
                        v.mainBody.position + v.mainBody.transform.up * (float)v.mainBody.Radius - v.CoM
                     );
                    break;

                case ReferenceFrame.Maneuver:
                    if (f.DelayedManeuver != null)
                    {
                        forward = f.DelayedManeuver.GetBurnVector(v.orbit);
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
            HoldOrientation(fs, f, rotationReference);
        }

        public static void HoldOrientation(FlightCtrlState fs, FlightComputer f, Quaternion target)
        {
            f.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            kOS.SteeringHelper.SteerShipToward(target, fs, f);
        }

        public static double GetTotalThrust(Vessel v)
        {
            double thrust = 0.0;
            foreach (var pm in v.parts.SelectMany(p => p.FindModulesImplementing<ModuleEngines>()))
            {
                if (!pm.EngineIgnited) continue;
                thrust += (double)pm.maxThrust * (pm.thrustPercentage / 100);
            }
            foreach (var pm in v.parts.SelectMany(p => p.FindModulesImplementing<ModuleEnginesFX>()))
            {
                if (!pm.EngineIgnited) continue;
                thrust += (double)pm.maxThrust * (pm.thrustPercentage / 100);
            }
            return thrust;
        }
    }
}

namespace kOS
{
    public static class SteeringHelper
    {
        /// <summary>
        /// Automatically guides the ship to face a desired orientation
        /// </summary>
        /// <param name="target">The desired orientation</param>
        /// <param name="c">The FlightCtrlState for the current vessel.</param>
        /// <param name="fc">The flight computer carrying out the slew</param>
        public static void SteerShipToward(Quaternion target, FlightCtrlState c, RemoteTech.FlightComputer fc)
        {
            // Add support for roll-less targets later -- Starstrider42
            bool fixedRoll = true;
            Vessel vessel = fc.Vessel;
            Vector3d momentOfInertia = GetTrueMoI(vessel);
            Transform vesselReference = vessel.GetTransform();

            //---------------------------------------
            // Copied almost verbatim from MechJeb master on June 27, 2014 -- Starstrider42

            Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselReference.rotation) * target);

            Vector3d torque = GetTorque(vessel, c.mainThrottle);
            Vector3d spinMargin = GetStoppingAngle(vessel, torque);

            // Allow for zero torque around some but not all axes
            Vector3d normFactor;
            normFactor.x = (torque.x != 0 ? momentOfInertia.x / torque.x : 0.0);
            normFactor.y = (torque.y != 0 ? momentOfInertia.y / torque.y : 0.0);
            normFactor.z = (torque.z != 0 ? momentOfInertia.z / torque.z : 0.0);
            normFactor = SwapYZ(normFactor);

            // Find out the real shorter way to turn were we want to.
            // Thanks to HoneyFox

            Vector3d tgtLocalUp = vesselReference.transform.rotation.Inverse() * target * Vector3d.forward;
            Vector3d curLocalUp = Vector3d.up;

            double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
            var rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
            rotDirection = rotDirection.normalized * turnAngle / 180.0f;

            var err = new Vector3d(
                -rotDirection.y * Math.PI,
                rotDirection.x * Math.PI,
                fixedRoll ?
                    ((delta.eulerAngles.z > 180) ?
                        (delta.eulerAngles.z - 360.0F) :
                        delta.eulerAngles.z) * Math.PI / 180.0F
                    : 0F
            );

            err += SwapYZ(spinMargin);
            err = new Vector3d(Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));
            err.Scale(normFactor);

            // angular velocity:
            Vector3d omega = SwapYZ(vessel.angularVelocity);
            omega.Scale(normFactor);

            Vector3d pidAction = fc.pid.Compute(err, omega);

            // low pass filter, wf = 1/Tf:
            Vector3d act = fc.lastAct + (pidAction - fc.lastAct) * (1 / ((fc.Tf / TimeWarp.fixedDeltaTime) + 1));
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
        /// Returns a more accurate moment of inertia than Vessel.findLocalMOI()
        /// </summary>
        // Copied from MechJeb master on June 27, 2014
        // TODO: cache moment if inertia and update only when ship mass changes?
        private static Vector3d GetTrueMoI(Vessel vessel)
        {
            var inertiaTensor = new Matrix3x3();
            var centerOfMass = vessel.findWorldCenterOfMass();

            foreach (Part p in vessel.parts)
            {
                if (p.Rigidbody == null) continue;

                //Compute the contributions to the vessel inertia tensor due to the part inertia tensor
                Vector3d principalMoments = p.Rigidbody.inertiaTensor;
                Quaternion princAxesRot = Quaternion.Inverse(vessel.GetTransform().rotation) * p.transform.rotation * p.Rigidbody.inertiaTensorRotation;
                Quaternion invPrincAxesRot = Quaternion.Inverse(princAxesRot);

                for (int i = 0; i < 3; i++)
                {
                    Vector3d iHat = Vector3d.zero;
                    iHat[i] = 1;
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3d jHat = Vector3d.zero;
                        jHat[j] = 1;
                        inertiaTensor[i, j] += Vector3d.Dot(iHat, princAxesRot * Vector3d.Scale(principalMoments, invPrincAxesRot * jHat));
                    }
                }

                //Compute the contributions to the vessel inertia tensor due to the part mass and position
                double partMass = p.mass + p.GetResourceMass();
                Vector3 partPosition = vessel.GetTransform().InverseTransformDirection(p.Rigidbody.worldCenterOfMass - centerOfMass);

                for (int i = 0; i < 3; i++)
                {
                    inertiaTensor[i, i] += partMass * partPosition.sqrMagnitude;

                    for (int j = 0; j < 3; j++)
                    {
                        inertiaTensor[i, j] += -partMass * partPosition[i] * partPosition[j];
                    }
                }
            }

            return new Vector3d(inertiaTensor[0, 0], inertiaTensor[1, 1], inertiaTensor[2, 2]);
        }

        /// <summary>
        /// Calculates how far a ship can rotate before its rotation stops.
        /// </summary>
        /// <returns>A vector equal to the stopping angle, in radians, around the (pitch, roll, yaw) axes. 
        /// If it is impossible to stop the ship along one axis, returns 0 for that axis.</returns>
        /// <param name="vessel">The ship whose rotation needs to be stopped.</param>
        /// <param name="torque">The torque that can be applied to the ship.</param>
        public static Vector3d GetStoppingAngle(Vessel vessel, Vector3d torque)
        {
            var momentOfInertia = GetTrueMoI(vessel);
            var angularVelocity = Quaternion.Inverse(vessel.transform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = Vector3d.Scale(angularVelocity, momentOfInertia);

            // Adapted from MechJeb master on June 27, 2014
            Vector3d retVar;
            retVar.x = (torque.x != 0.0 ? angularMomentum.x*angularMomentum.x / (torque.x*momentOfInertia.x) : 0.0);
            retVar.y = (torque.y != 0.0 ? angularMomentum.y*angularMomentum.y / (torque.y*momentOfInertia.y) : 0.0);
            retVar.z = (torque.z != 0.0 ? angularMomentum.z*angularMomentum.z / (torque.z*momentOfInertia.z) : 0.0);
            retVar.Scale(Sign(angularMomentum));

            return retVar / 2;
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
            var centerOfMass = vessel.findLocalCenterOfMass();

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
                roll  += gimbal.y;
                yaw   += gimbal.z;
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
            foreach (ModuleGimbal gimbal in p.Modules.OfType<ModuleGimbal>()) {
                if (gimbal.gimbalLock)
                    continue;

                // Standardize treatment of ModuleEngines and ModuleEnginesFX; 
                //      IEngineStatus doesn't have the needed fields
                double maxThrust = 0.0;
                // Assume exactly one module of EITHER type ModuleEngines or ModuleEnginesFX exists in `p`
                bool engineFound = false;
                {
                    ModuleEngines engine = p.Modules.OfType<ModuleEngines>().FirstOrDefault();
                    if (engine != null) {
                        if (!engine.isOperational)
                            continue;
                        engineFound = true;
                        maxThrust = engine.maxThrust;
                    }
                }
                {
                    ModuleEnginesFX engine = p.Modules.OfType<ModuleEnginesFX>().FirstOrDefault();
                    if (engine != null) {
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
