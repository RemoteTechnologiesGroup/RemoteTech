using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace RemoteTech.UI
{
    class OptionWindow : AbstractWindow
    {
        #region Member
        /// <summary>Defines the option window width</summary>
        const uint WINDOW_WIDTH = 430;
        /// <summary>Defines the option window height</summary>
        const uint WINDOW_HEIGHT = 320;

        /// <summary>Option menu items</summary>
        public enum OPTION_MENUS
        {
            Start = 0,
            Presets,
            WorldScale,
            AlternativeRules,
            VisualStyle,
            Miscellaneous,
            Cheats
        }
        
        /// <summary>Small gray hint text color</summary>
        private GUIStyle mGuiHintText;
        /// <summary>Small white running text color</summary>
        private GUIStyle mGuiRunningText;
        /// <summary>Textstyle for list entrys</summary>
        private GUIStyle mGuiListText;
        /// <summary>Button style for list entrys</summary>
        private GUIStyle mGuiListButton;
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

            // Set the AppLauncherbutton to false
            if(RemoteTech.RTSpaceCentre.LauncherButton != null)
            {
                RemoteTech.RTSpaceCentre.LauncherButton.SetFalse();
            }

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

            if (GUILayout.Button("Close"))
            {
                this.Hide();
                RTSettings.OnSettingsChanged.Fire();
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
                fontSize = 13,
                normal = { textColor = Color.white }
            };

            this.mGuiListText = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12,
            };

            this.mGuiListButton = new GUIStyle(HighLogic.Skin.button)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            // initial Textures
            mTexHeadline = RTUtil.LoadImage("headline");
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
                        case OPTION_MENUS.Presets:
                            {
                                this.drawPresetsContent();
                                break;
                            }
                        case OPTION_MENUS.Cheats:
                            {
                                this.drawCheatContent();
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

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(90);
                this.mSettings.RemoteTechEnabled = GUILayout.Toggle(this.mSettings.RemoteTechEnabled, (this.mSettings.RemoteTechEnabled) ? "RemoteTech enabled" : "RemoteTech disabled");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(90);
                this.mSettings.CommNetEnabled = GUILayout.Toggle(this.mSettings.CommNetEnabled, (this.mSettings.CommNetEnabled) ? "CommNet enabled" : "CommNet disabled");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Need some help with RemoteTech?  Check out the online manual and tutorials.  If you can't find your answer, post in the forum thread.\n(Browser opens on click)", this.mGuiRunningText);
            GUILayout.BeginHorizontal();
            {
                if(GUILayout.Button("Online Manual and Tutorials"))
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
            GUILayout.Label("If set to a value other than 1, the power consumption of all antennas will be increased or decreased by this factor.\nDoes not affect energy consumption for science transmissions.", this.mGuiHintText);
            this.mSettings.ConsumptionMultiplier = (float)Math.Round(GUILayout.HorizontalSlider(this.mSettings.ConsumptionMultiplier, 0, 2), 2);

            GUILayout.Label("Antennas Range Multiplier: (" + this.mSettings.RangeMultiplier + ")", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("If set to a value other than 1, the range of all <b><color=#bada55>antennas</color></b> will be increased or decreased by this factor.\nDoes not affect Mission Control range.", this.mGuiHintText);
            mSettings.RangeMultiplier = (float)Math.Round(GUILayout.HorizontalSlider(mSettings.RangeMultiplier, 0, 5), 2);

            GUILayout.Label("Mission Control Range Multiplier: (" + mSettings.MissionControlRangeMultiplier + ")", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("If set to a value other than 1, the range of all <b><color=#bada55>Mission Controls</color></b> will be increased or decreased by this factor.\nDoes not affect antennas range.", this.mGuiHintText);
            mSettings.MissionControlRangeMultiplier = (float)Math.Round(GUILayout.HorizontalSlider(mSettings.MissionControlRangeMultiplier, 0, 5), 2);
        }

        /// <summary>
        /// Draws the content of the AlternativeRules section
        /// </summary>
        private void drawAlternativeRulesContent()
        {
            GUILayout.Space(10);
            this.mSettings.EnableSignalDelay = GUILayout.Toggle(this.mSettings.EnableSignalDelay, (this.mSettings.EnableSignalDelay) ? "Signal delay enabled" : "Signal delay disabled");
            GUILayout.Label("ON: All commands sent to RemoteTech-compatible probe cores are limited by the speed of light and have a delay before executing, based on distance.\nOFF: All commands will be executed immediately, although a working connection to Mission Control is still required.", this.mGuiHintText);

            GUILayout.Label("Range Model Mode", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("This setting controls how the game determines whether two antennas are in range of each other.\nRead more on our online manual about the difference for each rule.", this.mGuiHintText);
            GUILayout.BeginHorizontal();
            {
                RTUtil.FakeStateButton(new GUIContent("Standard"), () => this.mSettings.RangeModelType = RangeModel.RangeModel.Standard, (int)this.mSettings.RangeModelType, (int)RangeModel.RangeModel.Standard, GUILayout.Height(20));
                RTUtil.FakeStateButton(new GUIContent("Root"), () => this.mSettings.RangeModelType = RangeModel.RangeModel.Additive, (int)this.mSettings.RangeModelType, (int)RangeModel.RangeModel.Additive, GUILayout.Height(20));
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Multiple Antenna Multiplier : (" + this.mSettings.MultipleAntennaMultiplier + ")", GUILayout.Width(OptionWindow.WINDOW_WIDTH * 0.75f));
            GUILayout.Label("Multiple omnidirectional antennas on the same craft work together.\nThe default value of 0 means this is disabled.\nThe largest value of 1.0 sums the range of all omnidirectional antennas to provide a greater effective range.\nThe effective range scales linearly and this option works with both the Standard and Root range models.", this.mGuiHintText);
            this.mSettings.MultipleAntennaMultiplier = Math.Round(GUILayout.HorizontalSlider((float)mSettings.MultipleAntennaMultiplier, 0, 1), 2);
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

            this.mSettings.HideGroundStationsBehindBody = GUILayout.Toggle(this.mSettings.HideGroundStationsBehindBody, (this.mSettings.HideGroundStationsBehindBody) ? "Ground Stations are hidden behind bodies" : "Ground Stations always shown");
            GUILayout.Label("ON: Ground Stations are occluded by the planet or body, and are not visible behind it.\nOFF: Ground Stations are always shown (see range option below).", this.mGuiHintText);

            this.mSettings.HideGroundStationsOnDistance = GUILayout.Toggle(this.mSettings.HideGroundStationsOnDistance, (this.mSettings.HideGroundStationsOnDistance) ? "Ground Stations are hidden at a defined distance" : "Ground Stations always shown");
            GUILayout.Label("ON: Ground Stations will not be shown past a defined distance to the mapview camera.\nOFF: Ground Stations are shown regardless of distance.", this.mGuiHintText);

            this.mSettings.ShowMouseOverInfoGroundStations = GUILayout.Toggle(this.mSettings.ShowMouseOverInfoGroundStations, (this.mSettings.ShowMouseOverInfoGroundStations) ? "Mouseover of Ground Stations enabled" : "Mouseover of Ground Stations disabled");
            GUILayout.Label("ON: Some useful information is shown when you mouseover a Ground Station on the map view or Tracking Station.\nOFF: Information isn't shown during mouseover.", this.mGuiHintText);
        }

        /// <summary>
        /// Draws the content of the Miscellaneous section
        /// </summary>
        private void drawMiscellaneousContent()
        {
            GUILayout.Space(10);
            this.mSettings.ThrottleTimeWarp = GUILayout.Toggle(this.mSettings.ThrottleTimeWarp, (this.mSettings.ThrottleTimeWarp) ? "RemoteTech will throttle time warp" : "RemoteTech will not throttle time warp");
            GUILayout.Label("ON: The flight computer will automatically stop time warp a few seconds before executing a queued command.\nOFF: The player is responsible for controlling time warp during scheduled actions.", this.mGuiHintText);

            this.mSettings.ThrottleZeroOnNoConnection = GUILayout.Toggle(this.mSettings.ThrottleZeroOnNoConnection, (this.mSettings.ThrottleZeroOnNoConnection) ? "Throttle to zero on loss of connection" : "Throttle unaffected by loss of connection");
            GUILayout.Label("ON: The flight computer cuts the thrust if you lose connection to Mission Control.\nOFF: The throttle is not adjusted automatically.", this.mGuiHintText);

            this.mSettings.UpgradeableMissionControlAntennas = GUILayout.Toggle(this.mSettings.UpgradeableMissionControlAntennas, (this.mSettings.UpgradeableMissionControlAntennas) ? "Mission Control antennas are upgradeable": "Mission Control antennas are not upgradeable");
            GUILayout.Label("ON: Mission Control antenna range is upgraded when the Tracking Center is upgraded.\nOFF: Mission Control antenna range isn't upgradeable.", this.mGuiHintText);

            this.mSettings.AutoInsertKaCAlerts = GUILayout.Toggle(this.mSettings.AutoInsertKaCAlerts, (this.mSettings.AutoInsertKaCAlerts) ? "Alarms added to Kerbal Alarm Clock" : "No alarms added to Kerbal Alarm Clock");
            GUILayout.Label("ON: The flight computer will automatically add alarms to the Kerbal Alarm Clock mod for burn and maneuver commands.  The alarm goes off 3 minutes before the command executes.\nOFF: No alarms are added to Kerbal Alarm Clock", this.mGuiHintText);
        }

        /// <summary>
        /// Draws the content of the Presets section
        /// </summary>
        private void drawPresetsContent()
        {
            GUILayout.Label("A third-party mod can replace your current RemoteTech settings with its own settings (GameData/ExampleMod/RemoteTechSettings.cfg).\nAlso, you can revert to RemoteTech's default settings.\nHere you can see what presets are available:", this.mGuiRunningText);
            GUILayout.Space(15);

            List<String> presetList = this.mSettings.PreSets;

            if(this.mSettings.PreSets.Count <= 0)
            {
                GUILayout.Label("No presets are found", this.mGuiRunningText);
            }

            for(int i = presetList.Count - 1; i >= 0; --i)
            {
                GUILayout.BeginHorizontal("box", GUILayout.MaxHeight(15));
                {
                    string folderName = presetList[i];
                    int index = folderName.LastIndexOf("/RemoteTechSettings");
                    folderName = folderName.Substring(0, index) + folderName.Substring(index).Replace("/RemoteTechSettings", ".cfg").Trim();

                    GUILayout.Space(15);
                    GUILayout.Label("- " + folderName, this.mGuiListText, GUILayout.ExpandWidth(true));
                    if(GUILayout.Button("Overwrite", this.mGuiListButton, GUILayout.Width(70), GUILayout.Height(20)))
                    {
                        RTSettings.ReloadSettings(this.mSettings, presetList[i]);
                        ScreenMessages.PostScreenMessage(string.Format("Your RemoteTech settings are set to {0}", folderName), 15);
                        RTLog.Notify("Overwrote current settings with this cfg {0}", RTLogLevel.LVL3, presetList[i]);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Draws the content of the Cheat section
        /// </summary>
        private void drawCheatContent()
        {
            GUILayout.Space(10);
            this.mSettings.ControlAntennaWithoutConnection = GUILayout.Toggle(this.mSettings.ControlAntennaWithoutConnection, (this.mSettings.ControlAntennaWithoutConnection) ? "No Connection needed to control antennas" : "Connection is needed to control antennas");
            GUILayout.Label("ON: antennas can be activated, deactivated and targeted without a connection.\nOFF: No control without a working connection.", this.mGuiHintText);
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