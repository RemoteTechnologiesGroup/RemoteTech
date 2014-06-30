using System;
using KSP.IO;
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

        public static String FormatDuration(double duration, string numFormat)
        {
            var time = TimeSpan.FromSeconds(duration);
            var s = new StringBuilder();
            if (time.TotalDays / DaysInAYear >= 1)
            {
                s.Append(Math.Floor(time.TotalDays / DaysInAYear).ToString(numFormat));
                s.Append("y");
            }
            if (time.TotalDays % DaysInAYear >= 1)
            {
                s.Append(Math.Floor(time.TotalDays % DaysInAYear).ToString(numFormat));
                s.Append("d");
            }
            if (time.Hours > 0)
            {
                s.Append(time.Hours.ToString(numFormat));
                s.Append("h");
            }
            if (time.Minutes > 0)
            {
                s.Append(time.Minutes.ToString(numFormat));
                s.Append("m");
            }
            s.Append((time.Seconds + time.Milliseconds / 1000.0f).ToString(numFormat));
            s.Append("s");
            return s.ToString();
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

        public static String TargetName(Guid guid)
        {
            ISatellite sat;
            if (RTCore.Instance != null && RTCore.Instance.Network != null && RTCore.Instance.Satellites != null)
            {
                if (guid == System.Guid.Empty)
                {
                    return "No Target";
                }
                if (RTCore.Instance.Network.Planets.ContainsKey(guid))
                {
                    return RTCore.Instance.Network.Planets[guid].name;
                }
                if (guid == NetworkManager.ActiveVesselGuid)
                {
                    return "Active Vessel";
                }
                if ((sat = RTCore.Instance.Network[guid]) != null)
                {
                    return sat.Name;
                }
            }
            return "Unknown Target";
        }

        public static Guid Guid(this CelestialBody cb)
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

        public static void Button(Texture2D icon, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(icon, options))
            {
                onClick.Invoke();
            }
        }

        public static void Button(String text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                onClick.Invoke();
            }
        }

        public static void Button(GUIContent text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                onClick.Invoke();
            }
        }

        public static void HorizontalSlider(ref float state, float min, float max, params GUILayoutOption[] options)
        {
            state = GUILayout.HorizontalSlider(state, min, max, options);
        }

        public static void GroupButton(int wide, String[] text, ref int group, params GUILayoutOption[] options)
        {
            group = GUILayout.SelectionGrid(group, text, wide, options);
        }

        public static void GroupButton(int wide, String[] text, ref int group, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            int group2;
            if ((group2 = GUILayout.SelectionGrid(group, text, wide, options)) != group)
            {
                group = group2;
                onStateChange.Invoke(group2);
            }
        }

        public static void StateButton(GUIContent text, int state, int value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Object.Equals(state, value), text, GUI.skin.button, options)) != Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? value : ~value);
            }
        }

        public static void StateButton<T>(GUIContent text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Object.Equals(state, value), text, GUI.skin.button, options)) != Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }

        public static void StateButton<T>(String text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Object.Equals(state, value), text, GUI.skin.button, options)) != Object.Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }

        public static void TextField(ref String text, params GUILayoutOption[] options)
        {
            text = GUILayout.TextField(text, options);
        }

        public static bool ContainsMouse(this Rect window)
        {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }

        public static void LoadImage(out Texture2D texture, String fileName)
        {
            fileName = fileName.Split('.')[0];
            String path = "RemoteTech2/Textures/" + fileName;
            RTLog.Notify("LoadImage({0})", path);
            texture = GameDatabase.Instance.GetTexture(path, false);
            if (texture == null)
            {
                texture = new Texture2D(32, 32);
                texture.SetPixels32(Enumerable.Repeat((Color32)Color.magenta, 32 * 32).ToArray());
                texture.Apply();
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

        public static T CachePerFrame<T>(ref CachedField<T> cachedField, Func<T> getter)
        {
            if (cachedField.Frame != Time.frameCount)
            {
                cachedField.Frame = Time.frameCount;
                return cachedField.Field = getter();
            }
            else
            {
                return cachedField.Field;
            }
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

        public static string ConstrictNum(string s)
        {
            return ConstrictNum(s, true);
        }

        public static String ConstrictNum(string s, float max)
        {

            string tmp = ConstrictNum(s, false);

            float f = 0;

            Single.TryParse(tmp, out f);

            if (f > max)
                return max.ToString("00");
            else
                return tmp;
        }

        public static String ConstrictNum(string s, bool allowNegative)
        {
            StringBuilder tmp = new StringBuilder();
            if (allowNegative && s.StartsWith("-"))
                tmp.Append(s[0]);
            bool point = false;

            foreach (char c in s)
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

        public static bool CBhit(CelestialBody body, out Vector2 latlon)
        {

            Vector3d hitA;
            Vector3 origin, dir;

            if (MapView.MapIsEnabled)
            {
                //Use Scaled camera and don't attempt physics raycast if in map view.
                Ray ray = ScaledCamera.Instance.camera.ScreenPointToRay(Input.mousePosition);
                origin = ScaledSpace.ScaledToLocalSpace(ray.origin);
                dir = ray.direction.normalized;
            }
            else
            {
                //Attempt ray cast and return results if successfull.
                Ray ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitB;
                float dist = (float)(Vector3.Distance(body.position, ray.origin) - body.Radius / 2);
                if (Physics.Raycast(ray, out hitB, dist))
                {
                    latlon = new Vector2((float)body.GetLatitude(hitB.point), (float)body.GetLongitude(hitB.point));
                    return true;
                }
                //if all else fails, try with good oldfashioned arithmetic.
                origin = ray.origin;
                dir = ray.direction.normalized;
            }

            if (CBhit(body, origin, dir, out hitA))
            {
                latlon = new Vector2((float)body.GetLatitude(hitA), (float)body.GetLongitude(hitA));
                return true;
            }
            else
            {
                latlon = Vector2.zero;
                return false;
            }
        }

        public static bool CBhit(CelestialBody body, Vector3d originalOrigin, Vector3d direction, out Vector3d hit)
        {
            double r = body.Radius;
            //convert the origin point from world space to body local space and assume body center as (0,0,0).
            Vector3d origin = originalOrigin - body.position;

            //Compute A, B and C coefficients
            double a = Vector3d.Dot(direction, direction);
            double b = 2 * Vector3d.Dot(direction, origin);
            double c = Vector3d.Dot(origin, origin) - (r * r);

            //Find discriminant
            double disc = b * b - 4 * a * c;

            // if discriminant is negative there are no real roots, so return 
            // false as ray misses sphere
            if (disc < 0)
            {
                hit = Vector3d.zero;
                return false;
            }

            // compute q.
            double distSqrt = Math.Sqrt(disc);
            double q;
            if (b < 0)
                q = (-b - distSqrt) / 2.0;
            else
                q = (-b + distSqrt) / 2.0;

            // compute t0 and t1
            double t0 = q / a;
            double t1 = c / q;

            // make sure t0 is smaller than t1
            if (t0 > t1)
            {
                // if t0 is bigger than t1 swap them around
                double temp = t0;
                t0 = t1;
                t1 = temp;
            }

            // if t1 is less than zero, the body is in the ray's negative direction
            // and consequently the ray misses the sphere
            if (t1 < 0)
            {
                hit = Vector3d.zero;
                return false;
            }

            // if t0 is less than zero, the intersection point is at t1
            if (t0 < 0)
            {
                hit = originalOrigin + (t1 * direction);
                return true;
            }
            // else the intersection point is at t0
            else
            {
                hit = originalOrigin + (t0 * direction);
                return true;
            }
        }

        public static float GetHDG(Vector3 dir, Vector3 up, Vector3 north)
        {
            return Quaternion.Inverse(Quaternion.Inverse(Quaternion.LookRotation(dir, up)) * Quaternion.LookRotation(north, up)).eulerAngles.y;
        }

        public static double ClampDegrees360(double angle)
        {
            angle = angle % 360.0;
            if (angle < 0)
                return angle + 360.0;
            else
                return angle;
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
            if (angle < 0)
                return angle + 360f;
            else
                return angle;
        }

        public static float ClampDegrees180(float angle)
        {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float AngleBetween(float AngleFrom, float AngleTo)
        {
            float angle = AngleFrom - AngleTo;
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

}