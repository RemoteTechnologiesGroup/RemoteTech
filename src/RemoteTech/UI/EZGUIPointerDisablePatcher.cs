using System;
using System.Reflection;
using UnityEngine;

namespace RemoteTech
{

    public class EZGUIPointerDisablePatcher : IDisposable
    {
        private static EZGUIPointerDisablePatcher mInstance;
        private Func<Rect> mDelegate;
        private UIManager mManager;
        private bool[] mUsedPointers;

        private EZGUIPointerDisablePatcher()
        {
            mManager = UIManager.instance;
            mUsedPointers = (bool[])typeof(UIManager).GetField("usedPointers",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIManager.instance);
            mManager.AddMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
        }

        public void Dispose()
        {
            mManager.RemoveMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
            mUsedPointers = null;
        }

        public static void Register(Func<Rect> del)
        {
            if (mInstance == null)
            {
                mInstance = new EZGUIPointerDisablePatcher();
            }
            mInstance.mDelegate += del;
        }

        public static void Unregister(Func<Rect> del)
        {
            if (mInstance != null && mInstance.mDelegate != null)
            {
                mInstance.mDelegate -= del;
            }
        }

        private void EZGUIMouseTouchPtrListener(POINTER_INFO ptr)
        {
            if (mDelegate == null || mDelegate.GetInvocationList().Length == 0)
            {
                Dispose();
                mInstance = null;
                return;
            }
            foreach (Func<Rect> d in mDelegate.GetInvocationList())
            {
                if (d.Invoke().Contains(new Vector2(ptr.devicePos.x, Screen.height - ptr.devicePos.y)))
                {
                    mUsedPointers[0] = true;
                }
            }
        }
    }
}