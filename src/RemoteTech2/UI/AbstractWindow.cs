using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public enum WindowAlign
    {
        Floating,
        BottomRight,
        BottomLeft,
        TopRight,
        TopLeft,
    }

    public abstract class AbstractWindow
    {
        public Rect Position;
        public String Title { get; set; }
        public WindowAlign Alignment { get; set; }
        public bool Enabled = false;
        public static GUIStyle Frame = new GUIStyle(HighLogic.Skin.window);
        
        private readonly Guid mGuid;
        private static Dictionary<Guid, AbstractWindow> mWindows = new Dictionary<Guid, AbstractWindow>();

        static AbstractWindow()
        {
            Frame.padding = new RectOffset(5, 5, 5, 5);
        }

        public AbstractWindow(Guid id, String title, Rect position, WindowAlign align)
        {
            mGuid = id;
            Title = title;
            Alignment = align;
            Position = position;
        }

        public Rect RequestPosition() { return Position; }

        public virtual void Show()
        {
            if (Enabled)
                return;
            if (mWindows.ContainsKey(mGuid))
            {
                mWindows[mGuid].Hide();
            }
            mWindows[mGuid] = this;
            Enabled = true;
            RenderingManager.AddToPostDrawQueue(0, Draw);
            EZGUIPointerDisablePatcher.Register(RequestPosition);
        }

        public virtual void Hide()
        {
            mWindows.Remove(mGuid);
            Enabled = false;
            RenderingManager.RemoveFromPostDrawQueue(0, Draw);
            EZGUIPointerDisablePatcher.Unregister(RequestPosition);
        }

        public virtual void Window(int uid)
        {
            if (Title != null)
            {
                GUI.DragWindow(new Rect(0, 0, 100000, 20));
            }
            if (Event.current.isMouse && Position.ContainsMouse())
            {
                Event.current.Use();
            }
        }

        protected virtual void Draw()
        {
            if (Event.current.type == EventType.Layout)
            {
                Position.width = 0;
                Position.height = 0;
            }
            Position = GUILayout.Window(mGuid.GetHashCode(), Position, Window, Title, Title == null ? Frame : HighLogic.Skin.window);
            if (Event.current.type == EventType.Repaint)
            {
                switch (Alignment)
                {
                    case WindowAlign.BottomLeft:
                        Position.x = 0;
                        Position.y = Screen.height - Position.height;
                        break;
                    case WindowAlign.BottomRight:
                        Position.x = Screen.width - Position.width;
                        Position.y = Screen.height - Position.height;
                        break;
                    case WindowAlign.TopLeft:
                        Position.x = 0;
                        Position.y = 0;
                        break;
                    case WindowAlign.TopRight:
                        Position.x = Screen.width - Position.width;
                        Position.y = 0;
                        break;
                }
            }
        }
    }
}
