using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class TargetInfoWindow : AbstractWindow
    {
        /// <summary>Initial Window width of the targetInfowWindow</summary>
        private const float WINDOW_WIDTH = 180;
        /// <summary>Initial Window width of the targetInfowWindow</summary>
        private const float WINDOW_HEIGHT = 10;
        public static Guid Guid = new Guid("c6ba7467-7ecd-dcc4-5861-46bcc25d5f45");
        /// <summary>The reranged position based on the partne window or a fixed rect</summary>
        public Rect mParentPos;
        /// <summary>Holds the parent window to always get the current position of it</summary>
        public AbstractWindow mParentWindow;
        /// <summary>Thw allignment of this window</summary>
        private WindowAlign mPopupAlignment;
        /// <summary>Trigger to get the position from the parent window or not</summary>
        private bool mFixposition;
        ////////////////////////////
        TargetInfoFragment tif;

        /// <summary>
        /// Base initial method
        /// </summary>
        /// <param name="alignment">Aligment of this window</param>
        private void initial(WindowAlign alignment)
        {
            mPopupAlignment = alignment;
            tif = new TargetInfoFragment();
            Hide();
        }

        /// <summary>
        /// Empty Constructor
        /// </summary>
        private TargetInfoWindow()
            : base(Guid, null, new Rect(100, 100, TargetInfoWindow.WINDOW_WIDTH, TargetInfoWindow.WINDOW_HEIGHT), WindowAlign.Floating)
        {
        }

        /// <summary>
        /// Initialize the TargetWindow with a parent AbstractWindow
        /// </summary>
        /// <param name="parentWindow">Parent window for rerange the position</param>
        /// <param name="alignment">Alignment of the targetWindow</param>
        public TargetInfoWindow(AbstractWindow parentWindow, WindowAlign alignment): this()
        {
            mParentWindow = parentWindow;
            initial(alignment);
            // we are not on a fixed position for this targetwindow
            mFixposition = false;
        }

        /// <summary>
        /// Initialize the TargetWindow with a fix position rect
        /// </summary>
        /// <param name="position">Position of the targetWindow</param>
        /// <param name="alignment">Alignment of the targetWindow</param>
        public TargetInfoWindow(Rect position, WindowAlign alignment): this()
        {
            mParentPos = position;
            initial(alignment);
            // we are on a fixed position for this targetwindow
            mFixposition = true;
        }

        /// <summary>
        /// Set a new target for this targetwindow
        /// </summary>
        /// <param name="targetEntry">Target from the antenna fragment</param>
        /// <param name="antenna">current antenna</param>
        public void setTarget(AntennaFragment.Entry target,IAntenna antenna)
        {
            tif.setTarget(target, antenna);
        }

        /// <summary>
        /// Open this window
        /// </summary>
        public override void Show()
        {
            calculatePosition();
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
            GUILayout.BeginVertical(GUILayout.Width(TargetInfoWindow.WINDOW_WIDTH), GUILayout.Height(TargetInfoWindow.WINDOW_HEIGHT));
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
        public void calculatePosition()
        {
            if (!mFixposition)
            {
                // get the new position from the parent window
                mParentPos = mParentWindow.Position;
                mParentPos.width = mParentWindow.mInitialWidth;
                mParentPos.height = mParentWindow.mInitialHeight;
            }

            // refresh the position of the targetWindow
            setXPosition(mParentPos.x);
            setYPosition(mParentPos.y);

            // auto rerange the position of this window
            var aligmentSwitcher = mPopupAlignment;

            if (aligmentSwitcher == WindowAlign.Floating)
            {
                // check if we are outside the screen than flip to the other side
                if (mParentPos.x + mParentPos.width + mInitialWidth < Screen.width)
                {
                    aligmentSwitcher = WindowAlign.TopRight;
                }
                else
                {
                    aligmentSwitcher = WindowAlign.TopLeft;
                }
            }

            // switch the current aligment
            switch (aligmentSwitcher)
            {
                case WindowAlign.TopRight:
                    {
                        setXPosition(mParentPos.x + mParentPos.width);
                        setYPosition(mParentPos.y);
                        break;
                    }
                case WindowAlign.TopLeft:
                    {
                        this.setXPosition(mParentPos.x - mInitialWidth);
                        this.setYPosition(mParentPos.y);
                        break;
                    }
            }
        }
        
        /// <summary>
        /// Set the y position
        /// </summary>
        /// <param name="yPos">New absolut position on y</param>
        public void setYPosition(float yPos)
        {
            Position.y = yPos;
        }

        /// <summary>
        /// Set the x position
        /// </summary>
        /// <param name="xPos">New absolut position on x</param>
        public void setXPosition(float xPos)
        {
            Position.x = xPos;
        }
    }
}
