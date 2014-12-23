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
        public RemoteTech.AntennaFragment.Entry mTarget;
        public Rect mParentPos;
        public AbstractWindow mParentWindow;
        private const float WINDOW_WIDTH = 180;
        private const float WINDOW_HEIGHT = 10;
        private WindowAlign mPopupAlignment;
        private bool fixposition;
        private KeyValuePair<string, Color> mTargetInfos;
        ////////////////////////////
        GUIStyle mGuiTableRow;
        GUIStyle mGuiHeadline;

        public void initial(WindowAlign alignment)
        {
            this.mPopupAlignment = alignment;
            this.Hide();

            this.mGuiTableRow = new GUIStyle(HighLogic.Skin.label);
            this.mGuiTableRow.fontSize = 12;
            this.mGuiTableRow.normal.textColor = UnityEngine.Color.white;

            this.mGuiHeadline = new GUIStyle(HighLogic.Skin.label);
            this.mGuiHeadline.fontSize = 13;
            this.mGuiHeadline.fontStyle = FontStyle.Bold;
        }

        public TargetInfoPopup(AbstractWindow parentWindow, WindowAlign alignment)
            : base(Guid, null, new Rect(100, 100, TargetInfoPopup.WINDOW_WIDTH, TargetInfoPopup.WINDOW_HEIGHT), WindowAlign.Floating)
        {
            this.mParentWindow = parentWindow;
            this.initial(alignment);
            this.fixposition = false;
        }

        public TargetInfoPopup(Rect position, WindowAlign alignment)
            : base(Guid, null, new Rect(100, 100, TargetInfoPopup.WINDOW_WIDTH, TargetInfoPopup.WINDOW_HEIGHT), WindowAlign.Floating)
        {
            this.mParentPos = position;
            this.initial(alignment);
            this.fixposition = true;
        }

        public void setTarget(RemoteTech.AntennaFragment.Entry target,IAntenna antenna)
        {
            this.mTarget = target;
            this.mTargetInfos = NetworkFeedback.tryConnection(antenna, target.Guid);
        }

        public override void Show()
        {
            if (!this.fixposition)
            {
                this.mParentPos = this.mParentWindow.Position;
                this.mParentPos.width = this.mParentWindow.mInitialWidth;
                this.mParentPos.height = this.mParentWindow.mInitialHeight;
            }
            // refresh the position
            this.setXPosition(this.mParentPos.x);
            this.setYPosition(this.mParentPos.y);

            if (this.mTargetInfos.Key.Trim().Length <= 0)
            {
                this.setXPosition(1000);
                this.setYPosition(1000);
            }

            var aligmentSwitcher = this.mPopupAlignment;
            if (this.mPopupAlignment == WindowAlign.Floating)
            {
                if (this.mParentPos.x + this.mParentPos.width + this.mInitialWidth < Screen.width)
                {
                    aligmentSwitcher = WindowAlign.TopRight;
                }
                else
                {
                    aligmentSwitcher = WindowAlign.TopLeft;
                }
            }

            switch (aligmentSwitcher)
            {
                case WindowAlign.TopRight:
                    {
                        this.setXPosition(this.mParentPos.x + this.mParentPos.width);
                        this.setYPosition(this.mParentPos.y);
                        break;
                    }
                case WindowAlign.TopLeft:
                    {
                        this.setXPosition(this.mParentPos.x - this.mInitialWidth);
                        this.setYPosition(this.mParentPos.y);
                        break;
                    }
            }

            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
        }

        public override void Window(int uid)
        {
            RTLog.Notify("Window: " + uid);
            Color pushColor = GUI.contentColor;
            GUI.skin = HighLogic.Skin;
            
            // set the max dimension of this vertical to the main dimension set on the constructor
            GUILayout.BeginVertical(GUILayout.Width(TargetInfoPopup.WINDOW_WIDTH), GUILayout.Height(TargetInfoPopup.WINDOW_HEIGHT));
            {
                GUILayout.Label(this.mTarget.Text, this.mGuiHeadline);

                var diagnostic = this.mTargetInfos.Key.Split(';');
                foreach (var diagnosticTextLines in diagnostic)
                {
                    try
                    {
                        GUILayout.BeginHorizontal();
                        if (diagnosticTextLines.Trim().Contains(':'))
                        {
                            var tableString = diagnosticTextLines.Trim().Split(':');
                            GUILayout.Label(tableString[0] + ':', this.mGuiTableRow, GUILayout.Width(110));
                            if (tableString[0].ToLower() == "status")
                            {
                                this.mGuiTableRow.normal.textColor = this.mTargetInfos.Value;
                            }

                            GUILayout.Label(tableString[1], this.mGuiTableRow);

                            this.mGuiTableRow.normal.textColor = UnityEngine.Color.white;
                        }
                        else
                        {
                            GUILayout.Label(diagnosticTextLines.Trim(), this.mGuiTableRow);
                        }
                        GUILayout.EndHorizontal();
                    }
                    catch(Exception){
                    }
                }
                GUI.contentColor = pushColor;
            }
            GUILayout.EndVertical();

            base.Window(uid);
        }
        
        public void setYPosition(float yPos)
        {
            this.Position.y = yPos;
        }

        public void setXPosition(float xPos)
        {
            this.Position.x = xPos;
        }
    }
}
