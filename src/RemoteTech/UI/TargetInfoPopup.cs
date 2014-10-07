using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class TargetInfoPopup : AbstractWindow
    {
        public static Guid Guid = new Guid("39fe8878-d894-4ded-befb-d6e070ddc2c5");
        public String mTargetName = "";
        public AbstractWindow mParentWindow;
        private const float WINDOW_WIDTH = 150;
        private const float WINDOW_HEIGHT = 45;

        public TargetInfoPopup(AbstractWindow parentWindow)
            : base(Guid, null, new Rect(100, 100, TargetInfoPopup.WINDOW_WIDTH, TargetInfoPopup.WINDOW_HEIGHT), WindowAlign.Floating)
        {
            this.mParentWindow = parentWindow;
            this.Position = this.mParentWindow.Position;
            this.Hide();
        }

        public void setTarget(String stargetName)
        {
            this.mTargetName = stargetName;
        }

        public override void Show()
        {
            // refresh the position
            this.Position = this.mParentWindow.Position;

            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
        }

        public override void Window(int uid)
        {
            GUI.skin = HighLogic.Skin;
            GUIStyle headline = new GUIStyle(HighLogic.Skin.window);
            headline.fontSize = 10;
            headline.alignment = TextAnchor.UpperLeft;

            // set the max dimension of this vertical to the main dimension set on the constructor
            GUILayout.BeginVertical(GUILayout.Width(TargetInfoPopup.WINDOW_WIDTH), GUILayout.Height(TargetInfoPopup.WINDOW_HEIGHT));
            {
                GUILayout.Label(this.mTargetName, headline);
            }
            GUILayout.EndVertical();
            base.Window(uid);
        }

        public void changePosition(float xValue, float yValue){

            //this.Position.x = this.mParentWindow.Position.x + this.mParentWindow.Position.width;
            // only change the x position if we are on the screen
            //if (this.Position.x + xValue + this.Position.width < Screen.width)
            //{
            //    this.Position.x += xValue;
            //}
            //else
            //{
            //    // otherwise set the x position to the parent - the popup width
            //    this.Position.x = this.mParentWindow.Position.x;
            //}
        }

        public void setYPositionToMouse()
        {
            this.setYPosition(this.mParentWindow.mMousePos.y);
        }

        public void setYPosition(float yPos)
        {
            this.Position.y = yPos;
        }

        public void setPositionToRight()
        {
            this.Position.x = this.mParentWindow.Position.width + 10;
        }
    }
}
