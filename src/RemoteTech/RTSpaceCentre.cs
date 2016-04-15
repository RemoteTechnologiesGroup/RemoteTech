using RemoteTech.UI;
using System;
using System.Linq;
using UnityEngine;
using KSP.UI.Screens;

namespace RemoteTech
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class RTSpaceCentre : MonoBehaviour
    {
        /// <summary>Button for KSP Stock Toolbar</summary>
        public static ApplicationLauncherButton LauncherButton = null;
        /// <summary>OptionWindow</summary>
        private OptionWindow OptionWindow;
        /// <summary>Texture for the KSP Stock Toolbar-Button</summary>
        private Texture2D RtOptionBtn;

        /// <summary>
        /// Start method for RTSpaceCentre
        /// </summary>
        public void Start()
        {
            // create the option window
            this.OptionWindow = new OptionWindow();
            
            GameEvents.onLevelWasLoaded.Add(onLevelWasLoaded);
            RTSettings.OnSettingsChanged.Add(OnRTSettingsChanged);
            GameEvents.OnUpgradeableObjLevelChange.Add(OnUpgradeableObjLevelChange);
            RtOptionBtn = RTUtil.LoadImage("gitpagessat");
            RTSpaceCentre.LauncherButton = ApplicationLauncher.Instance.AddModApplication(this.OptionWindow.toggleWindow, null, null, null, null, null,
                                                                                    ApplicationLauncher.AppScenes.SPACECENTER,
                                                                                    this.RtOptionBtn);
        }

        /// <summary>
        /// Callback-Event when a Upgradeable object (TrackingStation) has changed
        /// </summary>
        private void OnUpgradeableObjLevelChange(Upgradeables.UpgradeableObject obj, int lvl)
        {
            if (obj.name.Equals("TrackingStation"))
            {
                RTLog.Verbose("OnUpgradeableObjLevelChange {0} - lvl: {1}", RTLogLevel.LVL4, obj.name, lvl);
                this.reloadUpgradableAntennas(lvl+1);
            }
        }

        /// <summary>
        /// Callback-Event when the RTSettings are changed
        /// </summary>
        private void OnRTSettingsChanged()
        {
            this.reloadUpgradableAntennas();
        }

        private void onLevelWasLoaded(GameScenes scene)
        {
            if (scene == GameScenes.SPACECENTER)
            {
                if (RTSettings.Instance.firstStart)
                {
                    // open here the option dialog for the first start
                    RTLog.Notify("First start of RemoteTech!");
                    this.OptionWindow.Show();
                    RTSettings.Instance.firstStart = false;
                }
            }
        }
        
        /// <summary>
        /// Loop all the ground stations to applie antenna upgrades
        /// </summary>
        /// <param name="techlvl">lvl to set the antennas range</param>
        private void reloadUpgradableAntennas(int techlvl = 0)
        {
            foreach ( var satellite in RTSettings.Instance.GroundStations)
            {
                satellite.reloadUpgradeableAntennas(techlvl);
            }
        }

        /// <summary>
        /// Unity onGUI Method to draw the OptionWindow
        /// </summary>
        public void OnGUI()
        {
            Action windows = delegate { };
            foreach (var window in AbstractWindow.Windows.Values)
            {
                windows += window.Draw;
            }
            windows.Invoke();
        }

        /// <summary>
        /// Unity OnDestroy Method to clean up
        /// </summary>
        public void OnDestroy()
        {
            RTSettings.OnSettingsChanged.Remove(OnRTSettingsChanged);
            GameEvents.onLevelWasLoaded.Remove(onLevelWasLoaded);
            GameEvents.OnUpgradeableObjLevelChange.Remove(OnUpgradeableObjLevelChange);

            this.OptionWindow.Hide();
            this.OptionWindow = null;   // deinit

            if (RTSpaceCentre.LauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(RTSpaceCentre.LauncherButton);
            }
        }
    }
}
