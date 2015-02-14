using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTech
{
    /// <summary>
    /// Different log levels to log messages for debugging.
    /// </summary>
    public enum RTLogLevel
    {
        LVL1,
        LVL2,
        LVL3,
        LVL4,
        API,
        Assembly
    };

    public static class RTLog
    {

        /// <summary>On true the verbose-Methods will notify their messages</summary>
        private static readonly bool verboseLogging;
        /// <summary>debug log list</summary>
        public static readonly Dictionary<RTLogLevel, List<string>> RTLogList = new Dictionary<RTLogLevel, List<string>>();

        static RTLog()
        {
            verboseLogging = GameSettings.VERBOSE_DEBUG_LOG;

            #region ON-DEBUGMODE
#if DEBUG
            // always set the verboseLogging to true on debug mode
            verboseLogging = true;

            // initialize debug list
            foreach (RTLogLevel lvl in Enum.GetValues(typeof(RTLogLevel)))
            {
                RTLogList.Add(lvl, new List<string>());
            }
#endif
            #endregion
        }
        
        /// <summary>
        /// Notify a message to the log. In debug mode the message will also be logged 
        /// to the <paramref name="logLevel"/> list.
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="logLevel">Loglevel for debugging</param>
        public static void Notify(string message, RTLogLevel logLevel = RTLogLevel.LVL1)
        {
            UnityEngine.Debug.Log("RemoteTech: " + message);

            #region ON-DEBUGMODE
#if DEBUG
            NotifyToLogLevel(message, logLevel);
#endif
            #endregion
        }

        /// <summary>
        /// Notify a message to the log. Replaces each format item on the <paramref name="message"/>
        /// with the text equivalent of a corresponding objects value from <paramref name="param"/>.
        /// </summary>
        /// <param name="message">Message to log with format items</param>
        /// <param name="param">objects to format</param>
        public static void Notify(string message, params object[] param)
        {
            Notify(string.Format(message, param));
        }

        /// <summary>
        /// Notify a message to the log. Replaces each format item on the <paramref name="message"/>
        /// with the text equivalent of a corresponding objects value from <paramref name="param"/>.
        /// In debug mode the message will also be logged to the <paramref name="logLevel"/> list.
        /// </summary>
        /// <param name="message">Message to log with format items</param>
        /// <param name="logLevel">Loglevel for debugging</param>
        /// <param name="param">objects to format</param>
        public static void Notify(string message, RTLogLevel logLevel = RTLogLevel.LVL1, params object[] param)
        {
            Notify(string.Format(message, param), logLevel);
        }

        /// <summary>
        /// Notify a message to the log only if the VERBOSE_DEBUG_LOG from the ksp settings.cfg
        /// is set to true. In debug mode the message will also be logged to the
        /// <paramref name="logLevel"/> list.
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="logLevel">Loglevel for debugging</param>
        public static void Verbose(string message, RTLogLevel logLevel = RTLogLevel.LVL1)
        {
            if (verboseLogging)
            {
                Notify(message, logLevel);
            }
        }

        /// <summary>
        /// Notify a message to the log only if the VERBOSE_DEBUG_LOG from the ksp settings.cfg
        /// is set to true. Replaces each format item on the <paramref name="message"/>
        /// with the text equivalent of a corresponding objects value from <paramref name="param"/>.
        /// In debug mode the message will also be logged to the <paramref name="logLevel"/> list.
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="logLevel">Loglevel for debugging</param>
        /// <param name="param">objects to format</param>
        public static void Verbose(string message, RTLogLevel logLevel = RTLogLevel.LVL1, params object[] param)
        {
            Verbose(string.Format(message, param), logLevel);
        }

        /// <summary>
        /// Logs the <paramref name="message"/> to the <paramref name="logLevel"/>
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="logLevel">Loglevel for debugging</param>
        private static void NotifyToLogLevel(string message, RTLogLevel logLevel)
        {
            RTLogList[logLevel].Add(message);
        }
    }

    public static class RTLogExtenstions
    {
        public static string ToDebugString<T>(this List<T> list)
        {
            return "{" + string.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }
    }
}