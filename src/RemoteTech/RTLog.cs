using System.Collections.Generic;
using System.Linq;

namespace RemoteTech
{
    public static class RTLog
    {
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        private static readonly bool verboseLogging;
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public static int maxDebugLevels = 7;
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public static Dictionary<int, List<string>> RTLogList = new Dictionary<int, List<string>>();

        /// <summary>
        /// 
        /// </summary>
        static RTLog()
        {
            RTLog.verboseLogging = GameSettings.VERBOSE_DEBUG_LOG;

            #region ON-DEBUGMODE
#if DEBUG
            RTLog.verboseLogging = true;

            for (int i = 0; i < RTLog.maxDebugLevels; i++)
            {
                RTLog.RTLogList.Add(i, new List<string>());
            }
#endif
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="param"></param>
        /// <returns>formated string</returns>
        public static string formatMessage(string message, params object[] param)
        {
            return string.Format(message, param);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Notify(string message, int debugLvl = 0)
        {
            UnityEngine.Debug.Log("RemoteTech: " + message);

            #region ON-DEBUGMODE
#if DEBUG
            RTLog.NotifyToDebugLevel(message, debugLvl);
#endif
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="param"></param>
        public static void Notify(string message, params object[] param)
        {
            RTLog.Notify(RTLog.formatMessage(message, param));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Verbose(string message, int debugLvl = 0)
        {
            if (verboseLogging)
            {
                RTLog.Notify(message, debugLvl);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="param"></param>
        public static void Verbose(string message, int debugLvl, params object[] param)
        {
            RTLog.Verbose(RTLog.formatMessage(message, param), debugLvl);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="Debuglevel"></param>
        /// <param name="param"></param>
        public static void NotifyToDebugLevel(string message, int Debuglevel, params object[] param)
        {
            #region ON-DEBUGMODE
#if DEBUG
            RTLog.RTLogList[Debuglevel].Add(message);
#endif
            #endregion
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class RTLogExtenstions
    {
        public static string ToDebugString<T>(this List<T> list)
        {
            return "{" + string.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }
    }
}