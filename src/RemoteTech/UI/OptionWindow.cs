using System;
using System.Linq;
using UnityEngine;


namespace RemoteTech.UI
{
    class OptionWindow : AbstractWindow
    {
        #region Member
        /// <summary>Defines the option window width</summary>
        const uint WINDOW_WIDTH = 400;
        /// <summary>Defines the option window height</summary>
        const uint WINDOW_HEIGHT = 300;

        /// <summary>Option menu items</summary>
        public enum OPTION_MENUS
        {
            Start = 0,
            WorldScale,
            AlternativeRules,
            VisualStyle,
            Miscellaneous
        }
        
        /// <summary>Small gray hint text color</summary>
        private GUIStyle mGuiHintText;
        /// <summary>Small white running text color</summary>
        private GUIStyle mGuiRunningText;
        /// <summary>Texture to represent the dish color</summary>
        private Texture2D mVSColorDish;
        /// <summary>Texture to represent the omni color</summary>
        private Texture2D mVSColorOmni;
        /// <summary>Texture to represent the active color</summary>
        private Texture2D mVSColorActive;
        /// <summary>Texture to represent the remote station color</summary>
        private Texture2D mVSColorRemoteStation;
        /// <summary>Toggles the color slider for the dish color</summary>
        private bool dishSlider = false;
        /// <summary>Toggles the color slider for the omni color</summary>
        private bool omniSlider = false;
        /// <summary>Toggles the color slider for the active color</summary>
        private bool activeSlider = false;
        /// <summary>Toggles the color slider for the remote station color</summary>
        private bool remoteStationSlider = false;
        /// <summary>HeadlineImage</summary>
        private Texture2D mTexHeadline;
        /// <summary>Positionvector for the content scroller</summary>
        private Vector2 mOptionScrollPosition;
        /// <summary>Reference to the RTSettings</summary>
        private Settings mSettings { get { return RTSettings.Instance; } }
        /// <summary>Current selected menu item</summary>
        private int mMenuValue;
        #endregion

        #region AbstractWindow-Definitions

        public OptionWindow()
            : base(new Guid("387AEB5A-D29C-485B-B96F-CA575E776940"), "RemoteTech " + RTUtil.Version + " Options",
                   new Rect(Screen.width / 2 - (OptionWindow.WINDOW_WIDTH / 2), Screen.height / 2 - (OptionWindow.WINDOW_HEIGHT / 2), OptionWindow.WINDOW_WIDTH, OptionWindow.WINDOW_HEIGHT), WindowAlign.Floating)
        {

            this.mMenuValue = (int)OPTION_MENUS.Start;
            this.mCloseButton = false;
            this.initalAssets();
        }

        public override void Hide()
        {
            RTSettings.Instance.Save();
            base.Hide();
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

                this.drawOptionMenu();
                this.drawOptionContent();
            }
            GUILayout.EndVertical();

            if(GUILayout.Button("Save"))
            {
                this.Hide();
            }
            
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
            this.mGuiHintText = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 11,
                normal = { textColor = XKCDColors.Grey }
            };

            this.mGuiRunningText = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            // initial Textures
            RTUtil.LoadImage(out this.mTexHeadline, "OptionHeadline.png");
            // Visual style colors
            this.loadColorTexture(out this.mVSColorDish, this.mSettings.DishConnectionColor);
            this.loadColorTexture(out this.mVSColorOmni, this.mSettings.OmniConnectionColor);
            this.loadColorTexture(out this.mVSColorActive, this.mSettings.ActiveConnectionColor);
            this.loadColorTexture(out this.mVSColorRemoteStation, this.mSettings.RemoteStationColorDot);
        }

        /// <summary>
        /// Draws the option menu
        /// </summary>
        private void drawOptionMenu()
        {
            GUILayout.BeginHorizontal();
            {
                // push the font size of buttons
                var pushFontsize = GUI.skin.button.fontSize;
                GUI.skin.button.fontSize = 11;

                foreach (OPTION_MENUS menu in Enum.GetValues(typeof(OPTION_MENUS)))
                {
                    RTUtil.FakeStateButton(new GUIContent(menu.ToString()), () => this.mMenuValue = (int)menu, this.mMenuValue, (int)menu, GUILayout.Height(16));
                }

                // pop the saved button size back
                GUI.skin.button.fontSize = pushFontsize;
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// todo
        /// </summary>
        private void drawOptionContent()
        {
            GUILayout.BeginHorizontal();
            {
                this.mOptionScrollPosition = GUILayout.BeginScrollView(this.mOptionScrollPosition);
                {
                    switch ((OPTION_MENUS)this.mMenuValue)
                    {
                        case OPTION_MENUS.WorldScale:
                            {
                                this.drawWorldScaleContent();
                                break;
                            }
                        case OPTION_MENUS.AlternativeRules:
                            {
                                this.drawAlternativeRulesContent();
                                break;
                            }
                        case OPTION_MENUS.VisualStyle:
                            {
                                this.drawVisualStyleContent();
                                break;
                            }
                        case OPTION_MENUS.Miscellaneous:
                            {
                                this.drawMiscellaneousContent();
                                break;
                            }
                        case OPTION_MENUS.Start:
                        default:
                            {
                                this.drawStartContent();
                                break;
                            }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the content of the Start section
        /// </summary>
        private void drawStartContent()
        {
            GUILayout.Space(10);
            GUILayout.Label("Use the small menu buttons above to navigate through the different options.", this.mGuiRunningText);

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(90);
                this.mSettings.RemoteTechEnabled = GUILayout.Toggle(this.mSettings.RemoteTechEnabled, (this.mSettings.RemoteTechEnabled) ? "RemoteTech enabled" : "RemoteTech disabled");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.Label("You need some help with RemoteTech?\nLook on our online manuell or if you can't find your answer, there are many users on our forum thread.(Browser opens on click)", this.mGuiRunningText);
            GUILayout.BeginHorizontal();
            {
                if(GUILayout.Button("Online manuell"))
                {
                    Application.OpenURL("http://remotetechnologiesgroup.github.io/RemoteTech/");
                }
                if(GUILayout.Button("KSP Forum"))
                {
                    Application.OpenURL("http://forum.kerbalspaceprogram.com/threads/83305");
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the content of the WorldScale section
        /// </summary>
        private void drawWorldScaleContent()
        {
            GUILayout.Label("Consumption Multiplier: (" + this.mSettings.ConsumptionMultiplier + ")", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("If set to a value other than 1, the power consumption of all antennas will be increased or decreased by this factor. Does not affect energy consumption for science transmissions.", this.mGuiHintText);
            this.mSettings.ConsumptionMultiplier = GUILayout.HorizontalSlider(this.mSettings.ConsumptionMultiplier, 0, 2);

            GUILayout.Label("Range Multiplier: (" + this.mSettings.RangeMultiplier + ")", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("If set to a value other than 1, the range of all antennas will be increased or decreased by this factor. Does not affect Mission Control range.", this.mGuiHintText);
            this.mSettings.RangeMultiplier = GUILayout.HorizontalSlider(this.mSettings.RangeMultiplier, 0, 2);
        }

        /// <summary>
        /// Draws the content of the AlternativeRules section
        /// </summary>
        private void drawAlternativeRulesContent()
        {
            GUILayout.Space(10);
            this.mSettings.EnableSignalDelay = GUILayout.Toggle(this.mSettings.EnableSignalDelay, (this.mSettings.EnableSignalDelay) ? "Signal delay enabled" : "Signal delay disabled");
            GUILayout.Label("If set, then all commands sent to RemoteTech-compatible probe cores will be delayed, depending on the distance to the probe and the SpeedOfLight. If unset, then all commands will be executed instantaneously, so long as there is a connection of any length between the probe and Mission Control.", this.mGuiHintText);

            GUILayout.Label("Range Model type", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("This setting controls how the game determines whether two antennas are in range of each other. Read more on our online manuell about the difference for each rule.", this.mGuiHintText);
            GUILayout.BeginHorizontal();
            {
                RTUtil.FakeStateButton(new GUIContent("Standard"), () => this.mSettings.RangeModelType = RangeModel.RangeModel.Standard, (int)this.mSettings.RangeModelType, (int)RangeModel.RangeModel.Standard, GUILayout.Height(20));
                RTUtil.FakeStateButton(new GUIContent("Root"), () => this.mSettings.RangeModelType = RangeModel.RangeModel.Additive, (int)this.mSettings.RangeModelType, (int)RangeModel.RangeModel.Additive, GUILayout.Height(20));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Multiple Antenna Multiplier : (" + this.mSettings.MultipleAntennaMultiplier + ")", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("This setting lets multiple omnidirectional antennas on the same craft act as a single, slightly larger antenna. The default value of 0.0 means that omni antennas do not boost each other; a value of 1.0 means that the effective range of the satellite equals the total range of all omni antennas on board. The effective range scales linearly between these two extremes. This option works with both the Standard and Root range models.", this.mGuiHintText);
            this.mSettings.MultipleAntennaMultiplier = GUILayout.HorizontalSlider((float)this.mSettings.MultipleAntennaMultiplier, 0, 1);
            if(this.mSettings.MultipleAntennaMultiplier <= 0.49)
            {
                this.mSettings.MultipleAntennaMultiplier = 0;
            }
            else
            {
                this.mSettings.MultipleAntennaMultiplier = 1;
            }
        }

        /// <summary>
        /// Draws the content of the VisualStyle section
        /// </summary>
        private void drawVisualStyleContent()
        {
            
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Dish Connection Color:", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
                if(GUILayout.Button(this.mVSColorDish, GUILayout.Width(18)))
                {
                    this.dishSlider = !this.dishSlider;
                }
            }
            GUILayout.EndHorizontal();

            if (this.dishSlider)
            {
                this.mSettings.DishConnectionColor = this.drawColorSlider(this.mSettings.DishConnectionColor);
                this.loadColorTexture(out this.mVSColorDish, this.mSettings.DishConnectionColor);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Omni Connection Color:", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
                if(GUILayout.Button(this.mVSColorOmni, GUILayout.Width(18)))
                {
                    this.omniSlider = !this.omniSlider;
                }
            }
            GUILayout.EndHorizontal();

            if (this.omniSlider)
            {
                this.mSettings.OmniConnectionColor = this.drawColorSlider(this.mSettings.OmniConnectionColor);
                this.loadColorTexture(out this.mVSColorOmni, this.mSettings.OmniConnectionColor);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Active Connection Color:", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
                if(GUILayout.Button(this.mVSColorActive, GUILayout.Width(18)))
                {
                    this.activeSlider = !this.activeSlider;
                }
            }
            GUILayout.EndHorizontal();

            if (this.activeSlider)
            {
                this.mSettings.ActiveConnectionColor = this.drawColorSlider(this.mSettings.ActiveConnectionColor);
                this.loadColorTexture(out this.mVSColorActive, this.mSettings.ActiveConnectionColor);
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Remote Station Color:", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
                if (GUILayout.Button(this.mVSColorRemoteStation, GUILayout.Width(18)))
                {
                    this.remoteStationSlider = !this.remoteStationSlider;
                }
            }
            GUILayout.EndHorizontal();

            if (this.remoteStationSlider)
            {
                this.mSettings.RemoteStationColorDot = this.drawColorSlider(this.mSettings.RemoteStationColorDot);
                this.loadColorTexture(out this.mVSColorRemoteStation, this.mSettings.RemoteStationColorDot);
            }

            GUILayout.Space(10);
            GUILayout.BeginScrollView(new Vector2(), false, false, GUILayout.Height(10));
            { }
            GUILayout.EndScrollView();
            GUILayout.Space(10);

            this.mSettings.HideGroundStationsBehindBody = GUILayout.Toggle(this.mSettings.HideGroundStationsBehindBody, (this.mSettings.HideGroundStationsBehindBody) ? "Ground stations always shown" : "Ground stations are hidden behind body");
            GUILayout.Label("If true, ground stations occulued by the body they’re on will not be displayed. This prevents ground stations on the other side of the planet being visible through the planet itself.", this.mGuiHintText);
        }

        /// <summary>
        /// Draws the content of the Miscellaneous section
        /// </summary>
        private void drawMiscellaneousContent()
        {
            GUILayout.Space(10);
            this.mSettings.ThrottleTimeWarp = GUILayout.Toggle(this.mSettings.ThrottleTimeWarp, (this.mSettings.ThrottleTimeWarp) ? "RemoteTech will throttle timewarp" : "RemoteTech will not throttle timewarp");
            GUILayout.Label("If set, the flight computer will automatically come out of time warp a few seconds before executing a queued command. If unset, the player is responsible for making sure the craft is not in time warp during scheduled actions.", this.mGuiHintText);

            this.mSettings.ThrottleZeroOnNoConnection = GUILayout.Toggle(this.mSettings.ThrottleZeroOnNoConnection, (this.mSettings.ThrottleZeroOnNoConnection) ? "Throttles to zero on no connection" : "Keeps the throttle on no connection");
            GUILayout.Label("If true, the flight computer cuts the thrust (not on boosters) if you have no connection to mission control.", this.mGuiHintText);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="colorTex"></param>
        private void loadColorTexture(out Texture2D tex, Color colorTex)
        {
            tex = new Texture2D(16, 16);
            tex.SetPixels32(Enumerable.Repeat((Color32)colorTex, 16 * 16).ToArray());
            tex.Apply();
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private Color drawColorSlider(Color value)
        {
            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                    GUILayout.Label("Red: ("+(int)(value.r * 255) +")", this.mGuiHintText, GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.25f));
                    value.r = GUILayout.HorizontalSlider(value.r, 0, 1);
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                    GUILayout.Label("Green: (" + (int)(value.g * 255) + ")", this.mGuiHintText, GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.25f));
                    value.g = GUILayout.HorizontalSlider(value.g, 0, 1);
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                    GUILayout.Label("Blue: (" + (int)(value.b * 255) + ")", this.mGuiHintText, GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.25f));
                    value.b = GUILayout.HorizontalSlider(value.b, 0, 1);
                GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            return value;
        }
    }
}