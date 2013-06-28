using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = System.Object;

namespace RemoteTech {
    public static class RTUtil {
        public static readonly String[] DistanceUnits = {"", "k", "M", "G", "T"};

        private static readonly Regex mDurationRegex = 
            new Regex(@"(?:(?<seconds>\d+)\s*s[a-z]*[,\s]*)?" +
                      @"(?:(?<minutes>\d+)\s*m[a-z]*[,\s]*)?" +
                      @"(?:(?<hours>\d+)\s*h[a-z]*[,\s]*)?");

        public static bool TryParseDuration(String duration, out TimeSpan time) {
            time = new TimeSpan();
            Match match = mDurationRegex.Match(duration);
            if (match.Success) {
                if (match.Groups["seconds"].Success) {
                    time += TimeSpan.FromSeconds(Int32.Parse(match.Groups["seconds"].Value));
                }
                if (match.Groups["minutes"].Success) {
                    time += TimeSpan.FromMinutes(Int32.Parse(match.Groups["minutes"].Value));
                }
                if (match.Groups["hours"].Success) {
                    time += TimeSpan.FromHours(Int32.Parse(match.Groups["hours"].Value));
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
            if (time.Seconds > 1) {
                s.Append(time.Seconds);
                s.Append("s");
            }
            return s.ToString();
        }

        public static String FormatSI(double value, String unit) {
            int i = (int) RTUtil.Clamp(Math.Floor(Math.Log10(value)) / 3, 
                0,  DistanceUnits.Length - 1);
            value /= Math.Pow(1000, i);
            return value.ToString("F2") + DistanceUnits[i] + unit;
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
            
            if (guid == System.Guid.Empty) {
                return "No Target";
            }
            if (RTCore.Instance.Network.Planets.ContainsKey(guid)) {
                return RTCore.Instance.Network.Planets[guid].name;
            }
            if ((sat = RTCore.Instance.Satellites.For(guid)) != null) {
                return sat.Name;
            }
            if (guid.Equals((sat = RTCore.Instance.Network.MissionControl).Guid)) {
                return sat.Name;
            }
            return "[Unknown Target]";
        }

        public static Guid Guid(this CelestialBody cb) {
            char[] name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (int i = 0; i < 16; i++) {
                s.Append(((short) name[i%name.Length]).ToString("x"));
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
            }
            catch (ArgumentException) {
                /* nothing */
            }
            return false;
        }

        public static bool GetBool(this ProtoPartModuleSnapshot ppms, String value) {
            var n = new ConfigNode();
            ppms.Save(n);
            try {
                return Boolean.Parse(n.GetValue(value) ?? "False");
            }
            catch (ArgumentException) {
                return false;
            }
        }

        public static bool IsSignalProcessor(this ProtoPartModuleSnapshot ppms) {
            return ppms.GetBool("IsRTSignalProcessor") &&
                   ppms.GetBool("IsPowered");

        }

        public static bool IsSignalProcessor(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTSignalProcessor") &&
                   pm.Fields.GetValue<bool>("IsPowered");
        }

        public static ISignalProcessor GetSignalProcessor(this Vessel v) {
            Log("RTUtil: HasSignalProcessor: " + v);
            if (!v.loaded) {
                foreach (ProtoPartModuleSnapshot ppms in
                v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules)) {
                    if (ppms.IsSignalProcessor()) {
                        return new ProtoSignalProcessor(ppms, v);
                    }
                }
            }
            else {
                foreach (Part p in v.Parts) {
                    foreach (PartModule pm in p.Modules) {
                        if (pm.IsSignalProcessor()) {
                            return pm as ISignalProcessor;
                        }
                    }
                }
            }
            return null;
        }

        public static bool IsCommandStation(this ProtoPartModuleSnapshot ppms) {
            return ppms.GetBool("IsRTCommandStation") && 
                   ppms.GetBool("IsPowered");
        }

        public static bool IsCommandStation(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTCommandStation") &&
                   pm.Fields.GetValue<bool>("IsPowered");
        }

        public static bool HasCommandStation(this Vessel v) {
            Log("RTUtil: HasCommandStation: " + v);
            if (!v.loaded) {
                foreach (ProtoPartModuleSnapshot ppms in
                v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules)) {
                    if (ppms.IsCommandStation()) {
                        return true;
                    }
                }
            } else {
                foreach (Part p in v.Parts) {
                    foreach (PartModule pm in p.Modules) {
                        if (pm.IsCommandStation()) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms) {
            return ppms.GetBool("IsRTAntenna") && 
                   ppms.GetBool("IsPowered") &&
                   ppms.GetBool("IsRTActive");
        }

        public static bool IsAntenna(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTAntenna") && 
                   pm.Fields.GetValue<bool>("IsPowered") &&
                   pm.Fields.GetValue<bool>("IsRTActive");
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

        public static void StateButton(String text, bool state, OnState onStateChange,
                                        params GUILayoutOption[] options) {
            if (GUILayout.Toggle(state, text, GUI.skin.button, options) != state) {
                onStateChange.Invoke(state ? 0 : 1);
            }
        }

        public static void TextField(ref String text, params GUILayoutOption[] options) {
            text = GUILayout.TextField(text, options);
        }

        public static bool ContainsMouse(this Rect window) {
            return window.Contains(new Vector2(Input.mousePosition.x, 
                Screen.height - Input.mousePosition.y));
        }
    }
}
