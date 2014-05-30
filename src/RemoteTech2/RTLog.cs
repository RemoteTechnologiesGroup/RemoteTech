using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace RemoteTech
{
    public static class RTLog
    {
        [Conditional("DEBUG")]
        public static void Debug(String message)
        {
            UnityEngine.Debug.Log("RemoteTech: " + message);
        }

        [Conditional("DEBUG")]
        public static void Debug(String message, params System.Object[] param)
        {
            UnityEngine.Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        [Conditional("DEBUG")]
        public static void Debug(String message, params UnityEngine.Object[] param)
        {
            UnityEngine.Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        public static void Notify(String message)
        {
            UnityEngine.Debug.Log("RemoteTech: " + message);
        }

        public static void Notify(String message, params UnityEngine.Object[] param)
        {
            UnityEngine.Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        public static void Notify(String message, params System.Object[] param)
        {
            UnityEngine.Debug.Log(String.Format("RemoteTech: " + message, param));
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return "{" +
                   string.Join(",",
                               dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString())
                                         .ToArray()) + "}";
        }

        public static string ToDebugString<T>(this List<T> list)
        {
            return "{" + string.Join(",", list.Select(x => x.ToString()).ToArray()) + "}";
        }
    }
}