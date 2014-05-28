using System;
using KSP.IO;
using System.Resources;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = System.Object;
using Debug = UnityEngine.Debug;

namespace RemoteTech
{
    public static partial class RTUtil
    {
        public static double GameTime { get { return Planetarium.GetUniversalTime(); } }

        public const int DaysInAYear = 365;

        public static readonly String[]
            DistanceUnits = { "", "k", "M", "G", "T" },
            ClassDescripts = {  "Short-Planetary (SP)",
                                "Medium-Planetary (MP)",
                                "Long-Planetary (LP)",
                                "Short-Interplanetary (SI)",
                                "Medium-Interplanetary (MI)",
                                "Long-Interplanetary (LI)"};

        private static readonly Regex mDurationRegex = new Regex(
            String.Format("{0}?{1}?{2}?{3}?{4}?", 
                @"(?:(?<seconds>\d*\.?\d+)\s*s[a-z]*[,\s]*)",
                @"(?:(?<minutes>\d*\.?\d+)\s*m[a-z]*[,\s]*)",
                @"(?:(?<hours>\d*\.?\d+)\s*h[a-z]*[,\s]*)",
                @"(?:(?<days>\d*\.?\d+)\s*d[a-z]*[,\s]*)",
                @"(?:(?<years>\d*\.?\d+)\s*y[a-z]*[,\s]*)"));

        public static bool TryParseDuration(String duration, out TimeSpan time)
        {
            time = new TimeSpan();
            MatchCollection matches = mDurationRegex.Matches(duration);
            foreach (Match match in matches)
            {
                if (match.Groups["seconds"].Success)
                {
                    time += TimeSpan.FromSeconds(Double.Parse(match.Groups["seconds"].Value));
                }
                if (match.Groups["minutes"].Success)
                {
                    time += TimeSpan.FromMinutes(Double.Parse(match.Groups["minutes"].Value));
                }
                if (match.Groups["hours"].Success)
                {
                    time += TimeSpan.FromHours(Double.Parse(match.Groups["hours"].Value));
                }
                if (match.Groups["days"].Success)
                {
                    time += TimeSpan.FromDays(Double.Parse(match.Groups["days"].Value));
                }
                if (match.Groups["years"].Success)
                {
                    time += TimeSpan.FromDays(Double.Parse(match.Groups["years"].Value) * DaysInAYear);
                }
            }
            if (time.TotalSeconds == 0)
            {
                double parsedDouble;
                bool result = Double.TryParse(duration, out parsedDouble);
                time = TimeSpan.FromSeconds(result ? parsedDouble : 0);
                return result;
            }
            return true;
        }

        public static void ScreenMessage(String msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4.0f, ScreenMessageStyle.UPPER_LEFT));
        }

        public static String Truncate(this String targ, int len)
        {
            const String suffix = "...";
            if (targ.Length > len)
            {
                return targ.Substring(0, len - suffix.Length) + suffix;
            }
            else
            {
                return targ;
            }
        }

        public static Vector3 Format360To180(Vector3 v)
        {
            return new Vector3(Format360To180(v.x), Format360To180(v.y), Format360To180(v.z));
        }

        public static float Format360To180(float degrees)
        {
            if (degrees > 360)
            {
                return degrees - 360;
            }
            else
            {
                return degrees;
            }
        }

        public static float Format180To360(float degrees)
        {
            if (degrees < 0)
            {
                return degrees + 360;
            }
            else
            {
                return degrees;
            }
        }

        public static String FormatDuration(double duration)
        {
            var time = TimeSpan.FromSeconds(duration);
            var s = new StringBuilder();
            if (time.TotalDays / DaysInAYear >= 1)
            {
                s.Append(Math.Floor(time.TotalDays / DaysInAYear));
                s.Append("y");
            }
            if (time.TotalDays % DaysInAYear >= 1)
            {
                s.Append(Math.Floor(time.TotalDays % DaysInAYear));
                s.Append("d");
            }
            if (time.Hours > 0)
            {
                s.Append(time.Hours);
                s.Append("h");
            }
            if (time.Minutes > 0)
            {
                s.Append(time.Minutes);
                s.Append("m");
            }
            s.Append((time.Seconds + time.Milliseconds / 1000.0f).ToString("F2"));
            s.Append("s");
            return s.ToString();
        }

        public static String FormatConsumption(double consumption)
        {
            return consumption.ToString("F2") + " charge/s";
        }

        public static String FormatSI(double value, String unit)
        {
            int i = (int)RTUtil.Clamp(Math.Floor(Math.Log10(value)) / 3,
                0, DistanceUnits.Length - 1);
            value /= Math.Pow(1000, i);
            return value.ToString("F2") + DistanceUnits[i] + unit;
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
        }

        public static Guid GenerateGuid(this CelestialBody cb)
        {
            char[] name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            return new Guid(s.ToString());
        }

        public static bool HasValue(this ProtoPartModuleSnapshot ppms, String value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return Boolean.TryParse(value, out result) ? result : false;
        }

        public static bool GetBool(this ProtoPartModuleSnapshot ppms, String value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return Boolean.TryParse(n.GetValue(value) ?? "False", out result) ? result : false;
        }

        public static Single? TryParseSingleNullable(String s)
        {
            Single tmp;
            return Single.TryParse(s, out tmp) ? (Single?) tmp : null;
        }

        public static Double? TryParseDoubleNullable(String s)
        {
            Double tmp;
            return Double.TryParse(s, out tmp) ? (Double?) tmp : null;
        }

        public static Int32? TryParseIntNullable(String s)
        {
            Int32 tmp;
            return Int32.TryParse(s, out tmp) ? (Int32?) tmp : null;
        }
        public static Boolean? TryParseBooleanNullable(String s)
        {
            Boolean tmp;
            return Boolean.TryParse(s, out tmp) ? (Boolean?) tmp : null;
        }
        public static Guid? TryParseGuidNullable(String s)
        {
            try
            {
                return new Guid(s);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static IEnumerable<Transform> FindTransformsWithCollider(Transform input)
        {
            if (input.collider != null)
            {
                yield return input;
            }

            foreach (Transform t in input)
            {
                foreach (Transform x in FindTransformsWithCollider(t))
                {
                    yield return x;
                }
            }
        }

        public static void ExecuteNextFrame(Action a)
        {
            RTCore.Instance.StartCoroutine(Coroutine_DelayFrame(a));
        }

        private static IEnumerator Coroutine_DelayFrame(Action a)
        {
            yield return null;
            a.Invoke();
        }

        public static IEnumerable<T> WrapAround<T>(this IEnumerable<T> input)
        {
            for (; ; )
            {
                foreach (var a in input) {
                    yield return a;
                }
            }
        }

        public static String TrimLines(this String text)
        {
            return text.TrimEnd(Environment.NewLine.ToCharArray());
        }

        // Thanks Fractal_UK!
        public static bool IsTechUnlocked(string techid)
        {
            if (techid.Equals("None")) return true;
            try
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) return true;
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode[] scenarios = gameconf.GetNodes("SCENARIO");
                foreach (ConfigNode scenario in scenarios)
                {
                    if (scenario.GetValue("name") == "ResearchAndDevelopment")
                    {
                        ConfigNode[] techs = scenario.GetNodes("Tech");
                        foreach (ConfigNode technode in techs)
                        {
                            if (technode.GetValue("id") == techid)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}