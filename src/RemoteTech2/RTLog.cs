using System.Collections.Generic;
using System.Linq;

namespace RemoteTech
{
    public static class RTLog
    {
        private static readonly bool verboseLogging;

        static RTLog()
        {
            verboseLogging = GameSettings.VERBOSE_DEBUG_LOG;
        }

        public static void Notify(string message)
        {
            UnityEngine.Debug.Log("RemoteTech: " + message);
        }

        public static void Notify(string message, params UnityEngine.Object[] param)
        {
            UnityEngine.Debug.Log(string.Format("RemoteTech: " + message, param));
        }

        public static void Notify(string message, params object[] param)
        {
            UnityEngine.Debug.Log(string.Format("RemoteTech: " + message, param));
        }

        public static void Verbose(string message, params object[] param)
        {
            if (verboseLogging)
            {
                Notify(message, param);
            }
        }

        public static void Verbose(string message, params UnityEngine.Object[] param)
        {
            if (verboseLogging)
            {
                Notify(message, param);
            }
        }
    }

    public static class LoggingExtenstions
    {
        public static string ToDebugString<T>(this List<T> list)
        {
            return "{" + string.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }
    }
}