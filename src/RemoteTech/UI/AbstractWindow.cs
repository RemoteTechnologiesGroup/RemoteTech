using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech.UI
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
        protected bool mSavePosition = false;

        private double mLastTime;
        private double mTooltipTimer;
        private readonly Guid mGuid;
        public static Dictionary<Guid, AbstractWindow> Windows = new Dictionary<Guid, AbstractWindow>();
        /// <summary>The initial width of this window</summary>
        public float mInitialWidth;
        /// <summary>The initial height of this window</summary>
        public float mInitialHeight;
        /// <summary>Callback trigger for the change in the posistion</summary>
        public Action onPositionChanged = delegate { };
        public Rect backupPosition;
        /// <summary>todo</summary>
        protected bool mCloseButton = true;

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
            backupPosition = position;
            mInitialHeight = position.height + 15;
            mInitialWidth = position.width + 15;

            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
        }

        public Rect RequestPosition() { return Position; }

        public virtual void Show()
        {
            if (Enabled)
                return;

            if(mSavePosition)
            {
                onPositionChanged += storePosition;

                // read the saved position
                if (RTSettings.Instance.savedWindowPositions.ContainsKey(this.GetType().ToString()))
                {
                    Position = RTSettings.Instance.savedWindowPositions[this.GetType().ToString()];
                }
            }

            if (Windows.ContainsKey(mGuid))
            {
                Windows[mGuid].Hide();
            }
            Windows[mGuid] = this;
            Enabled = true;
        }

        private void OnHideUI()
        {
            Enabled = false;
        }

        private void OnShowUI()
        {
            Enabled = true;
        }

        public virtual void Hide()
        {
            removeWindowCtrlLock();
            Windows.Remove(mGuid);
            Enabled = false;
            if (mSavePosition)
            {
                onPositionChanged -= storePosition;
            }
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
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
            if (!Enabled) return;
            if (Event.current.type == EventType.Layout)
            {
                Position.width = 0;
                Position.height = 0;
            }

            Position = GUILayout.Window(mGuid.GetHashCode(), Position, WindowPre, Title, Title == null ? Frame : HighLogic.Skin.window);
            
            if (Title != null && this.mCloseButton)
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

                // Position of the window changed?
                if (!backupPosition.Equals(Position))
                {
                    // trigger the onPositionChanged callbacks
                    onPositionChanged.Invoke();
                    backupPosition = Position;
                }

                // Set ship control lock if one rt input is in focus
                if (GUI.GetNameOfFocusedControl().StartsWith("rt_"))
                {
                    setWindowCtrlLock();
                }
                else
                {
                    removeWindowCtrlLock();
                }
            }
        }

        private void storePosition()
        {
            RTSettings.Instance.savedWindowPositions.Remove(this.GetType().ToString());
            RTSettings.Instance.savedWindowPositions.Add(this.GetType().ToString(), Position);
        }

        /// <summary>
        /// Set a input lock to keep typing to this window
        /// </summary>
        public void setWindowCtrlLock()
        {
            // only if we are enabled and the controllock is not set
            if (Enabled && InputLockManager.GetControlLock("RTLockControlForWindows") == ControlTypes.None)
            {
                InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, "RTLockControlForWindows");
                InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS, "RTLockControlCamForWindows");
            }
        }


        /// <summary>
        /// Remove the input lock
        /// </summary>
        public void removeWindowCtrlLock()
        {
            // only if the controllock is set
            if (InputLockManager.GetControlLock("RTLockControlForWindows") != ControlTypes.None)
            {
                InputLockManager.RemoveControlLock("RTLockControlForWindows");
                InputLockManager.RemoveControlLock("RTLockControlCamForWindows");
            }
        }

        /// <summary>
        /// Toggle the window
        /// </summary>
        public void toggleWindow()
        {
            if (this.Enabled)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }
    }
}
