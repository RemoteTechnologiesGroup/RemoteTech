using System;
using RemoteTech.Common.UI;
using UnityEngine;

namespace RemoteTech.UI
{
    public class TargetInfoWindow : AbstractWindow
    {
        /// <summary>Initial Window width of the targetInfowWindow</summary>
        private const float WINDOW_WIDTH = 180;
        /// <summary>Initial Window width of the targetInfowWindow</summary>
        private const float WINDOW_HEIGHT = 10;
        public static Guid Guid = new Guid("c6ba7467-7ecd-dcc4-5861-46bcc25d5f45");

        /// <summary>The rearranged position based on the parent window or a fixed rect</summary>
        private Rect parentPos;
        /// <summary>Holds the parent window to always get the current position of it</summary>
        public AbstractWindow ParentWindow { get; set; }
        /// <summary>The alignment of this window</summary>
        private WindowAlign PopupAlignment { get; set; }
        /// <summary>Trigger to get the position from the parent window or not</summary>
        private bool FixPosition { get; set; }
        ////////////////////////////
        TargetInfoFragment tif;

        /// <summary>
        /// Base initial method
        /// </summary>
        /// <param name="alignment">Alignment of this window</param>
        private void Initial(WindowAlign alignment)
        {
            PopupAlignment = alignment;
            tif = new TargetInfoFragment();
            Hide();
        }

        /// <summary>
        /// Empty Constructor
        /// </summary>
        private TargetInfoWindow()
            : base(Guid, null, new Rect(100, 100, WINDOW_WIDTH, WINDOW_HEIGHT), WindowAlign.Floating)
        {
        }

        /// <summary>
        /// Initialize the TargetWindow with a parent AbstractWindow
        /// </summary>
        /// <param name="parentWindow">Parent window for rearrange the position</param>
        /// <param name="alignment">Alignment of the targetWindow</param>
        public TargetInfoWindow(AbstractWindow parentWindow, WindowAlign alignment): this()
        {
            ParentWindow = parentWindow;
            Initial(alignment);
            // we are not on a fixed position for this targetwindow
            FixPosition = false;
        }

        /// <summary>
        /// Initialize the TargetWindow with a fix position rect
        /// </summary>
        /// <param name="position">Position of the targetWindow</param>
        /// <param name="alignment">Alignment of the targetWindow</param>
        public TargetInfoWindow(Rect position, WindowAlign alignment): this()
        {
            parentPos = position;
            Initial(alignment);
            // we are on a fixed position for this targetwindow
            FixPosition = true;
        }

        /// <summary>
        /// Set a new target for this targetwindow
        /// </summary>
        /// <param name="target">Target from the antenna fragment</param>
        /// <param name="antenna">current antenna</param>
        public void SetTarget(AntennaFragment.Entry target,IAntenna antenna)
        {
            tif.SetTarget(target, antenna);
        }

        /// <summary>
        /// Open this window
        /// </summary>
        public override void Show()
        {
            CalculatePosition();
            base.Show();
        }

        /// <summary>
        /// Close this window
        /// </summary>
        public override void Hide()
        {
            if (tif != null) tif.Dispose();

            base.Hide();
        }

        /// <summary>
        /// Draw the base for this window with the targetFragment in it
        /// </summary>
        public override void Window(int uid)
        {
            Color pushColor = GUI.contentColor;
            GUI.skin = HighLogic.Skin;

            // set the max dimension of this vertical to the main dimension, set in the constructor
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH), GUILayout.Height(WINDOW_HEIGHT));
            {
                tif.Draw();
            }
            GUILayout.EndVertical();
            
            GUI.contentColor = pushColor;
            base.Window(uid);
        }

        /// <summary>
        /// Calculates the new position. This method is needed for the non fixed version
        /// of this window. To get the new position of the parentWindow if it was dragged.
        /// </summary>
        public void CalculatePosition()
        {
            if (!FixPosition)
            {
                // get the new position from the parent window
                parentPos = ParentWindow.Position;
                parentPos.width = ParentWindow.mInitialWidth;
                parentPos.height = ParentWindow.mInitialHeight;
            }

            // refresh the position of the targetWindow
            SetXPosition(parentPos.x);
            SetYPosition(parentPos.y);

            // auto rearrange the position of this window
            var aligmentSwitcher = PopupAlignment;

            if (aligmentSwitcher == WindowAlign.Floating)
            {
                // check if we are outside the screen than flip to the other side
                if (parentPos.x + parentPos.width + mInitialWidth < Screen.width)
                {
                    aligmentSwitcher = WindowAlign.TopRight;
                }
                else
                {
                    aligmentSwitcher = WindowAlign.TopLeft;
                }
            }

            // switch the current alignment
            switch (aligmentSwitcher)
            {
                case WindowAlign.TopRight:
                    {
                        SetXPosition(parentPos.x + parentPos.width);
                        SetYPosition(parentPos.y);
                        break;
                    }
                case WindowAlign.TopLeft:
                    {
                        SetXPosition(parentPos.x - mInitialWidth);
                        SetYPosition(parentPos.y);
                        break;
                    }
            }
        }
        
        /// <summary>
        /// Set the y position
        /// </summary>
        /// <param name="yPos">New absolute position on y</param>
        public void SetYPosition(float yPos)
        {
            Position.y = yPos;
        }

        /// <summary>
        /// Set the x position
        /// </summary>
        /// <param name="xPos">New absolute position on x</param>
        public void SetXPosition(float xPos)
        {
            Position.x = xPos;
        }
    }
}
