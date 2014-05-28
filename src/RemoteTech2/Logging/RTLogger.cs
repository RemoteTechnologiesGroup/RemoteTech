using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace RemoteTech
{
    public enum Severity
    {
        Off,
        Debug, 
        Info,
        Warning,
        Error,
        Fatal,
        All,
    }
    public static class RTLogger {

        public static ILogger CreateLogger(Type clazz)
        {
            return new Logger(clazz.Name);
        }
        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return "{" +
                   String.Join(",",
                               dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString())
                                         .ToArray()) + "}";
        }

        public static string ToDebugString<T>(this IList<T> list)
        {
            return "{" + String.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }
    }
    
    internal class Logger : ILogger {

        private String clazz;

        public Logger(String clazz)
        {
            this.clazz = clazz;
        }
        public void Fatal(String message, params object[] objects)
        {
            Log(Severity.Fatal, clazz, String.Format(message, objects));
        }

        public void Error(String message, params object[] objects)
        {
            Log(Severity.Error, clazz, String.Format(message, objects));
        }

        public void Warning(String message, params object[] objects)
        {
            Log(Severity.Warning, clazz, String.Format(message, objects));
        }

        public void Info(String message, params object[] objects)
        {
            Log(Severity.Info, clazz, String.Format(message, objects));
        }

        public void Debug(String message, params object[] objects)
        {
            Log(Severity.Debug, clazz, String.Format(message, objects));
        }

        private static void Log(Severity severity, String clazz, String message)
        {
            UnityEngine.Debug.Log("RemoteTech[" + severity.ToString() + "][" + clazz + "]: " + message);
        }
    }
}