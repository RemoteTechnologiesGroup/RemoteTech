using System;
using System.Reflection;
using UnityEngine;

namespace RemoteTech {
    
    public class EZGUIPointerDisablePatcher {
        public delegate Rect EZGUIRequestRectDelegate();
        private static EZGUIPointerDisablePatcher mInstance;
        private EZGUIRequestRectDelegate mDelegate;
        private bool[] mUsedPointers;
        
        private EZGUIPointerDisablePatcher() {
            mUsedPointers = (bool[])typeof(UIManager).GetField("usedPointers",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIManager.instance);
            UIManager.instance.AddMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
        }

        public static void Register(EZGUIRequestRectDelegate del) {
            if (mInstance == null) {
                mInstance = new EZGUIPointerDisablePatcher();
            }
            mInstance.mDelegate += del;
        }

        public static void Unregister(EZGUIRequestRectDelegate del) {
            mInstance.mDelegate -= del;
            if (mInstance.mDelegate.GetInvocationList().Length == 0) {
                UIManager.instance.RemoveMouseTouchPtrListener(mInstance.EZGUIMouseTouchPtrListener);
                mInstance = null;
            }
        }

        private void EZGUIMouseTouchPtrListener(POINTER_INFO ptr) {
            foreach (EZGUIRequestRectDelegate d in mDelegate.GetInvocationList()) {
                if (d().Contains(new Vector2(ptr.devicePos.x, Screen.height - ptr.devicePos.y))) {
                    mUsedPointers[0] = true;
                }
            }
        }
    }
}