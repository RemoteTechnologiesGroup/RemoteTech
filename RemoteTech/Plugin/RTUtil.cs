using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = System.Object;

namespace RemoteTech {
    public static class RTUtil {
        public static String[] DistanceUnits = {"m", "km", "Mm", "Gm", "Tm"};

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

        public static String FormatDistance(float dist) {
            // Engineering notation
            int i;
            for (i = 0; dist > 1000 && i < DistanceUnits.Length; i++) {
                dist /= 1000;
            }
            return dist.ToString("G6") + DistanceUnits[i];
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
            return ppms.HasValue("IsRTSignalProcessor");
        }

        public static bool IsSignalProcessor(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTSignalProcessor");
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

        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms) {
            return ppms.HasValue("IsRTAntenna");
        }

        public static bool IsAntenna(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTAntenna");
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

        public static void ToggleButton(String text, ref bool state, OnState onStateChange,
                                        params GUILayoutOption[] options) {
            if (GUILayout.Toggle(state, text, GUI.skin.button, options) != state) {
                state = !state;
                onStateChange.Invoke(state ? 1 : 0);
            }
        }

        public static void TextField(ref String text, params GUILayoutOption[] options) {
            text = GUILayout.TextField(text, options);
        }

        public static bool IsMouseInWindow(Vector2 mouse, Rect window) {
            return (mouse.x > window.x) && (mouse.x < (window.x + window.width)) &&
                   (mouse.y > window.y) && (mouse.y < (window.y + window.height));
        }
    }
}
