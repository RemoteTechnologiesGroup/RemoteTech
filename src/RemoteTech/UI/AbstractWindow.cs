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
        public String Tooltip { get; set; }
        public bool Enabled = false;
        public static GUIStyle Frame = new GUIStyle(HighLogic.Skin.window);
        public const double TooltipDelay = 0.5;

        private double mLastTime;
        private double mTooltipTimer;
        private readonly Guid mGuid;
        public Vector2 mMousePos;
        public static Dictionary<Guid, AbstractWindow> Windows = new Dictionary<Guid, AbstractWindow>();
        public float mInitialWidth;
        public float mInitialHeight;
        public bool Shown;

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
            mInitialHeight = position.height + 15;
            mInitialWidth = position.width + 15;
        }

        public Rect RequestPosition() { return Position; }

        public virtual void Show()
        {
            if (Enabled)
                return;
            if (Windows.ContainsKey(mGuid))
            {
                Windows[mGuid].Hide();
            }
            Windows[mGuid] = this;
            Enabled = true;
        }

        public virtual void Hide()
        {
            Windows.Remove(mGuid);
            Enabled = false;
        }

        private void WindowPre(int uid)
        {
            Window(uid);
        }

        public virtual void Window(int uid)
        {
            if (Title != null)
            {
                GUI.DragWindow(new Rect(0, 0, Single.MaxValue, 20));
            }
            Tooltip = GUI.tooltip;
        }

        public virtual void Draw()
        {
            if (Event.current.type == EventType.Layout)
            {
                Position.width = 0;
                Position.height = 0;
            }

            this.mMousePos = Event.current.mousePosition;
            Position = GUILayout.Window(mGuid.GetHashCode(), Position, WindowPre, Title, Title == null ? Frame : HighLogic.Skin.window);
            if (Title != null)
            {
                if (GUI.Button(new Rect(Position.x + Position.width - 18, Position.y + 2, 16, 16), ""))
                {
                    Hide();
                }
            }
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
                if (Tooltip != "")
                {
                    if (mTooltipTimer > TooltipDelay)
                    {
                        var pop = GUI.skin.box.alignment;
                        var width = GUI.skin.box.CalcSize(new GUIContent(Tooltip)).x;
                        GUI.skin.box.alignment = TextAnchor.MiddleLeft;
                        GUI.Box(new Rect(Position.x, Position.y + Position.height + 10, width, 28), Tooltip);
                        GUI.skin.box.alignment = pop;
                    }
                    else
                    {
                        mTooltipTimer += Time.time - mLastTime;
                        mLastTime = Time.time;
                    }
                }
                else
                {
                    mTooltipTimer = 0.0;
                }
                mLastTime = Time.time;
            }
        }
    }
}
