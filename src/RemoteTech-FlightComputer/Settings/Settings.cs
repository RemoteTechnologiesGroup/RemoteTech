using RemoteTech.Common;
using RemoteTech.Common.Settings;


namespace RemoteTech.FlightComputer.Settings
{
    public class FlightComputerSettingsManager
    {
        public static EventVoid OnSettingsChanged = new EventVoid("OnSettingsChanged");
        public static EventVoid OnSettingsLoaded = new EventVoid("OnSettingsLoaded");
        public static EventVoid OnSettingsSaved = new EventVoid("OnSettingsSaved");

        private static FlightComputerSettings _instance;
        public static FlightComputerSettings Instance
        {
            get
            {
                //check if there's an already loaded instance
                if (_instance != null && _instance.SettingsLoaded)
                    return _instance;

                // otherwise load settings to get the instance
                Common.Settings.ISettings tmpInstance  = new FlightComputerSettings();
                return _instance = (FlightComputerSettings)tmpInstance.Load(tmpInstance);
            }
        }

        /// <summary>
        /// Replace the given settings with a new Settings object of the given setting preset, and save it
        /// </summary>
        public static void ReloadSettings(FlightComputerSettings previousSettings, string presetCfgUrl)
        {
            _instance = FlightComputerSettings.LoadPreset(previousSettings, presetCfgUrl);
            _instance.Save();
        }
    }

    [SettingsAttribute(DefaultSettingFileName = "RemoteTech_FlightComputer_default.cfg", NodeName = "RemoteTech_FlightComputer", SaveFileName = "RemoteTech_FlightComputer.cfg")]
    public class FlightComputerSettings : AbstractSettings
    {
        [Persistent]
        public int FCLeadTime;
        [Persistent]
        public bool FCOffAfterExecute;
        [Persistent]
        public bool ThrottleTimeWarp;
        [Persistent]
        public bool ThrottleZeroOnNoConnection;

        public FlightComputerSettings()
        {
            var attribute = (SettingsAttribute) System.Attribute.GetCustomAttribute(typeof(FlightComputerSettings), typeof(SettingsAttribute));
            if (attribute == null)
            {
                RTLog.Notify("FlightComputerSettings: No attribute could be found.");
                return;
            }

            DefaultSettingFileName = attribute.DefaultSettingFileName;
            NodeName = attribute.NodeName;
            SaveFileName = attribute.SaveFileName;
        }

        public static FlightComputerSettings LoadPreset(FlightComputerSettings previousSettings, string presetCfgUrl)
        {
            var newPreSetSettings = new FlightComputerSettings();
            var successLoadPreSet = false;

            // Exploit KSP's GameDatabase to find third-party mods' RemoteTechSetting node (from GameData/ExampleMod/RemoteTechSettings.cfg)
            var rtSettingCfGs = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            foreach (var rtSettingCfg in rtSettingCfGs)
            {
                if (!rtSettingCfg.url.Equals(presetCfgUrl))
                    continue;

                // Preserve important information of RT, such as the single ID
                var importantInfoNode = new ConfigNode();

                successLoadPreSet = ConfigNode.LoadObjectFromConfig(newPreSetSettings, rtSettingCfg.config);
                RTLog.Notify("Load the preset cfg into object with {0}: LOADED {1}", newPreSetSettings, successLoadPreSet ? "OK" : "FAIL");

                // Restore backups
                ConfigNode.LoadObjectFromConfig(newPreSetSettings, importantInfoNode);
                break;
            }

            return successLoadPreSet ? newPreSetSettings : previousSettings;
        }
    }
}
