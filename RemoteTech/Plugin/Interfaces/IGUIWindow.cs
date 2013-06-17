using System;
using UnityEngine;
using Random = System.Random;

namespace RemoteTech {
    internal interface IGUIWindow {
        void Show();
        void Hide();
    }

    public abstract class AbstractWindow : IGUIWindow {
        public virtual void Show() {
            RenderingManager.AddToPostDrawQueue(0, Draw);
        }

        public virtual void Hide() {
            RenderingManager.RemoveFromPostDrawQueue(0, Draw);
        }

        private readonly String mTitle;
        private readonly int mWindowId;
        protected Rect mWindowPosition;

        public AbstractWindow(String title, Rect position) {
            mTitle = title;
            mWindowPosition = position;
            mWindowId = (new Random()).Next();
        }

        public virtual void Window(int uid) {
            if (GUI.Button(new Rect(mWindowPosition.width - 18, 2, 16, 16), "")) {
                Hide();
            }
            GUI.DragWindow(new Rect(0, 0, 100000, 20));
        }

        private void Draw() {
            mWindowPosition = GUILayout.Window(mWindowId, mWindowPosition, Window, mTitle);
        }
    }
}
