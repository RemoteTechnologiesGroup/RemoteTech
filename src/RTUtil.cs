using System;
using KSP.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = System.Object;

namespace RemoteTech {
    public static partial class RTUtil {
        public static readonly String[]
            DistanceUnits = { "", "k", "M", "G", "T" },
            ClassDescripts = {  "Short-Planetary (SP)",
                                "Medium-Planetary (MP)",
                                "Long-Planetary (LP)",
                                "Short-Interplanetary (SI)",
                                "Medium-Interplanetary (MI)",
                                "Long-Interplanetary (LI)"};

        private static readonly Regex mDurationRegex =
            new Regex(@"(?:(?<seconds>\d*\.?\d+)\s*s[a-z]*[,\s]*)?" +
                      @"(?:(?<minutes>\d*\.?\d+)\s*m[a-z]*[,\s]*)?" +
                      @"(?:(?<hours>\d*\.?\d+)\s*h[a-z]*[,\s]*)?");

        public static bool TryParseDuration(String duration, out TimeSpan time) {
            time = new TimeSpan();
            MatchCollection matches = mDurationRegex.Matches(duration);
            if (matches.Count > 0) {
                foreach (Match match in matches) {
                    if (match.Groups["seconds"].Success) {
                        time += TimeSpan.FromSeconds(Double.Parse(match.Groups["seconds"].Value));
                    }
                    if (match.Groups["minutes"].Success) {
                        time += TimeSpan.FromMinutes(Double.Parse(match.Groups["minutes"].Value));
                    }
                    if (match.Groups["hours"].Success) {
                        time += TimeSpan.FromHours(Double.Parse(match.Groups["hours"].Value));
                    }
                }
                return true;
            } else {
                double parsedDouble;
                bool result = Double.TryParse(duration, out parsedDouble);
                time = TimeSpan.FromSeconds(result ? parsedDouble : 0);
                return result;
            }
        }

        public static String FormatDuration(double duration) {
            TimeSpan time = TimeSpan.FromSeconds(duration);
            StringBuilder s = new StringBuilder();
            if (time.TotalHours > 1) {
                s.Append(Math.Floor(time.TotalHours));
                s.Append("h");
            }
            if (time.Minutes > 1) {
                s.Append(time.Minutes);
                s.Append("m");
            }
            if (time.Seconds > 0 || time.Milliseconds > 0) {
                s.Append((time.Seconds + time.Milliseconds / 1000.0f).ToString("F2"));
                s.Append("s");
            }
            return s.ToString();
        }

        public static String FormatConsumption(double consumption) {
            return consumption.ToString("F2") + "charge/s";
        }

        public static String FormatSI(double value, String unit) {
            int i = (int)RTUtil.Clamp(Math.Floor(Math.Log10(value)) / 3,
                0, DistanceUnits.Length - 1);
            value /= Math.Pow(1000, i);
            return value.ToString("F2") + DistanceUnits[i] + unit;
        }

        public static string ConstrictNum(string s) {
            return ConstrictNum(s, true);
        }

        public static String ConstrictNum(string s, float max) {

            string tmp = ConstrictNum(s, false);

            float f = 0;

            Single.TryParse(tmp, out f);

            if (f > max)
                return max.ToString("00");
            else
                return tmp;
        }

        public static String ConstrictNum(string s, bool allowNegative) {
            StringBuilder tmp = new StringBuilder();
            if (allowNegative && s.StartsWith("-"))
                tmp.Append(s[0]);
            bool point = false;

            foreach (char c in s) {
                if (char.IsNumber(c))
                    tmp.Append(c);
                else if (!point && (c == '.' || c == ',')) {
                    point = true;
                    tmp.Append('.');
                }
            }
            return tmp.ToString();
        }

        public static String FormatClass(float range) {
            List<float> classes = new List<float>();

            classes.Add(Math.Abs(500000 - range));
            classes.Add(Math.Abs(7500000 - range));
            classes.Add(Math.Abs(50000000 - range));
            classes.Add(Math.Abs(50000000000 - range));
            classes.Add(Math.Abs(200000000000 - range));
            classes.Add(Math.Abs(900000000000 - range));

            return ClassDescripts[classes.IndexOf(classes.Min())];
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T> {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
        }

        public static void Log(String message) {
            Debug.Log("RemoteTech: " + message);
        }

        public static void Log(String message, params Object[] param) {
            Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        public static void Log(String message, params UnityEngine.Object[] param) {
            Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) {
            return "{" +
                   string.Join(",",
                               dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString())
                                         .ToArray()) + "}";
        }

        public static string ToDebugString<T>(this List<T> list) {
            return "{" + string.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }

        public static double GetGameTime() {
            return (Planetarium.GetUniversalTime());
        }

        public static String TargetName(Guid guid) {
            ISatellite sat;
            if (RTCore.Instance != null &&
                    RTCore.Instance.Network != null &&
                    RTCore.Instance.Satellites != null) {
                if (guid == System.Guid.Empty) {
                    return "No Target";
                }
                if (RTCore.Instance.Network.Planets.ContainsKey(guid)) {
                    return RTCore.Instance.Network.Planets[guid].name;
                }
                if ((sat = RTCore.Instance.Satellites[guid]) != null) {
                    return sat.Name;
                }
                if (guid.Equals((sat = RTCore.Instance.Network.MissionControl).Guid)) {
                    return sat.Name;
                }
            }
            return "[Unknown Target]";
        }

        public static Guid Guid(this CelestialBody cb) {
            char[] name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (int i = 0; i < 16; i++) {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            Log("cb.Guid: " + s);
            return new Guid(s.ToString());
        }

        public static bool HasValue(this ProtoPartModuleSnapshot ppms, String value) {
            var n = new ConfigNode();
            ppms.Save(n);
            try {
                if (Boolean.Parse(n.GetValue(value))) {
                    return true;
                }
            } catch (ArgumentException) {
                /* nothing */
            }
            return false;
        }

        public static bool GetBool(this ProtoPartModuleSnapshot ppms, String value) {
            var n = new ConfigNode();
            ppms.Save(n);
            try {
                return Boolean.Parse(n.GetValue(value) ?? "False");
            } catch (ArgumentException) {
                return false;
            }
        }

        public static void Button(Texture2D icon, OnClick onClick, params GUILayoutOption[] options) {
            if (GUILayout.Button(icon, options)) {
                onClick.Invoke();
            }
        }

        public static void Button(String text, OnClick onClick, params GUILayoutOption[] options) {
            if (GUILayout.Button(text, options)) {
                onClick.Invoke();
            }
        }

        public static void HorizontalSlider(ref float state, float min, float max,
                                    params GUILayoutOption[] options) {
            state = GUILayout.HorizontalSlider(state, min, max, options);
        }

        public static void GroupButton(int wide, String[] text, ref int group,
                                       params GUILayoutOption[] options) {
            group = GUILayout.SelectionGrid(group, text, wide, options);
        }

        public static void GroupButton(int wide, String[] text, ref int group, OnState onStateChange,
                                       params GUILayoutOption[] options) {
            int group2;
            if ((group2 = GUILayout.SelectionGrid(group, text, wide, options)) != group) {
                group = group2;
                onStateChange.Invoke(group2);
            }
        }

        public static void StateButton(String text, int state, int value, OnState onStateChange,
                                       params GUILayoutOption[] options) {
            bool result;
            if ((result = GUILayout.Toggle(state == value, text, GUI.skin.button, options))
                                                                            != (state == value)) {
                onStateChange.Invoke(result ? value : ~value);
            }
        }

        public static void TextField(ref String text, params GUILayoutOption[] options) {
            text = GUILayout.TextField(text, options);
        }

        public static bool ContainsMouse(this Rect window) {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }

        public static void LoadImage(out Texture2D texture, String fileName) {
            fileName = fileName.Split('.')[0];
            texture = GameDatabase.Instance.GetTexture("RemoteTech2/Textures/" + fileName, false);
            if (texture == null) {
                texture = new Texture2D(32, 32);
            }
        }

        public static double ClampDegrees360(double angle) {
            angle = angle % 360.0;
            if (angle < 0)
                return angle + 360.0;
            else
                return angle;
        }

        public static double ClampDegrees180(double angle) {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float ClampDegrees360(float angle) {
            angle = angle % 360f;
            if (angle < 0)
                return angle + 360f;
            else
                return angle;
        }

        public static float ClampDegrees180(float angle) {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static IEnumerable<Transform> FindTransformsWithCollider(Transform input) {
            if (input.collider != null) {
                yield return input;
            }

            foreach (Transform t in input) {
                foreach (Transform x in FindTransformsWithCollider(t)) {
                    yield return x;
                }
            }
        }

        public static void findTransformsWithPrefix(Transform input, ref List<Transform> list, string prefix) {
            if (input.name.ToLower().StartsWith(prefix.ToLower()))
                list.Add(input);
            foreach (Transform t in input)
                findTransformsWithPrefix(t, ref list, prefix);
        }

        public static bool CBhit(CelestialBody body, out Vector2 latlon) {

            Vector3d hitA;
            Vector3 origin, dir;
                        
            if (MapView.MapIsEnabled) {
                //Use Scaled camera and don't attempt physics raycast if in map view.
                Ray ray = ScaledCamera.Instance.camera.ScreenPointToRay(Input.mousePosition);
                origin = ScaledSpace.ScaledToLocalSpace(ray.origin);
                dir = ray.direction.normalized;
            } else {
                //Attempt ray cast and return results if successfull.
                Ray ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitB;
                float dist = (float)(Vector3.Distance(body.position, ray.origin) - body.Radius/2);
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
            } else {
                latlon = Vector2.zero;
                return false;
            }
        }

        public static bool CBhit(CelestialBody body, Vector3d originalOrigin, Vector3d direction, out Vector3d hit) {
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
            if (disc < 0) {
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
            if (t0 > t1) {
                // if t0 is bigger than t1 swap them around
                double temp = t0;
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
                // else the intersection point is at t0
            else {
                hit = originalOrigin + (t0 * direction);
                return true;
            }
        }

        public static float GetHDG(Vector3 dir, Vector3 up, Vector3 north) {
            return Quaternion.Inverse(Quaternion.Inverse(Quaternion.LookRotation(dir, up)) * Quaternion.LookRotation(north, up)).eulerAngles.y;
        }
    }
}
