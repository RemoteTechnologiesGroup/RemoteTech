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
        /// <summary>Button for KSP Stock Tool bar</summary>
        public static ApplicationLauncherButton LauncherButton = null;

        /// <summary>OptionWindow</summary>
        private OptionWindow _optionWindow;
        /// <summary>Texture for the KSP Stock Tool-bar Button</summary>
        private Texture2D _rtOptionBtn;

        /// <summary>
        /// Start method for RTSpaceCentre
        /// </summary>
        public void Start()
        {
            // create the option window
            _optionWindow = new OptionWindow();
            
            GameEvents.onLevelWasLoaded.Add(onLevelWasLoaded);
            GameEvents.OnUpgradeableObjLevelChange.Add(OnUpgradeableObjLevelChange);
            RTSettings.OnSettingsChanged.Add(OnRtSettingsChanged);

            _rtOptionBtn = RTUtil.LoadImage("gitpagessat");

            LauncherButton = ApplicationLauncher.Instance.AddModApplication(
                _optionWindow.toggleWindow, null, null, null, null, null,
                ApplicationLauncher.AppScenes.SPACECENTER,
                _rtOptionBtn);
        }

        /// <summary>
        /// Callback-Event when a Upgradeable object (TrackingStation) has changed
        /// </summary>
        private void OnUpgradeableObjLevelChange(Upgradeables.UpgradeableObject obj, int lvl)
        {
            if (!obj.name.Equals("TrackingStation"))
                return;

            RTLog.Verbose("OnUpgradeableObjLevelChange {0} - Level: {1}", RTLogLevel.LVL4, obj.name, lvl);
            ReloadUpgradableAntennas(lvl+1);
        }

        /// <summary>
        /// Callback-Event when the RTSettings are changed
        /// </summary>
        private void OnRtSettingsChanged()
        {
            ReloadUpgradableAntennas();
        }

        // Note: KSP's GameEvents.onLevelWasLoaded has the lower-case 'on' instead of usual 'On'
        private void onLevelWasLoaded(GameScenes scene)
        {
            if (scene != GameScenes.SPACECENTER)
                return;

            if (!RTSettings.Instance.FirstStart)
                return;

            // open here the option dialog for the first start
            RTLog.Notify("First start of RemoteTech!");
            _optionWindow.Show();
            RTSettings.Instance.FirstStart = false;
        }

        /// <summary>
        /// Apply antenna upgrades to all ground stations.
        /// </summary>
        /// <param name="techlvl">The level applied to the antennas range.</param>
        private void ReloadUpgradableAntennas(int techlvl = 0)
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
            var windowCount = AbstractWindow.Windows.Values.Count;
            for (var i = 0; i < windowCount; i++)
            {
                var window = AbstractWindow.Windows.Values.ElementAt(i);
                windows += window.Draw;
            }

            windows.Invoke();
        }

        /// <summary>
        /// Unity OnDestroy Method to clean up
        /// </summary>
        public void OnDestroy()
        {
            RTSettings.OnSettingsChanged.Remove(OnRtSettingsChanged);
            GameEvents.onLevelWasLoaded.Remove(onLevelWasLoaded);
            GameEvents.OnUpgradeableObjLevelChange.Remove(OnUpgradeableObjLevelChange);

            _optionWindow.Hide();

            _optionWindow = null;

            if (LauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(LauncherButton);
            }
        }
    }
}
