using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public static class RTUtil {

        public static void Log(string message) {
            Debug.Log("RemoteTech: " + message);
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) {
            return "{" + string.Join(",", dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString()).ToArray()) + "}";
        }

        public static string ToDebugString<T>(this List<T> list) {
            return "{" + string.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }

        public static long GetGameTime() {
            return (long)(Planetarium.GetUniversalTime() * 1000);
        }

        public static String TargetName(Guid guid) {
            ISatellite sat;

            if(RTCore.Instance.Network.Planets.ContainsKey(guid)) {
                return RTCore.Instance.Network.Planets[guid].name;
            }
            if((sat = RTCore.Instance.Satellites.WithGuid(guid)) != null) {
                return sat.Name;
            }
            if(guid.Equals(RTCore.Instance.Network.MissionControl.Guid)) {
                return RTCore.Instance.Network.MissionControl.Name;
            }
            return "[Unknown Target]";
        }

        public static Guid Guid(this CelestialBody cb) {
            char[] name = cb.GetName().ToCharArray();
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < 16; i++) {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            RTUtil.Log("cb.Guid: " + s.ToString());
            return new Guid(s.ToString());
        }

        public static String[] DistanceUnits = { "m", "km", "Mm", "Gm", "Tm" };

        public static String FormatDistance(float dist) {
            // Engineering notation
            int i;
            for(i = 0; dist > 1000 && i < DistanceUnits.Length; i++) {
                dist /= 1000;
            }
            return dist.ToString("G6") + DistanceUnits[i];
        }

        public static bool HasValue(this ProtoPartModuleSnapshot ppms, String value) {
            ConfigNode n = new ConfigNode();
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
            ConfigNode n = new ConfigNode();
            ppms.Save(n);
            try {
                return Boolean.Parse(n.GetValue(value) ?? "False");
            } catch (ArgumentException) {
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
            RTUtil.Log("RTUtil: HasSignalProcessor: " + v);
            if(!v.loaded) {
                foreach (ProtoPartModuleSnapshot ppms in v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules)) {
                    if (ppms.IsSignalProcessor()) {
                        return new ProtoSignalProcessor(ppms, v);
                    }
                }

            } else {
                foreach (Part p in v.Parts) {
                    foreach (PartModule pm in p.Modules) {
                        if(pm.IsSignalProcessor()) {
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

        public static void Button(String text, OnClick onClick) {
            if(GUILayout.Button(text)) {
                onClick.Invoke();
            }
        }

        public static void GroupButton(int wide, String[] text, ref int group) {
            group = GUILayout.SelectionGrid(group, text, wide);
        }

        public static void GroupButton(int wide, String[] text, ref int group, OnState onStateChange) {
            int group2;
            if ((group2 = GUILayout.SelectionGrid(group, text, wide)) != group) {
                group = group2;
                onStateChange.Invoke(group2);
            }
        }

        public static void ToggleButton(String text, ref bool state, OnState onStateChange) {
            if (GUILayout.Toggle(state, text, GUI.skin.button) != state) {
                state = !state;
                onStateChange.Invoke(state ? 1 : 0);
            }
        }

        public static void TextField(ref String text) {
            text = GUILayout.TextField(text);
        }
        
        public static bool IsMouseInWindow(Vector2 mouse, Rect window) {
            return (mouse.x > window.x) && (mouse.x < (window.x + window.width)) &&
                   (mouse.y > window.y) && (mouse.y < (window.y + window.height));
        }

    }
}

