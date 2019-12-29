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

        /// <summary>
        /// Single entry point of all Flight Computer orientation holding, including maneuver node.
        /// </summary>
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

        /* MechJeb2 torque variables */
        private static Vector6 torqueReactionWheel = new Vector6();
        private static Vector6 torqueGimbal = new Vector6();
        private static Vector6 torqueOthers = new Vector6();
        private static Vector6 torqueControlSurface = new Vector6();
        private static Vector6 rcsThrustAvailable = new Vector6();
        private static Vector6 rcsTorqueAvailable = new Vector6();

        private static Vector3d torqueAvailable = Vector3d.zero;
        public static Vector3d TorqueAvailable
        {
            get { return torqueAvailable; }
        }

        private static Vector3d torqueReactionSpeed;
        public static Vector3d TorqueReactionSpeed
        {
            get { return torqueReactionSpeed; }
        }

        /// <summary>
        /// Automatically guides the ship to face a desired orientation
        /// </summary>
        /// <param name="target">The desired orientation</param>
        /// <param name="c">The FlightCtrlState for the current vessel.</param>
        /// <param name="fc">The flight computer carrying out the slew</param>
        /// <param name="ignoreRoll">[optional] to ignore the roll</param>
        public static void SteerShipToward(Quaternion target, FlightCtrlState c, FlightComputer fc, bool ignoreRoll)
        {
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
        /// Import from MechJeb2
        /// https://github.com/MuMech/MechJeb2/blob/dev/MechJeb2/VesselState.cs
        /// </summary>
        public static void AnalyzeParts(Vessel vessel)
        {
            torqueAvailable = Vector3d.zero;
            Vector6 torqueReactionSpeed6 = new Vector6();

            torqueReactionWheel.Reset();
            torqueControlSurface.Reset();
            torqueGimbal.Reset();
            torqueOthers.Reset();

            UpdateRCSThrustAndTorque(vessel);

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];

                for (int m = 0; m < p.Modules.Count; m++)
                {
                    PartModule pm = p.Modules[m];
                    if (!pm.isEnabled)
                    {
                        continue;
                    }

                    ModuleReactionWheel rw = pm as ModuleReactionWheel;
                    if (rw != null)
                    {
                        Vector3 pos;
                        Vector3 neg;
                        rw.GetPotentialTorque(out pos, out neg);

                        // GetPotentialTorque reports the same value for pos & neg on ModuleReactionWheel
                        torqueReactionWheel.Add(pos);
                        torqueReactionWheel.Add(-neg);
                    }
                    else if (pm is ModuleControlSurface) // also does ModuleAeroSurface
                    {
                        ModuleControlSurface cs = (pm as ModuleControlSurface);

                        Vector3 ctrlTorquePos;
                        Vector3 ctrlTorqueNeg;

                        cs.GetPotentialTorque(out ctrlTorquePos, out ctrlTorqueNeg);

                        torqueControlSurface.Add(ctrlTorquePos);
                        torqueControlSurface.Add(ctrlTorqueNeg);

                        torqueReactionSpeed6.Add(Mathf.Abs(cs.ctrlSurfaceRange) / cs.actuatorSpeed * Vector3d.Max(ctrlTorquePos.Abs(), ctrlTorqueNeg.Abs()));
                    }
                    else if (pm is ModuleGimbal)
                    {
                        ModuleGimbal g = (pm as ModuleGimbal);

                        Vector3 pos;
                        Vector3 neg;

                        g.GetPotentialTorque(out pos, out neg);
                        // GetPotentialTorque reports the same value for pos & neg on ModuleGimbal

                        torqueGimbal.Add(pos);
                        torqueGimbal.Add(-neg);

                        if (g.useGimbalResponseSpeed)
                            torqueReactionSpeed6.Add((Mathf.Abs(g.gimbalRange) / g.gimbalResponseSpeed) * Vector3d.Max(pos.Abs(), neg.Abs()));
                    }
                    else if (pm is ModuleRCS)
                    {
                        // Handled separately
                    }
                    else if (pm is ITorqueProvider)
                    {
                        ITorqueProvider tp = pm as ITorqueProvider;

                        Vector3 pos;
                        Vector3 neg;
                        tp.GetPotentialTorque(out pos, out neg);

                        torqueOthers.Add(pos);
                        torqueOthers.Add(neg);
                    }
                }
            }

            torqueAvailable += Vector3d.Max(torqueReactionWheel.positive, torqueReactionWheel.negative);
            torqueAvailable += Vector3d.Max(rcsTorqueAvailable.positive, rcsTorqueAvailable.negative);
            torqueAvailable += Vector3d.Max(torqueControlSurface.positive, torqueControlSurface.negative);
            torqueAvailable += Vector3d.Max(torqueGimbal.positive, torqueGimbal.negative);
            torqueAvailable += Vector3d.Max(torqueOthers.positive, torqueOthers.negative);

            if (torqueAvailable.sqrMagnitude > 0)
            {
                torqueReactionSpeed = Vector3d.Max(torqueReactionSpeed6.positive, torqueReactionSpeed6.negative);
                torqueReactionSpeed.Scale(torqueAvailable.InvertNoNaN());
            }
            else
            {
                torqueReactionSpeed = Vector3d.zero;
            }
        }

        /// <summary>
        /// Minor helpers for MechJeb2 work
        /// </summary>
        public static Vector3d InvertNoNaN(this Vector3d vector)
        {
            return new Vector3d(vector.x != 0 ? 1 / vector.x : 0, vector.y != 0 ? 1 / vector.y : 0, vector.z != 0 ? 1 / vector.z : 0);
        }
        public static Vector3 Abs(this Vector3 vector)
        {
            return new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
        }

        /// <summary>
        /// Import from MechJeb2
        /// https://github.com/MuMech/MechJeb2/blob/dev/MechJeb2/VesselState.cs
        /// </summary>
        public static void UpdateRCSThrustAndTorque(Vessel vessel)
        {
            rcsThrustAvailable.Reset();
            rcsTorqueAvailable.Reset();

            if (!vessel.ActionGroups[KSPActionGroup.RCS])
            {
                return;
            }

            Vector3d movingCoM = vessel.CurrentCoM;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                for (int m = 0; m < p.Modules.Count; m++)
                {
                    ModuleRCS rcs = p.Modules[m] as ModuleRCS;

                    if (rcs == null)
                        continue;

                    if (!p.ShieldedFromAirstream && rcs.rcsEnabled && rcs.isEnabled && !rcs.isJustForShow)
                    {
                        Vector3 attitudeControl = new Vector3(rcs.enablePitch ? 1 : 0, rcs.enableRoll ? 1 : 0, rcs.enableYaw ? 1 : 0);
                        Vector3 translationControl = new Vector3(rcs.enableX ? 1 : 0f, rcs.enableZ ? 1 : 0, rcs.enableY ? 1 : 0);

                        for (int j = 0; j < rcs.thrusterTransforms.Count; j++)
                        {
                            Transform t = rcs.thrusterTransforms[j];
                            Vector3d thrusterPosition = t.position - movingCoM;
                            Vector3d thrustDirection = rcs.useZaxis ? -t.forward : -t.up;

                            float power = rcs.thrusterPower;

                            if (FlightInputHandler.fetch.precisionMode)
                            {
                                if (rcs.useLever)
                                {
                                    float lever = rcs.GetLeverDistance(t, thrustDirection, movingCoM);
                                    if (lever > 1)
                                    {
                                        power = power / lever;
                                    }
                                }
                                else
                                {
                                    power *= rcs.precisionFactor;
                                }
                            }

                            Vector3d thrusterThrust = thrustDirection * power;

                            rcsThrustAvailable.Add(Vector3.Scale(vessel.GetTransform().InverseTransformDirection(thrusterThrust), translationControl));
                            Vector3d thrusterTorque = Vector3.Cross(thrusterPosition, thrusterThrust);

                            // Convert in vessel local coordinate
                            rcsTorqueAvailable.Add(Vector3.Scale(vessel.GetTransform().InverseTransformDirection(thrusterTorque), attitudeControl));
                        }
                    }
                }
            }
        }
    }
}