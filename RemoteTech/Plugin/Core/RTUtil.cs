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

        public static String GetAnimationArrows() {
            switch ((long)Planetarium.GetUniversalTime() % 4) {
                default:
                case 0:
                    return "";
                case 1:
                    return "»";
                case 2:
                    return "»»";
                case 3:
                    return "»»»";
            }
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

        public static bool IsSignalProcessor(this ProtoPartModuleSnapshot ppms) {
            return ppms.HasValue("IsRTSignalProcessor");
        }

        public static bool IsSignalProcessor(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTSignalProcessor");
        }

        public static bool HasSignalProcessor(this Vessel v) {
            RTUtil.Log("RTUtil: HasSignalProcessor: " + v);
            if(!v.loaded) {
                foreach (ProtoPartModuleSnapshot ppms in v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules)) {
                    if (ppms.IsSignalProcessor()) {
                        return true;
                    }
                }

            } else {
                foreach (Part p in v.Parts) {
                    foreach (PartModule pm in p.Modules) {
                        if(pm.IsSignalProcessor()) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms) {
            return ppms.HasValue("IsRTAntenna");
        }

        public static bool IsAntenna(this PartModule pm) {
            return pm.Fields.GetValue<bool>("IsRTAntenna");
        }

        // GUI
        public delegate void ClickDelegate();
        public delegate void StateDelegate(int state);

        public static void Button(int width, int height, String text, ClickDelegate onClick) {
            if(GUILayout.Button(text, GUILayout.Height(height), GUILayout.Width(width))) {
                onClick.Invoke();
            }
        }

        public static void Button(int width, int height, String text) {
            GUILayout.Button(text, GUILayout.Height(height), GUILayout.Width(width));
        }

        public static void GroupButton(int width, int height, String[] text, ref int group) {
            group = GUILayout.SelectionGrid(group, text, 1, GUILayout.Height(height * text.Length), GUILayout.Width(width));
        }

        public static void GroupButton(int width, int height, String[] text, ref int group, StateDelegate onStateChange) {
            int group2;
            if ((group2 = GUILayout.SelectionGrid(group, text, 1, GUILayout.Height(height * text.Length), GUILayout.Width(width))) != group) {
                group = group2;
                onStateChange.Invoke(group2);
            }
        }

        public static void ToggleButton(int width, int height, String text, ref bool state, StateDelegate onStateChange) {
            if (GUILayout.Toggle(state, text, GUI.skin.button, GUILayout.Height(height), GUILayout.Width(width)) != state) {
                state = !state;
                onStateChange.Invoke(state ? 1 : 0);
            }
        }

        public static void Label(int width, int height, String text) {
            GUILayout.Label(text, GUILayout.Height(height), GUILayout.Width(width));
        }

        public static void TextField(int width, int height, ref String text) {
            text = GUILayout.TextField(text, GUILayout.Height(height), GUILayout.Width(width));
        }
        
        public static bool IsMouseInWindow(Vector2 mouse, Rect window) {
            return (mouse.x > window.x) && (mouse.x < (window.x + window.width)) &&
                   (mouse.y > window.y) && (mouse.y < (window.y + window.height));
        }

    }
}

