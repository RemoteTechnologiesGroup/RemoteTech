using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var rotationReference = Quaternion.identity;
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
                    up = v.transform.up;
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
                    if (f.DelayedTarget != null && f.DelayedTarget is Vessel)
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
                    if (f.DelayedTarget != null && f.DelayedTarget is Vessel)
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
            rotationReference = Quaternion.LookRotation(forward, up);
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
            // @todo: Is it possible to select ModuleEngines OR ModuleEnginesFX in a single iterator?
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
        public static Vector3d prev_err;
        public static Vector3d integral;
        private static Vector3d[] averagedAct = new Vector3d[5];

        // TODO: for some reason this function no longer gets called. Reinstate or delete?
        public static void KillRotation(FlightCtrlState c, Vessel vessel)
        {
            var act = vessel.transform.InverseTransformDirection(vessel.rigidbody.angularVelocity).normalized;

            c.pitch = act.x;
            c.roll = act.y;
            c.yaw = act.z;

            c.killRot = true;
        }

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
            var CoM = vessel.findWorldCenterOfMass();
            var MoI = getTrueMoI(vessel);

            //---------------------------------------
            // Copied almost verbatim from MechJeb master on June 27, 2014 -- Starstrider42

            Quaternion delta = Quaternion.Inverse(
                Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * target);

            Vector3d deltaEuler = new Vector3d(
                  (delta.eulerAngles.x > 180) ? (delta.eulerAngles.x - 360.0F) : delta.eulerAngles.x,
                -((delta.eulerAngles.y > 180) ? (delta.eulerAngles.y - 360.0F) : delta.eulerAngles.y),
                  (delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z
            );

            Vector3d torque = GetTorque(vessel, c.mainThrottle);
            Vector3d inertia = GetEffectiveInertia(vessel, torque);

            // ( MoI / avaiable torque ) factor:
            Vector3d NormFactor = SwapYZ( Vector3d.Scale( MoI, Inverse(torque) ) );

            // Find out the real shorter way to turn were we want to.
            // Thanks to HoneyFox

            Vector3d tgtLocalUp = vessel.ReferenceTransform.transform.rotation.Inverse() * target * Vector3d.forward;
            Vector3d curLocalUp = Vector3d.up;

            double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
            Vector2d rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
            rotDirection = rotDirection.normalized * turnAngle / 180.0f;

            Vector3d err = new Vector3d(
                -rotDirection.y * Math.PI,
                rotDirection.x * Math.PI,
                fixedRoll 
                    ? ((delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z) * Math.PI / 180.0F 
                    : 0F
            );

            err += SwapYZ(inertia) / 2;
            err = new Vector3d(Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));
            err.Scale(NormFactor);

            // angular velocity:
            Vector3d omega;
            omega.x = vessel.angularVelocity.x;
            omega.y = vessel.angularVelocity.z; // y <=> z
            omega.z = vessel.angularVelocity.y; // z <=> y
            omega.Scale(NormFactor);

            Vector3d pidAction = fc.pid.Compute(err, omega);

            // low pass filter, wf = 1/Tf:
            Vector3d act = fc.lastAct + (pidAction - fc.lastAct) * (1 / ((fc.Tf / TimeWarp.fixedDeltaTime) + 1));
            fc.lastAct = act;

            // end MechJeb import
            //---------------------------------------

            float precision = Mathf.Clamp((float)(torque.x * 20f / MoI.magnitude), 0.5f, 10f);
            float drive_limit = Mathf.Clamp01((float)(err.magnitude * 380.0f / precision));

            act.x = Mathf.Clamp((float)act.x, -drive_limit, drive_limit);
            act.y = Mathf.Clamp((float)act.y, -drive_limit, drive_limit);
            act.z = Mathf.Clamp((float)act.z, -drive_limit, drive_limit);

            c.roll = Mathf.Clamp((float)(c.roll + act.z), -drive_limit, drive_limit);
            c.pitch = Mathf.Clamp((float)(c.pitch + act.x), -drive_limit, drive_limit);
            c.yaw = Mathf.Clamp((float)(c.yaw + act.y), -drive_limit, drive_limit);
        }

        public static Vector3d SwapYZ(Vector3d input)
        {
            return new Vector3d(input.x, input.z, input.y);
        }

        public static Vector3d Pow(Vector3d v3d, float exponent)
        {
            return new Vector3d(Math.Pow(v3d.x, exponent), Math.Pow(v3d.y, exponent), Math.Pow(v3d.z, exponent));
        }

        // Copied from MechJeb master on June 27, 2014
        private class Matrix3x3
        {
            //row index, then column index
            private double[,] e = new double[3, 3];

            public double this[int i, int j]
            {
                get { return e[i, j]; }
                set { e[i, j] = value; }
            }

            public static Vector3d operator *(Matrix3x3 M, Vector3d v)
            {
                Vector3d ret = Vector3d.zero;
                for(int i = 0; i < 3; i++) {
                    for(int j = 0; j < 3; j++) {
                        ret[i] += M.e[i, j] * v[j];
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
        private static Vector3d getTrueMoI(Vessel vessel) {
            var inertiaTensor = new Matrix3x3();
            var CoM           = vessel.findWorldCenterOfMass();

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
                Vector3 partPosition = vessel.GetTransform().InverseTransformDirection(p.Rigidbody.worldCenterOfMass - CoM);

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

        public static Vector3d GetEffectiveInertia(Vessel vessel, Vector3d torque)
        {
            var CoM = vessel.findWorldCenterOfMass();
            var MoI = getTrueMoI(vessel);
            var angularVelocity = Quaternion.Inverse(vessel.transform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d(angularVelocity.x * MoI.x, angularVelocity.y * MoI.y, angularVelocity.z * MoI.z);

            // Adapted from MechJeb master on June 27, 2014
            var retVar = Vector3d.Scale(Sign(angularMomentum), 
                Vector3d.Scale (
                    Vector3d.Scale (angularMomentum, angularMomentum),
                    Inverse(Vector3d.Scale(torque, MoI))
                ));

            return retVar;
        }

        /// <summary>
        /// Returns the torque the ship can exert around its center of mass
        /// </summary>
        /// <returns>The torque in N m, around the <pitch, roll, yaw> axes.</returns>
        /// <param name="vessel">The ship whose torque should be measured.</param>
        /// <param name="thrust">The ship's throttle setting, on a scale of 0 to 1.</param>
        public static Vector3d GetTorque(Vessel vessel, float thrust)
        {
            var CoM = vessel.findWorldCenterOfMass();

            float pitchYaw = 0;
            float roll = 0;

            foreach (Part part in vessel.parts)
            {
                var relCoM = part.Rigidbody.worldCenterOfMass - CoM;

                if (part is CommandPod)
                {
                    pitchYaw += Math.Abs(((CommandPod)part).rotPower);
                    roll += Math.Abs(((CommandPod)part).rotPower);
                }

                if (part is RCSModule)
                {
                    float max = 0;
                    foreach (float power in ((RCSModule)part).thrusterPowers)
                    {
                        max = Mathf.Max(max, power);
                    }

                    pitchYaw += max * relCoM.magnitude;
                }

                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleReactionWheel)
                    {
                        pitchYaw += ((ModuleReactionWheel)module).PitchTorque;
                        roll += ((ModuleReactionWheel)module).RollTorque;
                    }
                }

                pitchYaw += (float)GetThrustTorque(part, vessel) * thrust;
            }

            return new Vector3d(pitchYaw, roll, pitchYaw);
        }

        public static double GetThrustTorque(Part p, Vessel vessel)
        {
            var CoM = vessel.CoM;

            if (p.State == PartStates.ACTIVE)
            {
                if (p is LiquidEngine)
                {
                    if (((LiquidEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((LiquidEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }
                else if (p is LiquidFuelEngine)
                {
                    if (((LiquidFuelEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((LiquidFuelEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidFuelEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }
                else if (p is AtmosphericEngine)
                {
                    if (((AtmosphericEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((AtmosphericEngine)p).gimbalRange) * Math.PI / 180) * ((AtmosphericEngine)p).maximumEnginePower * ((AtmosphericEngine)p).totalEfficiency * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                    }
                }
            }

            return 0;
        }

        private static Vector3d ReduceAngles(Vector3d input)
        {
            return new Vector3d(
                      (input.x > 180f) ? (input.x - 360f) : input.x,
                      (input.y > 180f) ? (input.y - 360f) : input.y,
                      (input.z > 180f) ? (input.z - 360f) : input.z
                  );
        }

        public static Vector3d Inverse(Vector3d input)
        {
            return new Vector3d(1 / input.x, 1 / input.y, 1 / input.z);
        }

        public static Vector3d Sign(Vector3d vector)
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }

        private static Vector3d averageVector3d(Vector3d[] vectorArray, Vector3d newVector, int n)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            int k = 0;

            // Loop through the array to determine average
            // Give more weight to newer items and less weight to older items
            for (int i = 0; i < n; i++)
            {
                k += i + 1;
                if (i < n - 1) { vectorArray[i] = vectorArray[i + 1]; }
                else { vectorArray[i] = newVector; }
                x += vectorArray[i].x * (i + 1);
                y += vectorArray[i].y * (i + 1);
                z += vectorArray[i].z * (i + 1);
            }
            return new Vector3d(x / k, y / k, z / k);
        }
    }
}