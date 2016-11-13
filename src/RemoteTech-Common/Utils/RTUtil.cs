using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTech.Common.Utils
{



    public static class ClampUtil
    {
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
        }

        public static double ClampDegrees360(double angle)
        {
            angle = angle % 360.0;
            return angle < 0 ? angle + 360.0 : angle;
        }

        public static double ClampDegrees180(double angle)
        {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float ClampDegrees360(float angle)
        {
            angle = angle % 360f;
            return angle < 0 ? angle + 360f : angle;
        }

        public static float ClampDegrees180(float angle)
        {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float AngleBetween(float angleFrom, float angleTo)
        {
            var angle = angleFrom - angleTo;
            while (angle < -180) angle += 360;
            while (angle > 180) angle -= 360;
            return angle;
        }

        public static float ClampDegrees90(float angle)
        {
            if (angle > 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;
            return angle;
        }
    }

    public static partial class RTUtil
    {
        public static string Truncate(this string targ, int len)
        {
            const string suffix = "...";
            if (targ.Length > len)
            {
                return targ.Substring(0, len - suffix.Length) + suffix;
            }
            return targ;
        }





        public static Guid Guid(this CelestialBody cb)
        {
            var name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (var i = 0; i < 16; i++)
            {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            return new Guid(s.ToString());
        }



        //Note: Keep this method even if it has no reference, it is useful to track some bugs.
        /// <summary>
        /// Get a private field value from an object instance though reflection.
        /// </summary>
        /// <param name="type">The type of the object instance from which to obtain the private field.</param>
        /// <param name="instance">The object instance</param>
        /// <param name="fieldName">The field name in the object instance, from which to obtain the value.</param>
        /// <returns>The value of the <paramref name="fieldName"/> instance or null if no such field exist in the instance.</returns>
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                           | BindingFlags.Static;
            var field = type.GetField(fieldName, bindFlags);
            return field?.GetValue(instance);
        }

 
        
 


        public static bool CBhit(CelestialBody body, out Vector2 latlon) {

            Vector3d hitA;
            Vector3 origin, dir;

            if (MapView.MapIsEnabled) {
                //Use Scaled camera and don't attempt physics raycast if in map view.
                var ray = ScaledCamera.Instance.galaxyCamera.ScreenPointToRay(Input.mousePosition);
                origin = ScaledSpace.ScaledToLocalSpace(ray.origin);
                dir = ray.direction.normalized;
            } else {
                //Attempt ray cast and return results if successfull.
                var ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitB;
                var dist = (float)(Vector3.Distance(body.position, ray.origin) - body.Radius / 2);
                if (Physics.Raycast(ray, out hitB, dist)) {
                    latlon = new Vector2((float)body.GetLatitude(hitB.point), (float)body.GetLongitude(hitB.point));
                    return true;
                }
                //if all else fails, try with good oldfashioned arithmetic.
                origin = ray.origin;
                dir = ray.direction.normalized;
            }

            if (CBhit(body, origin, dir, out hitA)) {
                latlon = new Vector2((float)body.GetLatitude(hitA), (float)body.GetLongitude(hitA));
                return true;
            }
            latlon = Vector2.zero;
            return false;
        }

        public static bool CBhit(CelestialBody body, Vector3d originalOrigin, Vector3d direction, out Vector3d hit) {
            var r = body.Radius;
            //convert the origin point from world space to body local space and assume body center as (0,0,0).
            var origin = originalOrigin - body.position;

            //Compute A, B and C coefficients
            var a = Vector3d.Dot(direction, direction);
            var b = 2 * Vector3d.Dot(direction, origin);
            var c = Vector3d.Dot(origin, origin) - (r * r);

            //Find discriminant
            var disc = b * b - 4 * a * c;

            // if discriminant is negative there are no real roots, so return 
            // false as ray misses sphere
            if (disc < 0) {
                hit = Vector3d.zero;
                return false;
            }

            // compute q.
            var distSqrt = Math.Sqrt(disc);
            double q;
            if (b < 0)
                q = (-b - distSqrt) / 2.0;
            else
                q = (-b + distSqrt) / 2.0;

            // compute t0 and t1
            var t0 = q / a;
            var t1 = c / q;

            // make sure t0 is smaller than t1
            if (t0 > t1) {
                // if t0 is bigger than t1 swap them around
                var temp = t0;
                t0 = t1;
                t1 = temp;
            }

            // if t1 is less than zero, the body is in the ray's negative direction
            // and consequently the ray misses the sphere
            if (t1 < 0) {
                hit = Vector3d.zero;
                return false;
            }

            // if t0 is less than zero, the intersection point is at t1
            if (t0 < 0) {
                hit = originalOrigin + (t1 * direction);
                return true;
            }

            // the intersection point is at t0
            hit = originalOrigin + (t0 * direction);
            return true;
        }





        /// <summary>
        /// Returns a vessel object by the given <paramref name="vesselid"/> or
        /// null if no vessel was found
        /// </summary>
        /// <param name="vesselid">Guid of a vessel</param>
        public static Vessel GetVesselById(Guid vesselid)
        {
            return FlightGlobals.Vessels.FirstOrDefault(vessel => vessel.id == vesselid);
        }

        // -----------------------------------------------
        // Copied from MechJeb master on 18.04.2016
        public static Vector3d DeltaEuler(this Quaternion delta) 
        {
            return new Vector3d(
                (delta.eulerAngles.x > 180) ? (delta.eulerAngles.x - 360.0F) : delta.eulerAngles.x,
                -((delta.eulerAngles.y > 180) ? (delta.eulerAngles.y - 360.0F) : delta.eulerAngles.y),
                (delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z
                );
        }

        public static Vector3d Invert(this Vector3d vector) 
        {
            return new Vector3d(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }

        public static Vector3d Reorder(this Vector3d vector, int order) 
        {
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
            throw new ArgumentException("Invalid order", nameof(order));
        }

        public static Vector3d Sign(this Vector3d vector) 
        {
            return new Vector3d(Math.Sign(vector.x), Math.Sign(vector.y), Math.Sign(vector.z));
        }

        public static Vector3d Clamp(this Vector3d value, double min, double max) 
        {
            return new Vector3d(
                Clamp(value.x, min, max),
                Clamp(value.y, min, max),
                Clamp(value.z, min, max)
                );
        }

        // end MechJeb import
        //---------------------------------------
    }


    public static partial class RTUtil
    {
        public static bool GetBool(this ProtoPartModuleSnapshot ppms, string value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return bool.TryParse(n.GetValue(value) ?? "False", out result) && result;
        }

        /// <summary>Searches a ProtoPartModuleSnapshot for an integer field.</summary>
        /// <returns>True if the member <paramref name="valueName"/> exists, false otherwise.</returns>
        /// <param name="ppms">The <see cref="ProtoPartModuleSnapshot"/> to query.</param>
        /// <param name="valueName">The name of a member in the  ProtoPartModuleSnapshot.</param>
        /// <param name="value">The value of the member <paramref name="valueName"/> on success. An undefined value on failure.</param>
        public static bool GetInt(this ProtoPartModuleSnapshot ppms, string valueName, out int value)
        {
            value = 0;
            var result = ppms.moduleValues.TryGetValue(valueName, ref value);
            if (!result)
            {
                RTLog.Notify($"No integer '{value}' in ProtoPartModule '{ppms.moduleName}'");
            }

            return result;
        }

        public static bool HasValue(this ProtoPartModuleSnapshot ppms, string value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return bool.TryParse(value, out result) && result;
        }

        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTAntenna") &&
                   ppms.GetBool("IsRTPowered") &&
                   ppms.GetBool("IsRTActive");
        }

        public static bool IsSignalProcessor(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTSignalProcessor");

        }

        public static bool IsCommandStation(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTCommandStation");
        }

        public static bool IsAntenna(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTAntenna") &&
                   pm.Fields.GetValue<bool>("IsRTPowered") &&
                   pm.Fields.GetValue<bool>("IsRTActive");
        }

        public static bool IsCommandStation(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTCommandStation");
        }

        public static bool IsSignalProcessor(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTSignalProcessor");
        }


        public static ISignalProcessor GetSignalProcessor(this Vessel v)
        {
            RTLog.Notify("GetSignalProcessor({0}): Check", v.vesselName);

            ISignalProcessor result = null;

            if (v.loaded && v.parts.Count > 0)
            {
                var partModuleList = v.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Where(pm => pm.IsSignalProcessor()).ToList();
                // try to look for a moduleSPU
                result = partModuleList.FirstOrDefault(pm => pm.moduleName == "ModuleSPU") as ISignalProcessor ??
                         partModuleList.FirstOrDefault() as ISignalProcessor;
            }
            else
            {
                var protoPartList = v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Where(ppms => ppms.IsSignalProcessor()).ToList();
                // try to look for a moduleSPU on a unloaded vessel
                var protoPartProcessor = protoPartList.FirstOrDefault(ppms => ppms.moduleName == "ModuleSPU") ??
                                         protoPartList.FirstOrDefault();

                // convert the found protoPartSnapshots to a ProtoSignalProcessor
                if (protoPartProcessor != null)
                {
                    result = new ProtoSignalProcessor(protoPartProcessor, v);
                }
            }

            return result;
        }

        public static bool HasCommandStation(this Vessel v)
        {
            RTLog.Notify("HasCommandStation({0})", v.vesselName);
            if (v.loaded && v.parts.Count > 0)
            {
                return v.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Any(pm => pm.IsCommandStation());
            }
            return v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Any(pm => pm.IsCommandStation());
        }
    }
}
