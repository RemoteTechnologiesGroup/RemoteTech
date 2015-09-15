using System;
using System.Collections.Generic;
using UnityEngine;


namespace RemoteTech.UI
{
    class OptionWindow : AbstractWindow
    {
        const uint WINDOW_WIDTH = 500;
        const uint WINDOW_HEIGHT = 400;

        /// <summary>Style set for each row on the target pop-up</summary>
        private GUIStyle mGuiTableRow;
        /// <summary>HeadlineImage</summary>
        private Texture2D mTexHeadline;
        /// <summary>todo</summary>
        private Vector2 mOptionScrollPosition;

        #region AbstractWindow-Definitions

        public OptionWindow()
            : base(new Guid("387AEB5A-D29C-485B-B96F-CA575E776940"), "RemoteTech " + RTUtil.Version + " options",
                   new Rect(Screen.width / 2 - (OptionWindow.WINDOW_WIDTH / 2), Screen.height / 2 - (OptionWindow.WINDOW_HEIGHT / 2), OptionWindow.WINDOW_WIDTH, OptionWindow.WINDOW_HEIGHT), WindowAlign.Floating)
        {
            this.mCloseButton = false;
            this.initalAssets();
        }

        #endregion

        #region Base-drawing
        /// <summary>
        /// Draws the content of the window
        /// </summary>
        public override void Window(int uid)
        {
            // push the current GUI.skin
            var pushSkin = GUI.skin;
            GUI.skin = HighLogic.Skin;

            GUILayout.BeginVertical(GUILayout.Width(OptionWindow.WINDOW_WIDTH), GUILayout.Height(OptionWindow.WINDOW_HEIGHT));
            {
                // Header image
                GUI.DrawTexture(new Rect(16, 25, OptionWindow.WINDOW_WIDTH - 14, 70), this.mTexHeadline);
                GUILayout.Space(70);

                GUILayout.BeginHorizontal(GUILayout.Height(15));
                {
                    GUILayout.Space(8);
                    GUILayout.Label("Options", this.mGuiTableRow);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    this.drawOptionContent();
                }
                GUILayout.EndHorizontal();

                // Draw
                var pushSkin4CloseBtn = GUI.skin;
                GUI.skin = null;

                RTUtil.Button("Save and Close", () => this.onClickSaveOptions());

                GUI.skin = pushSkin4CloseBtn;
            }
            GUILayout.EndVertical();

            base.Window(uid);

            // pop back the saved skin
            GUI.skin = pushSkin;
        }
        #endregion

        /// <summary>
        /// Initializes the styles and assets
        /// </summary>
        private void initalAssets()
        {
            // initial styles
            this.mGuiTableRow = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 13,
                normal = { textColor = XKCDColors.AppleGreen }
            };

            // initial Textures
            RTUtil.LoadImage(out this.mTexHeadline, "OptionHeadline.png");
        }

        /// <summary>
        /// todo
        /// </summary>
        private void drawOptionContent()
        {
            this.mOptionScrollPosition = GUILayout.BeginScrollView(this.mOptionScrollPosition);
            {
                GUILayout.Label("Options here");
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// todo
        /// </summary>
        private void onClickSaveOptions()
        {
            RTLog.Notify("OptionWindow.onClickSaveOptions!", RTLogLevel.LVL4);

            RTSettings.Instance.Save();
            this.Hide();
        }

    }
}