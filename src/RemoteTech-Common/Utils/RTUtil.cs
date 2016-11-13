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
}
