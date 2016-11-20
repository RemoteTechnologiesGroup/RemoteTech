using System;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Extensions;
using RemoteTech.Common.Interfaces.FlightComputer;
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
            var ignoreRoll = false;

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
            var rotationReference = Quaternion.LookRotation(forward, up);
            
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

        public static void HoldOrientation(FlightCtrlState fs, IFlightComputer f, Quaternion target, bool ignoreRoll = false)
        {
            f.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            SteeringHelper.SteerShipToward(target, fs, f, ignoreRoll);
        }

        /// <summary>
        /// Checks the needed propellant of an engine. Its always true if infinite fuel is active.
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
                // check the total capacity and the required amount of propellant
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
            var thrust = 0.0;

            foreach (var pm in v.parts.SelectMany(p => p.FindModulesImplementing<ModuleEngines>()))
            {
                // Notice: flame-out is only true if you try to perform with this engine not before
                if (!pm.EngineIgnited || pm.flameout) continue;
                // check for the needed propellant before changing the total thrust
                if (!hasPropellant(pm.propellants)) continue;
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
        public static void SteerShipToward(Quaternion target, FlightCtrlState c, IFlightComputer fc, bool ignoreRoll)
        {
            // Add support for roll-less targets later -- Starstrider42
            var fixedRoll = !ignoreRoll;
            var vessel = fc.Vessel;
            Vector3d momentOfInertia = vessel.MOI;
            Vector3d torque = GetVesselTorque(vessel);

            if (FlightCore.UseSas)
            {
                if (vessel.ActionGroups[KSPActionGroup.SAS])
                    return;

                InputLockManager.RemoveControlLock("RTLockSAS");
                vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                InputLockManager.SetControlLock(ControlTypes.SAS, "RTLockSAS");
                RTLog.Notify("Autopilot enabled: {0}", vessel.Autopilot.Enabled);
                RTLog.Notify("Autopilot CanEngageSAS: {0}", vessel.Autopilot.SAS.CanEngageSAS());
                vessel.Autopilot.SAS.SetTargetOrientation(target * Vector3.up, false);

                return;
            }

            // -----------------------------------------------
            // prepare mechjeb values

            var _axisControl = new Vector3d();
            _axisControl.x = true ? 1 : 0;
            _axisControl.y = true ? 1 : 0;
            _axisControl.z = fixedRoll ? 1 : 0;

            // see mechjeb UpdateMoIAndAngularMom() in VesselState.cs
            var angularMomentum = Vector3d.zero;
            angularMomentum.x = momentOfInertia.x * vessel.angularVelocity.x;
            angularMomentum.y = momentOfInertia.y * vessel.angularVelocity.y;
            angularMomentum.z = momentOfInertia.z * vessel.angularVelocity.z;

            var inertia = Vector3d.Scale(
                angularMomentum.Sign(),
                Vector3d.Scale(
                    Vector3d.Scale(angularMomentum, angularMomentum),
                    Vector3d.Scale(torque, momentOfInertia).InvertNoNaN()
                    )
                );

            var TfV = new Vector3d(0.3, 0.3, 0.3);

            double kpFactor = 3;
            double kiFactor = 6;
            var kdFactor = 0.5;
            var kWlimit = 0.15;
            var deadband = 0.0001;

            /* -------------------------------------------------------------------------------
             * Start MechJeb code; from MechJebModuleAttitudeController.cs in Drive() function 
             * Updated: 2016-10-22
             */

            var vesselTransform = vessel.ReferenceTransform;

            // Find out the real shorter way to turn where we wan to.
            // Thanks to HoneyFox
            Vector3d tgtLocalUp = vesselTransform.transform.rotation.Inverse() * target * Vector3d.forward;
            var curLocalUp = Vector3d.up;

            var turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
            var rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
            rotDirection = rotDirection.normalized * turnAngle / 180.0;

            // And the lowest roll
            // Thanks to Crzyrndm
            var normVec = Vector3.Cross(target * Vector3.forward, vesselTransform.up);
            var targetDeRotated = Quaternion.AngleAxis((float)turnAngle, normVec) * target;
            var rollError = Vector3.Angle(vesselTransform.right, targetDeRotated * Vector3.right) * Math.Sign(Vector3.Dot(targetDeRotated * Vector3.right, vesselTransform.forward));


            // From here everything should use MOI order for Vectors (pitch, roll, yaw)
            var error = new Vector3d(
                -rotDirection.y * Math.PI,
                rollError * Mathf.Deg2Rad,
                rotDirection.x * Math.PI
                );

            error.Scale(_axisControl);

            var err = error + inertia * 0.5;
            err = new Vector3d(
                Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));

            // ( MoI / available torque ) factor:
            var NormFactor = Vector3d.Scale(momentOfInertia, torque.InvertNoNaN());

            err.Scale(NormFactor);

            // angular velocity:
            Vector3d omega = vessel.angularVelocity;
            omega.Scale(NormFactor);

            SetPIDParameters(fc, TfV, kdFactor, kpFactor, kiFactor);

            // angular velocity limit:
            var Wlimit = new Vector3d(Math.Sqrt(NormFactor.x * Math.PI * kWlimit),
                                       Math.Sqrt(NormFactor.y * Math.PI * kWlimit),
                                       Math.Sqrt(NormFactor.z * Math.PI * kWlimit));

            var pidAction = fc.pid.Compute(err, omega, Wlimit);

            // deadband
            pidAction.x = Math.Abs(pidAction.x) >= deadband ? pidAction.x : 0.0;
            pidAction.y = Math.Abs(pidAction.y) >= deadband ? pidAction.y : 0.0;
            pidAction.z = Math.Abs(pidAction.z) >= deadband ? pidAction.z : 0.0;

            // low pass filter,  wf = 1/Tf:
            var act = fc.lastAct;
            act.x += (pidAction.x - fc.lastAct.x) * (1.0 / ((TfV.x / TimeWarp.fixedDeltaTime) + 1.0));
            act.y += (pidAction.y - fc.lastAct.y) * (1.0 / ((TfV.y / TimeWarp.fixedDeltaTime) + 1.0));
            act.z += (pidAction.z - fc.lastAct.z) * (1.0 / ((TfV.z / TimeWarp.fixedDeltaTime) + 1.0));
            fc.lastAct = act;

            // end MechJeb import
            //---------------------------------------

            /*
            float precision = Mathf.Clamp((float)(torque.x * 20f / momentOfInertia.magnitude), 0.5f, 10f);
            float driveLimit = Mathf.Clamp01((float)(err.magnitude * 380.0f / precision));

            act.x = Mathf.Clamp((float)act.x, -driveLimit, driveLimit);
            act.y = Mathf.Clamp((float)act.y, -driveLimit, driveLimit);
            act.z = Mathf.Clamp((float)act.z, -driveLimit, driveLimit);
            */
            float driveLimit = 1;

            if (!double.IsNaN(act.y)) c.roll = Mathf.Clamp((float)(act.y), -driveLimit, driveLimit);
            if (!double.IsNaN(act.x)) c.pitch = Mathf.Clamp((float)(act.x), -driveLimit, driveLimit);
            if (!double.IsNaN(act.z)) c.yaw = Mathf.Clamp((float)(act.z), -driveLimit, driveLimit);

            /*
            c.roll = Mathf.Clamp((float)(c.roll + act.z), -driveLimit, driveLimit);
            c.pitch = Mathf.Clamp((float)(c.pitch + act.x), -driveLimit, driveLimit);
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -driveLimit, driveLimit);
            */
        }

        private static void SetPIDParameters(FlightComputer fc, Vector3d TfV, double kdFactor, double kpFactor, double kiFactor)
        {
            var pid = fc.pid;

            var invTf = TfV.InvertNoNaN();
            pid.Kd = kdFactor * invTf;

            pid.Kp = (1 / (kpFactor * Math.Sqrt(2))) * pid.Kd;
            pid.Kp.Scale(invTf);

            pid.Ki = (1 / (kiFactor * Math.Sqrt(2))) * pid.Kp;
            pid.Ki.Scale(invTf);

            pid.intAccum = pid.intAccum.Clamp(-5, 5);
        }

        public static Vector3d SwapYZ(Vector3d input)
        {
            return new Vector3d(input.x, input.z, input.y);
        }

        public static Vector3d Pow(Vector3d vector, float exponent)
        {
            return new Vector3d(Math.Pow(vector.x, exponent), Math.Pow(vector.y, exponent), Math.Pow(vector.z, exponent));
        }

        public static Vector3d InvertNoNaN(this Vector3d vector)
        {
            return new Vector3d(vector.x != 0 ? 1 / vector.x : 0, vector.y != 0 ? 1 / vector.y : 0, vector.z != 0 ? 1 / vector.z : 0);
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
                var ret = Vector3d.zero;
                for (var i = 0; i < 3; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        ret[i] += m.e[i, j] * v[j];
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// Get the total torque for a vessel.
        /// </summary>
        /// <param name="vessel">The vessel from which to get the total torque.</param>
        /// <returns>The vessel torque as a Vector3.</returns>
        public static Vector3 GetVesselTorque(Vessel vessel)
        {
            // the resulting torque
            var vesselTorque = Vector3.zero;

            // positive and negative vessel torque for all part modules that are torque providers
            var positiveTorque = Vector3.zero;
            var negativeTorque = Vector3.zero;

            // cycle through all vessel parts.
            var partCount = vessel.Parts.Count;
            for(var iPart = 0; iPart < partCount; ++iPart)
            {
                var part = vessel.Parts[iPart];

                // loop through all modules for the part
                var moduleCount = part.Modules.Count;
                for (var iModule = 0; iModule < moduleCount; ++iModule)
                {
                    // find modules in part that are torque providers.
                    var torqueProvider = part.Modules[iModule] as ITorqueProvider;
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