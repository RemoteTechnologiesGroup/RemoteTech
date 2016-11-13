using System;
using System.Text;
using UnityEngine;

namespace RemoteTech.Common.Utils
{
    public static class FormatUtil
    {
        public static readonly string[]
            DistanceUnits = { "", "k", "M", "G", "T" },
            ClassDescripts = {  "Short-Planetary (SP)",
                                "Medium-Planetary (MP)",
                                "Long-Planetary (LP)",
                                "Short-Interplanetary (SI)",
                                "Medium-Interplanetary (MI)",
                                "Long-Interplanetary (LI)"};

        public static string ConstrictNum(string s)
        {
            return ConstrictNum(s, true);
        }

        public static string ConstrictNum(string s, float max)
        {

            var tmp = ConstrictNum(s, false);

            float f;

            float.TryParse(tmp, out f);

            return f > max ? max.ToString("00") : tmp;
        }

        public static string ConstrictNum(string s, bool allowNegative)
        {
            var tmp = new StringBuilder();
            if (allowNegative && s.StartsWith("-"))
                tmp.Append(s[0]);
            var point = false;

            foreach (var c in s)
            {
                if (char.IsNumber(c))
                    tmp.Append(c);
                else if (!point && (c == '.' || c == ','))
                {
                    point = true;
                    tmp.Append('.');
                }
            }
            return tmp.ToString();
        }

        public static Vector3 Format360To180(Vector3 v)
        {
            return new Vector3(Format360To180(v.x), Format360To180(v.y), Format360To180(v.z));
        }

        public static float Format360To180(float degrees)
        {
            if (degrees > 180)
            {
                return degrees - 360;
            }
            return degrees;
        }

        public static float Format180To360(float degrees)
        {
            if (degrees < 0)
            {
                return degrees + 360;
            }
            return degrees;
        }

        public static string FormatConsumption(double consumption)
        {
            const string timeindicator = "sec";

            return $"{consumption:F2}/{timeindicator}.";
        }

        public static string FormatSI(double value, string unit)
        {
            var i = (int)Clamp(Math.Floor(Math.Log10(value)) / 3,
                0, DistanceUnits.Length - 1);
            value /= Math.Pow(1000, i);
            return value.ToString("F2") + DistanceUnits[i] + unit;
        }
    }
}
