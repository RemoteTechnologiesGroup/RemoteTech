using System;
using UnityEngine;
using System.Reflection;
using Random = System.Random;

namespace RemoteTech {
    public enum WindowAlign {
        Floating,
        BottomRight,
        BottomLeft,
        TopRight,
        TopLeft,
    }

    internal interface IGUIWindow {
        void Show();
        void Hide();

    }

    public abstract class AbstractWindow : IGUIWindow {
        public static GUIStyle Frame = new GUIStyle(HighLogic.Skin.window);

        private readonly String mTitle;
        private readonly WindowAlign mAlign;
        private readonly int mWindowId;
        private readonly bool[] mUsedPointers;
        protected Rect mWindowPosition;

        static AbstractWindow() {
            Frame.padding = new RectOffset(5, 5, 5, 5);
        }

        public AbstractWindow(String title, Rect position, WindowAlign align) {
            mTitle = title;
            mAlign = align;
            mWindowPosition = position;
            mWindowId = (new Random()).Next();

            // Dirty reflection hack to remove EZGUI mouse events over the Unity GUI.
            // Not illegal, no decompilation necessary to obtain data.
            mUsedPointers = (bool[]) typeof(UIManager).GetField("usedPointers",
    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIManager.instance);
        }

        public virtual void Show() {
            RenderingManager.AddToPostDrawQueue(0, Draw);
            UIManager.instance.AddMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
        }

        public virtual void Hide() {
            RenderingManager.RemoveFromPostDrawQueue(0, Draw);
            UIManager.instance.RemoveMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
        }

        public virtual void Window(int uid) {
            if (mTitle != null) {
                if (GUI.Button(new Rect(mWindowPosition.width - 18, 2, 16, 16), "")) {
                    Hide();
                }
                GUI.DragWindow(new Rect(0, 0, 100000, 20));
            }
        }

        protected virtual void Draw() {
            switch (mAlign) {
                default:
                    break;
                case WindowAlign.BottomLeft:
                    mWindowPosition.x = 0;
                    mWindowPosition.y = Screen.height - mWindowPosition.height;
                    break;
                case WindowAlign.BottomRight:
                    mWindowPosition.x = Screen.width - mWindowPosition.width;
                    mWindowPosition.y = Screen.height - mWindowPosition.height;
                    break;
                case WindowAlign.TopLeft:
                    mWindowPosition.x = 0;
                    mWindowPosition.y = 0;
                    break;
                case WindowAlign.TopRight:
                    mWindowPosition.x = Screen.width - mWindowPosition.width;
                    mWindowPosition.y = 0;
                    break;
            }
            if (mTitle == null) {
                mWindowPosition = GUILayout.Window(mWindowId, mWindowPosition, Window, mTitle,
                    GUIStyle.none);
            } else {
                mWindowPosition = GUILayout.Window(mWindowId, mWindowPosition, Window, mTitle);
            }
        }

        private void EZGUIMouseTouchPtrListener(POINTER_INFO ptr) {
            if (mWindowPosition.Contains(new Vector2(ptr.devicePos.x, 
                                                     Screen.height - ptr.devicePos.y))) {
                mUsedPointers[0] = true;
            }
        }
    }
}
