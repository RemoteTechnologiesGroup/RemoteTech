using System;
using System.Linq;
using System.Text;
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
    }
}