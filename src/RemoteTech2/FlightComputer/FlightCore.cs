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
                    if (f.DelayedTarget is Vessel)
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
                    if (f.DelayedTarget is Vessel)
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
            kOS.SteeringHelper.SteerShipToward(target, fs, f.Vessel);
        }

        public static double GetTotalThrust(Vessel v)
        {
            var thrust = v.parts.SelectMany(p => p.FindModulesImplementing<ModuleEngines>())
                        .Where(pm => pm.EngineIgnited)
                        .Sum(pm => (double)pm.maxThrust * (pm.thrustPercentage / 100));
            thrust += v.parts.SelectMany(p => p.FindModulesImplementing<ModuleEnginesFX>())
                        .Where(pm => pm.EngineIgnited)
                        .Sum(pm => (double)pm.maxThrust * (pm.thrustPercentage / 100));
            return thrust;
        }
    }
}

namespace kOS
{
    public static class SteeringHelper
    {
        public static void KillRotation(FlightCtrlState c, Vessel vessel)
        {
            var act = vessel.transform.InverseTransformDirection(vessel.rigidbody.angularVelocity).normalized;

            c.pitch = act.x;
            c.roll = act.y;
            c.yaw = act.z;

            c.killRot = true;
        }

        public static void SteerShipToward(Quaternion target, FlightCtrlState c, Vessel vessel)
        {
            // I take no credit for this, this is a stripped down, rearranged version of MechJeb's attitude control system
            var centerOfMass = vessel.findWorldCenterOfMass();
            var momentOfInertia = vessel.findLocalMOI(centerOfMass);

            var vesselR = vessel.transform.rotation;

            Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselR) * target);

            Vector3d deltaEuler = ReduceAngles(delta.eulerAngles);
            deltaEuler.y *= -1;

            Vector3d torque = GetTorque(vessel, c.mainThrottle);
            Vector3d inertia = GetEffectiveInertia(vessel, torque);

            Vector3d err = deltaEuler * Math.PI / 180.0F;
            err += new Vector3d(inertia.x, inertia.z, inertia.y);

            Vector3d act = 120.0f * err;

            float precision = Mathf.Clamp((float)torque.x * 20f / momentOfInertia.magnitude, 0.5f, 10f);
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

        public static Vector3d Pow(Vector3d v3d, float exponent)
        {
            return new Vector3d(Math.Pow(v3d.x, exponent), Math.Pow(v3d.y, exponent), Math.Pow(v3d.z, exponent));
        }

        public static Vector3d GetEffectiveInertia(Vessel vessel, Vector3d torque)
        {
            var centerOfMass = vessel.findWorldCenterOfMass();
            var momentOfInertia = vessel.findLocalMOI(centerOfMass);
            var angularVelocity = Quaternion.Inverse(vessel.transform.rotation) * vessel.rigidbody.angularVelocity;
            var angularMomentum = new Vector3d(angularVelocity.x * momentOfInertia.x, angularVelocity.y * momentOfInertia.y, angularVelocity.z * momentOfInertia.z);

            var retVar = Vector3d.Scale
            (
                Sign(angularMomentum) * 2.0f,
                Vector3d.Scale(Pow(angularMomentum, 2), Inverse(Vector3d.Scale(torque, momentOfInertia)))
            );

            retVar.y *= 10;

            return retVar;
        }

        public static Vector3d GetTorque(Vessel vessel, float thrust)
        {
            // Do everything in vessel coordinates
            var centerOfMass = vessel.findLocalCenterOfMass();

            // Don't assume any particular symmetry for the vessel
            float pitch = 0, roll = 0, yaw = 0;

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

                float gimbal = (float)GetThrustTorque(part, vessel) * thrust;
                pitch += gimbal;
                yaw += gimbal;
            }

            return new Vector3d(pitch, roll, yaw);
        }

        public static double GetThrustTorque(Part p, Vessel vessel)
        {
            var centerOfMass = vessel.CoM;

            if (p.State == PartStates.ACTIVE)
            {
                if (p is LiquidEngine)
                {
                    if (((LiquidEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((LiquidEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - centerOfMass).magnitude;
                    }
                }
                else if (p is LiquidFuelEngine)
                {
                    if (((LiquidFuelEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((LiquidFuelEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidFuelEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - centerOfMass).magnitude;
                    }
                }
                else if (p is AtmosphericEngine)
                {
                    if (((AtmosphericEngine)p).thrustVectoringCapable)
                    {
                        return Math.Sin(Math.Abs(((AtmosphericEngine)p).gimbalRange) * Math.PI / 180) * ((AtmosphericEngine)p).maximumEnginePower * ((AtmosphericEngine)p).totalEfficiency * (p.Rigidbody.worldCenterOfMass - centerOfMass).magnitude;
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
    }
}