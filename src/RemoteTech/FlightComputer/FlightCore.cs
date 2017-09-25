using System;
using System.Linq;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.FlightComputer
{
    public static class FlightCore
    {
        public static bool UseSas = true;

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

                    if (f.Vessel.patchedConicSolver == null)//scenario: two vessels within physical range with FC attitude hold cmds. Unloaded one doesn't have solver instance
                    {
                        f.Vessel.AttachPatchedConicsSolver();
                        f.Vessel.patchedConicSolver.Update();
                    }

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
                    if (f.DelayedTarget != null) // either target vessel or celestial body
                    {
                        forward = f.DelayedTarget.GetTransform().position - v.CoM;
                        up = (v.mainBody.position - v.CoM);
                    }
                    else // no target to aim; default to orbital prograde
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
        private const double outputDeadband = 0.001;
        private const float driveLimit = 1.0f;

        /// <summary>
        /// Automatically guides the ship to face a desired orientation
        /// </summary>
        /// <param name="target">The desired orientation</param>
        /// <param name="c">The FlightCtrlState for the current vessel.</param>
        /// <param name="fc">The flight computer carrying out the slew</param>
        /// <param name="ignoreRoll">[optional] to ignore the roll</param>
        public static void SteerShipToward(Quaternion target, FlightCtrlState c, FlightComputer fc, bool ignoreRoll)
        {
            Vessel vessel = fc.Vessel;

            var actuation = fc.PIDController.GetActuation(target);

            // deadband
            actuation.x = Math.Abs(actuation.x) >= outputDeadband ? actuation.x : 0.0;
            actuation.y = Math.Abs(actuation.y) >= outputDeadband ? actuation.y : 0.0;
            actuation.z = Math.Abs(actuation.z) >= outputDeadband ? actuation.z : 0.0;

            // update the flight controls
            c.pitch = Mathf.Clamp((float) actuation.x, -driveLimit, driveLimit);
            c.roll = !ignoreRoll ? Mathf.Clamp((float) actuation.y, -driveLimit, driveLimit) : 0.0f;
            c.yaw = Mathf.Clamp((float) actuation.z, -driveLimit, driveLimit);
        }

        /// <summary>
        /// Get the total torque for a vessel.
        /// </summary>
        /// <param name="vessel">The vessel from which ot get the total torque.</param>
        /// <returns>The vessel torque as a Vector3.</returns>
        public static Vector3 GetVesselTorque(Vessel vessel)
        {
            // the resulting torque
            Vector3 vesselTorque = Vector3.zero;

            // positive and negative vessel torque for all part modules that are torque providers
            Vector3 positiveTorque = Vector3.zero;
            Vector3 negativeTorque = Vector3.zero;

            // cycle through all vessel parts.
            int partCount = vessel.Parts.Count;
            for(int iPart = 0; iPart < partCount; ++iPart)
            {
                Part part = vessel.Parts[iPart];

                // loop through all modules for the part
                int moduleCount = part.Modules.Count;
                for (int iModule = 0; iModule < moduleCount; ++iModule)
                {
                    // find modules in part that are torque providers.
                    ITorqueProvider torqueProvider = part.Modules[iModule] as ITorqueProvider;
                    if (torqueProvider == null)
                        continue;

                    // pos and neg torque for this part module
                    Vector3 posTorque;
                    Vector3 negTorque;

                    // get potential torque for the current module and update pos and neg torques.
                    torqueProvider.GetPotentialTorque(out posTorque, out negTorque);
                    positiveTorque += posTorque;
                    negativeTorque += negTorque;
                }
            }

            // get max torque from all components of pos and neg torques.
            vesselTorque.x = Mathf.Max(positiveTorque.x, negativeTorque.x);
            vesselTorque.y = Mathf.Max(positiveTorque.y, negativeTorque.y);
            vesselTorque.z = Mathf.Max(positiveTorque.z, negativeTorque.z);

            return vesselTorque;
        }
    }
}