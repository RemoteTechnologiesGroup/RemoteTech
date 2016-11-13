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
    public static class GameUtil
    {
        /// <summary>Automatically finds the proper texture directory from the DLL location. Assumes the DLL is in the proper location of GameData/RemoteTech/Plugins/</summary>
        /// <returns>The texture directory string if found otherwise a null reference.</returns>
        private static string TextureDirectory {
            get
            {
                var location = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location))
                {
                    var parentLocation = Directory.GetParent(location).Parent;
                    if (parentLocation != null)
                        return parentLocation.Name + "/Textures/";

                    RTLog.Notify("TextureDirectory: cannot Find parent location", RTLogLevel.LVL4);
                    return null;
                }

                RTLog.Notify("TextureDirectory: cannot Find location", RTLogLevel.LVL4);
                return null;
            }
        }

        /// <summary>True if the current running game is a SCENARIO or SCENARIO_NON_RESUMABLE, otherwise false</summary>
        public static bool IsGameScenario => (HighLogic.CurrentGame != null && (HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO || HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO_NON_RESUMABLE));

        /// <summary>Load an image from the texture directory.</summary>
        /// <param name="texture">The output texture if the texture is found, otherwise a black texture.</param>
        /// <param name="fileName">The file name of the texture (in the texture directory).</param>
        /// <remarks>Replaces old manual method with unity style texture loading.</remarks>
        public static void LoadImage(out Texture2D texture, string fileName)
        {
            var str = TextureDirectory + fileName;
            if (GameDatabase.Instance.ExistsTexture(str))
                texture = GameDatabase.Instance.GetTexture(str, false);
            else
            {
                RTLog.Notify($"LoadImage: cannot Find Texture: {str}", RTLogLevel.LVL4);
                texture = Texture2D.blackTexture;
            }
        }

        /// <summary>Load an image from the texture directory.</summary>
        /// <param name="fileName">The file name of the texture (in the texture directory).</param>
        /// <returns>The texture if the file was found, otherwise a completely black texture.</returns>
        /// <remarks>Replaces old manual method with unity style texture loading.</remarks>
        public static Texture2D LoadImage(string fileName)
        {
            var str = TextureDirectory + fileName;
            if (GameDatabase.Instance.ExistsTexture(str))
                return GameDatabase.Instance.GetTexture(str, false);

            RTLog.Notify($"LoadImage: cannot Find Texture: {str}", RTLogLevel.LVL4);
            return Texture2D.blackTexture;
        }

        /// <summary>Returns the current AssemblyFileVersion, as a string, from AssemblyInfos.cs.</summary>
        public static string Version
        {
            get
            {
                var executableAssembly = Assembly.GetExecutingAssembly();
                if (!string.IsNullOrEmpty(executableAssembly.Location))
                    return "v" + FileVersionInfo.GetVersionInfo(executableAssembly.Location).ProductVersion;

                RTLog.Notify("Executing assembly is null", RTLogLevel.LVL4);
                return "Unknown version";
            }
        }
    }

    public static class TimeUtil
    {
        /// <summary>Format a <see cref="double"/>duration into a string.</summary>
        /// <param name="duration">The time duration as a double.</param>
        /// <param name="withMicroSecs">Whether or not to include microseconds in the output.</param>
        /// <returns>A string corresponding to the <paramref name="duration"/> input parameter.</returns>
        public static string FormatDuration(double duration, bool withMicroSecs = true)
        {
            TimeStringConverter time;

            if (GameSettings.KERBIN_TIME)
            {
                time = new KerbinTimeStringConverter();
            }
            else
            {
                time = new EarthTimeStringConverter();
            }

            return time.parseDouble(duration, withMicroSecs);
        }

        /// <summary>Generates a string for use in flight log entries.</summary>
        /// <returns>A string in the same format as used by stock flight log events</returns>
        /// <param name="years">The number of full years the mission has lasted</param>
        /// <param name="days">The number of additional days the mission has lasted</param>
        /// <param name="hours">The number of additional hours the mission has lasted</param>
        /// <param name="minutes">The number of additional minutes the mission has lasted</param>
        /// <param name="seconds">The number of additional seconds the mission has lasted</param>
        /// <precondition>All numerical arguments non-negative</precondition>
        /// <exceptionsafe>Does not throw exceptions</exceptionsafe>
        public static string FormatTimestamp(int years, int days, int hours, int minutes, int seconds)
        {
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        /// <summary>The simulation time, in seconds, since this save was started.</summary>
        public static double GameTime => Planetarium.GetUniversalTime();

        /// <summary>
        /// Try to parse a duration from a string to a double value.
        /// </summary>
        /// <param name="duration">A duration, as a string.</param>
        /// <returns>The <see cref="double"/> value corresponding to the <paramref name="duration"/> input string.</returns>
        public static double TryParseDuration(string duration)
        {
            TimeStringConverter time;

            if (GameSettings.KERBIN_TIME)
            {
                time = new KerbinTimeStringConverter();
            }
            else
            {
                time = new EarthTimeStringConverter();
            }

            return time.parseString(duration);
        }
    }

    public static class GuiUtil
    {
        /// <summary>This time member is needed to debounce the RepeatButton</summary>
        private static double _timeDebouncer = (HighLogic.LoadedSceneHasPlanetarium) ? TimeUtil.GameTime : 0;

        public static void Button(Texture2D icon, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(icon, options))
            {
                onClick.Invoke();
            }
        }

        public static void Button(string text, Action onClick, params GUILayoutOption[] options)
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

        /// <summary>
        /// Draws a repeat button. If you hold the mouse click the <paramref name="onClick"/>-callback
        /// will be triggered at least every 0.05 seconds.
        /// </summary>
        /// <param name="text">Text for the button</param>
        /// <param name="onClick">Callback to trigger for every repeat</param>
        /// <param name="options">GUILayout params</param>
        public static void RepeatButton(string text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.RepeatButton(text, options) && (_timeDebouncer + 0.05) < TimeUtil.GameTime)
            {
                onClick.Invoke();
                // set the new time to the debouncer
                _timeDebouncer = TimeUtil.GameTime;
            }
        }

        /// <summary>
        /// Draw a fake toggle button. It is an action button with a toggle functionality. When <param name="state" /> and
        /// <param name="value" /> are equal the background of the button will change to black.
        /// </summary>
        public static void FakeStateButton(GUIContent text, Action onClick, int state, int value, params GUILayoutOption[] options)
        {
            var pushBgColor = GUI.backgroundColor;
            if (state == value)
            {
                GUI.backgroundColor = Color.black;
            }

            if (GUILayout.Button(text, options))
            {
                onClick.Invoke();
            }
            GUI.backgroundColor = pushBgColor;
        }

        public static void HorizontalSlider(ref float state, float min, float max, params GUILayoutOption[] options)
        {
            state = GUILayout.HorizontalSlider(state, min, max, options);
        }

        public static void GroupButton(int wide, string[] text, ref int group, params GUILayoutOption[] options)
        {
            group = GUILayout.SelectionGrid(group, text, wide, options);
        }

        public static void GroupButton(int wide, string[] text, ref int group, Action<int> onStateChange, params GUILayoutOption[] options)
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
            if ((result = GUILayout.Toggle(Equals(state, value), text, GUI.skin.button, options)) != Equals(state, value))
            {
                onStateChange.Invoke(result ? value : ~value);
            }
        }

        public static void StateButton<T>(GUIContent text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Equals(state, value), text, GUI.skin.button, options)) != Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }

        public static void StateButton<T>(string text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Equals(state, value), text, GUI.skin.button, options)) != Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }

        public static void TextField(ref string text, params GUILayoutOption[] options)
        {
            text = GUILayout.TextField(text, options);
        }

        /// <summary>
        /// Draws a Textfield with a functionality to use the mouse wheel to trigger
        /// the events <paramref name="onWheelDown"/> and <paramref name="onWheelUp"/>.
        /// The callbacks will only be triggered if the textfield is focused while using
        /// the mouse wheel.
        /// </summary>
        /// <param name="text">Reference to the input value</param>
        /// <param name="fieldName">Name for this field</param>
        /// <param name="onWheelDown">Action trigger for the mousewheel down event</param>
        /// <param name="onWheelUp">Action trigger for the mousewheel up event</param>
        /// <param name="options">GUILayout params</param>
        public static void MouseWheelTriggerField(ref string text, string fieldName, Action onWheelDown, Action onWheelUp, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(fieldName);
            text = GUILayout.TextField(text, options);

            // Current textfield under control?
            if ((GUI.GetNameOfFocusedControl() == fieldName))
            {
                if (Input.GetAxis("Mouse ScrollWheel") > 0 && (_timeDebouncer + 0.05) < TimeUtil.GameTime)
                {
                    onWheelDown.Invoke();
                    _timeDebouncer = TimeUtil.GameTime;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0 && (_timeDebouncer + 0.05) < TimeUtil.GameTime)
                {
                    onWheelUp.Invoke();
                    _timeDebouncer = TimeUtil.GameTime;
                }
            }
        }

        public static bool ContainsMouse(this Rect window)
        {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }

        public static IEnumerable<Transform> FindTransformsWithCollider(Transform input)
        {
            if (input.GetComponent<Collider>() != null)
            {
                yield return input;
            }

            foreach (Transform t in input)
            {
                foreach (var x in FindTransformsWithCollider(t))
                {
                    yield return x;
                }
            }
        }

        public static void ScreenMessage(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4.0f, ScreenMessageStyle.UPPER_LEFT));
        }
    }

    public static partial class RTUtil
    {
        public static readonly string[]
            DistanceUnits = { "", "k", "M", "G", "T" },
            ClassDescripts = {  "Short-Planetary (SP)",
                                "Medium-Planetary (MP)",
                                "Long-Planetary (LP)",
                                "Short-Interplanetary (SI)",
                                "Medium-Interplanetary (MI)",
                                "Long-Interplanetary (LI)"};




        public static string Truncate(this string targ, int len)
        {
            const string suffix = "...";
            if (targ.Length > len)
            {
                return targ.Substring(0, len - suffix.Length) + suffix;
            }
            return targ;
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

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
        }



        public static Guid Guid(this CelestialBody cb)
        {
            var name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (var i = 0; i < 16; i++)
            {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            return new Guid(s.ToString());
        }



        //Note: Keep this method even if it has no reference, it is useful to track some bugs.
        /// <summary>
        /// Get a private field value from an object instance though reflection.
        /// </summary>
        /// <param name="type">The type of the object instance from which to obtain the private field.</param>
        /// <param name="instance">The object instance</param>
        /// <param name="fieldName">The field name in the object instance, from which to obtain the value.</param>
        /// <returns>The value of the <paramref name="fieldName"/> instance or null if no such field exist in the instance.</returns>
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                           | BindingFlags.Static;
            var field = type.GetField(fieldName, bindFlags);
            return field?.GetValue(instance);
        }

 
        
        public static bool IsTechUnlocked(string techid)
        {
            if (techid.Equals("None")) return true;
            return HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX ||
                ResearchAndDevelopment.GetTechnologyState(techid) == RDTech.State.Available;
        }
        public static string ConstrictNum(string s) {
            return ConstrictNum(s, true);
        }

        public static string ConstrictNum(string s, float max) {

            var tmp = ConstrictNum(s, false);

            float f;

            float.TryParse(tmp, out f);

            return f > max ? max.ToString("00") : tmp;
        }

        public static string ConstrictNum(string s, bool allowNegative) {
            var tmp = new StringBuilder();
            if (allowNegative && s.StartsWith("-"))
                tmp.Append(s[0]);
            var point = false;

            foreach (var c in s) {
                if (char.IsNumber(c))
                    tmp.Append(c);
                else if (!point && (c == '.' || c == ',')) {
                    point = true;
                    tmp.Append('.');
                }
            }
            return tmp.ToString();
        }

        public static bool CBhit(CelestialBody body, out Vector2 latlon) {

            Vector3d hitA;
            Vector3 origin, dir;

            if (MapView.MapIsEnabled) {
                //Use Scaled camera and don't attempt physics raycast if in map view.
                var ray = ScaledCamera.Instance.galaxyCamera.ScreenPointToRay(Input.mousePosition);
                origin = ScaledSpace.ScaledToLocalSpace(ray.origin);
                dir = ray.direction.normalized;
            } else {
                //Attempt ray cast and return results if successfull.
                var ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitB;
                var dist = (float)(Vector3.Distance(body.position, ray.origin) - body.Radius / 2);
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
            }
            latlon = Vector2.zero;
            return false;
        }

        public static bool CBhit(CelestialBody body, Vector3d originalOrigin, Vector3d direction, out Vector3d hit) {
            var r = body.Radius;
            //convert the origin point from world space to body local space and assume body center as (0,0,0).
            var origin = originalOrigin - body.position;

            //Compute A, B and C coefficients
            var a = Vector3d.Dot(direction, direction);
            var b = 2 * Vector3d.Dot(direction, origin);
            var c = Vector3d.Dot(origin, origin) - (r * r);

            //Find discriminant
            var disc = b * b - 4 * a * c;

            // if discriminant is negative there are no real roots, so return 
            // false as ray misses sphere
            if (disc < 0) {
                hit = Vector3d.zero;
                return false;
            }

            // compute q.
            var distSqrt = Math.Sqrt(disc);
            double q;
            if (b < 0)
                q = (-b - distSqrt) / 2.0;
            else
                q = (-b + distSqrt) / 2.0;

            // compute t0 and t1
            var t0 = q / a;
            var t1 = c / q;

            // make sure t0 is smaller than t1
            if (t0 > t1) {
                // if t0 is bigger than t1 swap them around
                var temp = t0;
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

            // the intersection point is at t0
            hit = originalOrigin + (t0 * direction);
            return true;
        }

        public static float GetHeading(Vector3 dir, Vector3 up, Vector3 north) {
            return Quaternion.Inverse(Quaternion.Inverse(Quaternion.LookRotation(dir, up)) * Quaternion.LookRotation(north, up)).eulerAngles.y;
        }

        public static double ClampDegrees360(double angle)
        {
            angle = angle % 360.0;
            return angle < 0 ? angle + 360.0 : angle;
        }

        public static double ClampDegrees180(double angle) {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float ClampDegrees360(float angle)
        {
            angle = angle % 360f;
            return angle < 0 ? angle + 360f : angle;
        }

        public static float ClampDegrees180(float angle) {
            angle = ClampDegrees360(angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public static float AngleBetween(float angleFrom, float angleTo) {
            var angle = angleFrom - angleTo;
            while (angle < -180) angle += 360;
            while (angle > 180) angle -= 360;
            return angle;
        }

        public static float ClampDegrees90(float angle) {
            if (angle > 90)
                angle -= 180;
            else if (angle < -90)
                angle += 180;
            return angle;
        }

        /// <summary>
        /// Returns a vessel object by the given <paramref name="vesselid"/> or
        /// null if no vessel was found
        /// </summary>
        /// <param name="vesselid">Guid of a vessel</param>
        public static Vessel GetVesselById(Guid vesselid)
        {
            return FlightGlobals.Vessels.FirstOrDefault(vessel => vessel.id == vesselid);
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


    public static partial class RTUtil
    {
        public static bool GetBool(this ProtoPartModuleSnapshot ppms, string value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return bool.TryParse(n.GetValue(value) ?? "False", out result) && result;
        }

        /// <summary>Searches a ProtoPartModuleSnapshot for an integer field.</summary>
        /// <returns>True if the member <paramref name="valueName"/> exists, false otherwise.</returns>
        /// <param name="ppms">The <see cref="ProtoPartModuleSnapshot"/> to query.</param>
        /// <param name="valueName">The name of a member in the  ProtoPartModuleSnapshot.</param>
        /// <param name="value">The value of the member <paramref name="valueName"/> on success. An undefined value on failure.</param>
        public static bool GetInt(this ProtoPartModuleSnapshot ppms, string valueName, out int value)
        {
            value = 0;
            var result = ppms.moduleValues.TryGetValue(valueName, ref value);
            if (!result)
            {
                RTLog.Notify($"No integer '{value}' in ProtoPartModule '{ppms.moduleName}'");
            }

            return result;
        }

        public static bool HasValue(this ProtoPartModuleSnapshot ppms, string value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return bool.TryParse(value, out result) && result;
        }

        public static bool IsAntenna(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTAntenna") &&
                   ppms.GetBool("IsRTPowered") &&
                   ppms.GetBool("IsRTActive");
        }

        public static bool IsSignalProcessor(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTSignalProcessor");

        }

        public static bool IsCommandStation(this ProtoPartModuleSnapshot ppms)
        {
            return ppms.GetBool("IsRTCommandStation");
        }

        public static bool IsAntenna(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTAntenna") &&
                   pm.Fields.GetValue<bool>("IsRTPowered") &&
                   pm.Fields.GetValue<bool>("IsRTActive");
        }

        public static bool IsCommandStation(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTCommandStation");
        }

        public static bool IsSignalProcessor(this PartModule pm)
        {
            return pm.Fields.GetValue<bool>("IsRTSignalProcessor");
        }


        public static ISignalProcessor GetSignalProcessor(this Vessel v)
        {
            RTLog.Notify("GetSignalProcessor({0}): Check", v.vesselName);

            ISignalProcessor result = null;

            if (v.loaded && v.parts.Count > 0)
            {
                var partModuleList = v.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Where(pm => pm.IsSignalProcessor()).ToList();
                // try to look for a moduleSPU
                result = partModuleList.FirstOrDefault(pm => pm.moduleName == "ModuleSPU") as ISignalProcessor ??
                         partModuleList.FirstOrDefault() as ISignalProcessor;
            }
            else
            {
                var protoPartList = v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Where(ppms => ppms.IsSignalProcessor()).ToList();
                // try to look for a moduleSPU on a unloaded vessel
                var protoPartProcessor = protoPartList.FirstOrDefault(ppms => ppms.moduleName == "ModuleSPU") ??
                                         protoPartList.FirstOrDefault();

                // convert the found protoPartSnapshots to a ProtoSignalProcessor
                if (protoPartProcessor != null)
                {
                    result = new ProtoSignalProcessor(protoPartProcessor, v);
                }
            }

            return result;
        }

        public static bool HasCommandStation(this Vessel v)
        {
            RTLog.Notify("HasCommandStation({0})", v.vesselName);
            if (v.loaded && v.parts.Count > 0)
            {
                return v.Parts.SelectMany(p => p.Modules.Cast<PartModule>()).Any(pm => pm.IsCommandStation());
            }
            return v.protoVessel.protoPartSnapshots.SelectMany(x => x.modules).Any(pm => pm.IsCommandStation());
        }
    }
}
