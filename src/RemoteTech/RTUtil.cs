using System;
using KSP.IO;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RemoteTech.SimpleTypes;
using UnityEngine;
using Object = System.Object;
using Debug = UnityEngine.Debug;

namespace RemoteTech
{
    public static partial class RTUtil
    {
        public static double GameTime { get { return Planetarium.GetUniversalTime(); } }

        public static readonly String[]
            DistanceUnits = { "", "k", "M", "G", "T" },
            ClassDescripts = {  "Short-Planetary (SP)",
                                "Medium-Planetary (MP)",
                                "Long-Planetary (LP)",
                                "Short-Interplanetary (SI)",
                                "Medium-Interplanetary (MI)",
                                "Long-Interplanetary (LI)"};
        
        public static double TryParseDuration(String duration)
        {
            TimeStringConverter time;

            if (GameSettings.KERBIN_TIME == true)
            {
                time = new KerbinTimeStringConverter();
            }
            else
            {
                time = new EarthTimeStringConverter();
            }

            return time.parseString(duration);
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
            if (degrees > 180)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static String FormatDuration(double duration, bool withMicroSecs = true)
        {
            TimeStringConverter time;

            if (GameSettings.KERBIN_TIME == true)
            {
                time = new KerbinTimeStringConverter();
            }
            else
            {
                time = new EarthTimeStringConverter();
            }

            return time.parseDouble(duration, withMicroSecs);
        }

        /// <summary>
        /// Generates a string for use in flight log entries
        /// </summary>
        /// <returns>A string in the same format as used by stock flight log events</returns>
        /// <param name="years">The number of full years the mission has lasted</param>
        /// <param name="days">The number of additional days the mission has lasted</param>
        /// <param name="hours">The number of additional hours the mission has lasted</param>
        /// <param name="minutes">The number of additional minutes the mission has lasted</param>
        /// <param name="seconds">The number of additional seconds the mission has lasted</param>
        /// 
        /// <precondition>All numerical arguments non-negative</precondition>
        /// 
        /// <exceptionsafe>Does not throw exceptions</exceptionsafe>
        public static String FormatTimestamp(int years, int days, int hours, int minutes, int seconds)
        {
            return String.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
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

        /// <summary>
        /// Searches a ProtoPartModuleSnapshot for an integer field
        /// </summary>
        /// 
        /// <returns>The value of the field named by <paramref name="value"/> in the PartModule represented 
        ///     by <paramref name="ppms"/></returns>
        /// <param name="ppms">The ProtoPartModule to query</param>
        /// <param name="value">The name of a member PartModule </param>
        /// 
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="value"/> does not exist 
        ///     or cannot be parsed as an integer.</exception>
        /// <exceptionsafe>The program state is unchanged in the event of an exception.</exceptionsafe>
        public static int GetInt(this ProtoPartModuleSnapshot ppms, String value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            int result;
            if (Int32.TryParse(n.GetValue(value) ?? "", out result)) {
                return result;
            } else {
                throw new ArgumentException (String.Format ("No integer '{0}' in ProtoPartModule", value), "value");
            }
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
            try 
	        {
		        Assembly myAssembly = Assembly.GetExecutingAssembly();
                Stream resStream = myAssembly.GetManifestResourceStream("RemoteTech.Resources." + fileName);

                if (resStream.Length <= 0) {
                    RTLog.Notify("LoadImageFromRessource({0}) failed", fileName);
                    throw new Exception("No ImageRessource found");
                }

                RTLog.Notify("LoadImageFromRessource({0}) success", fileName);
                // create a byte array from the stream ressource
                byte[] imageStream = new byte[resStream.Length];
                resStream.Read(imageStream, 0, (int)resStream.Length);
                // apply the image stream to a new Texture2D object
                texture = new Texture2D(4, 4, TextureFormat.ARGB32, false);
                texture.LoadImage(imageStream);

                imageStream = null;
                resStream.Close();
	        }
	        catch (Exception)
	        {
                texture = new Texture2D(32, 32);
                texture.SetPixels32(Enumerable.Repeat((Color32) Color.magenta, 32 * 32).ToArray());
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
    }
}