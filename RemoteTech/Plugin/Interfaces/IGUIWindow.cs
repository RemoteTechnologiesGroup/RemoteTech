using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    interface IGUIWindow {
        void Show();
        void Hide();
    }

    public abstract class AbstractGUIWindow : IGUIWindow {
        String mTitle;
        protected Rect mWindowPosition;
        int mWindowId;

        public AbstractGUIWindow(String title, Rect position) {
            mTitle = title;
            mWindowPosition = position;
            mWindowId = (new System.Random()).Next();
        }

        public virtual void Window(int uid) {
            if (GUI.Button(new Rect(mWindowPosition.x + mWindowPosition.width - 18,
mWindowPosition.y + 2, 16, 16), "")) {
                Hide();
            }
            GUI.DragWindow();
        }

        void Draw() {
            mWindowPosition = GUILayout.Window(mWindowId, mWindowPosition, Window, mTitle);

            
        }

        public virtual void Show() {
            RenderingManager.AddToPostDrawQueue(0, Draw);
        }

        public virtual void Hide() {
            RenderingManager.RemoveFromPostDrawQueue(0, Draw);
        }
    }
}
