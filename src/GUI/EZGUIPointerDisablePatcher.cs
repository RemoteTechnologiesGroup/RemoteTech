using System;
using System.Reflection;
using UnityEngine;

namespace RemoteTech {
    
    public class EZGUIPointerDisablePatcher : IDisposable {
        public delegate Rect EZGUIRequestRectDelegate();
        private static EZGUIPointerDisablePatcher mInstance;
        private EZGUIRequestRectDelegate mDelegate;
        private UIManager mManager;
        private bool[] mUsedPointers;
        
        private EZGUIPointerDisablePatcher() {
            mManager = UIManager.instance;
            mUsedPointers = (bool[])typeof(UIManager).GetField("usedPointers",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIManager.instance);
            mManager.AddMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
        }

        public void Dispose() {
            mManager.RemoveMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
            mUsedPointers = null;
        }

        public static void Register(EZGUIRequestRectDelegate del) {
            if (mInstance != null && 
                    mInstance.mManager != UIManager.instance) {
                mInstance.Dispose();
                mInstance = null;
            }
            if (mInstance == null) {
                mInstance = new EZGUIPointerDisablePatcher();
            }
            mInstance.mDelegate += del;
        }

        public static void Unregister(EZGUIRequestRectDelegate del) {
            mInstance.mDelegate -= del;
            if (mInstance.mDelegate.GetInvocationList().Length == 0) {
                mInstance.Dispose();
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