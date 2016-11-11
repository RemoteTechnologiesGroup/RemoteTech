using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTech
{
    public static partial class RTUtil
    {
        public static double GameTime { get { return Planetarium.GetUniversalTime(); } }
        /// <summary>This time member is needed to debounce the RepeatButton</summary>
        private static double TimeDebouncer = (HighLogic.LoadedSceneHasPlanetarium) ? RTUtil.GameTime : 0;

        /// <summary>
        /// Automatically finds the proper texture directory from the dll location. Assumes the dll is in the proper location of GameData/RemoteTech/Plugins/
        /// </summary>
        private static string TextureDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Name + "/Textures/";

        /// <summary>
        /// Returns the current AssemplyFileVersion from AssemblyInfos.cs
        /// </summary>
        public static string Version
        {
            get
            {
                Assembly executableAssembly = Assembly.GetExecutingAssembly();
                return "v" + FileVersionInfo.GetVersionInfo(executableAssembly.Location).ProductVersion;
            }
        }

        /// <summary>
        /// True if the current running game is a SCENARIO or SCENARIO_NON_RESUMABLE, otherwise false
        /// </summary>
        public static bool IsGameScenario
        {
            get
            {
                return (HighLogic.CurrentGame != null && (HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO || HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO_NON_RESUMABLE));
            }
        }

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

        public static void ScreenMessage(String msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4.0f, ScreenMessageStyle.UPPER_LEFT));
        }

        public static String Truncate(this String targ, int len)
        {
            const String SUFFIX = "...";
            if (targ.Length > len)
            {
                return targ.Substring(0, len - SUFFIX.Length) + SUFFIX;
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

        public static String FormatDuration(double duration, bool withMicroSecs = true)
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
            String timeindicator = "sec";

            /* Disabled to follow the stock consumption format
            if(consumption < 1)
            {
                // minutes
                consumption *= 60;
                timeindicator = "min";
            }
            */
            
            return String.Format("{0:F2}/{1}.", consumption, timeindicator);
        }

        public static String FormatSI(double value, String unit)
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

        public static String TargetName(Guid guid)
        {
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
                ISatellite sat;
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
            return Boolean.TryParse(value, out result) && result;
        }

        public static bool GetBool(this ProtoPartModuleSnapshot ppms, String value)
        {
            var n = new ConfigNode();
            ppms.Save(n);
            bool result;
            return Boolean.TryParse(n.GetValue(value) ?? "False", out result) && result;
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

        /// <summary>
        /// Draws a repeat button. If you hold the mouse click the <paramref name="onClick"/>-callback
        /// will be triggered at least every 0.05 seconds.
        /// </summary>
        /// <param name="text">Text for the button</param>
        /// <param name="onClick">Callback to trigger for every repeat</param>
        /// <param name="options">GUILayout params</param>
        public static void RepeatButton(String text, Action onClick, params GUILayoutOption[] options)
        {
            if (GUILayout.RepeatButton(text, options) && (RTUtil.TimeDebouncer + 0.05) < RTUtil.GameTime)
            {
                onClick.Invoke();
                // set the new time to the debouncer
                RTUtil.TimeDebouncer = RTUtil.GameTime;
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

        public static void StateButton<T>(String text, T state, T value, Action<int> onStateChange, params GUILayoutOption[] options)
        {
            bool result;
            if ((result = GUILayout.Toggle(Equals(state, value), text, GUI.skin.button, options)) != Equals(state, value))
            {
                onStateChange.Invoke(result ? 1 : -1);
            }
        }

        public static void TextField(ref String text, params GUILayoutOption[] options)
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
        public static void MouseWheelTriggerField(ref String text, string fieldName, Action onWheelDown, Action onWheelUp, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(fieldName);
            text = GUILayout.TextField(text, options);

            // Current textfield under control?
            if((GUI.GetNameOfFocusedControl() == fieldName))
            {
                if (Input.GetAxis("Mouse ScrollWheel") > 0 && (TimeDebouncer + 0.05) < RTUtil.GameTime)
                {
                    onWheelDown.Invoke();
                    TimeDebouncer = RTUtil.GameTime;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0 && (TimeDebouncer + 0.05) < RTUtil.GameTime)
                {
                    onWheelUp.Invoke();
                    TimeDebouncer = RTUtil.GameTime;
                }
            }
        }

        public static bool ContainsMouse(this Rect window)
        {
            return window.Contains(new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y));
        }

        // Replaces old manual method with unity style texture loading
        public static void LoadImage(out Texture2D texture, String fileName)
        {
            string str = TextureDirectory + fileName;
            if (GameDatabase.Instance.ExistsTexture(str))
                texture = GameDatabase.Instance.GetTexture(str, false);
            else
            {
                UnityEngine.Debug.Log("Cannot Find Texture: " + str);
                texture = Texture2D.blackTexture;
            }
        }

        // New method for ease of use
        public static Texture2D LoadImage(String fileName)
        {
            string str = TextureDirectory + fileName;
            if (GameDatabase.Instance.ExistsTexture(str))
                return GameDatabase.Instance.GetTexture(str, false);
            else
            {
                UnityEngine.Debug.Log("Cannot Find Texture: " + str);
                return Texture2D.blackTexture;
            }
        }

        public static IEnumerable<Transform> FindTransformsWithCollider(Transform input)
        {
            if (input.GetComponent<Collider>() != null)
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
            if (cachedField.Frame == Time.frameCount)
            {
                return cachedField.Field;
            }

            cachedField.Frame = Time.frameCount;
            return cachedField.Field = getter();
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

        public static String ConstrictNum(string s, float max) {

            string tmp = ConstrictNum(s, false);

            float f;

            Single.TryParse(tmp, out f);

            return f > max ? max.ToString("00") : tmp;
        }

        public static string ConstrictNum(string s, bool allowNegative) {
            var tmp = new StringBuilder();
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

        public static bool CBhit(CelestialBody body, out Vector2 latlon) {

            Vector3d hitA;
            Vector3 origin, dir;

            if (MapView.MapIsEnabled) {
                //Use Scaled camera and don't attempt physics raycast if in map view.
                Ray ray = ScaledCamera.Instance.galaxyCamera.ScreenPointToRay(Input.mousePosition);
                origin = ScaledSpace.ScaledToLocalSpace(ray.origin);
                dir = ray.direction.normalized;
            } else {
                //Attempt ray cast and return results if successfull.
                Ray ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
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
            float angle = angleFrom - angleTo;
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
            throw new ArgumentException("Invalid order", "order");
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
}
