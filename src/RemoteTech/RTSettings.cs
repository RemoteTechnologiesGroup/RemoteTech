using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class RTSettings
    {
        public static EventVoid OnSettingsChanged = new EventVoid("OnSettingsChanged");
        public static EventVoid OnSettingsLoaded = new EventVoid("OnSettingsLoaded");
        public static EventVoid OnSettingsSaved = new EventVoid("OnSettingsSaved");

        private static Settings mInstance;
        public static Settings Instance
        {
            get
            {
                if (mInstance is Settings && mInstance.settingsLoaded)
                {
                    return mInstance;
                }
                else
                {
                    return mInstance = Settings.Load();
                }
            }
        }

        /// <summary>
        /// Replace the given settings with a new Settings object of the given setting preset, and save it
        /// </summary>
        public static void ReloadSettings(Settings previousSettings, string presetCFGUrl)
        {
            RTSettings.mInstance = Settings.LoadPreset(previousSettings, presetCFGUrl);
            RTSettings.mInstance.Save();
        }
    }

    public class Settings
    {
        //Global settings of the RemoteTech add-on, whose default values are to be read from Default_Settings.cfg
        [Persistent] public bool RemoteTechEnabled;
        [Persistent] public bool CommNetEnabled;
        [Persistent] public float ConsumptionMultiplier;
        [Persistent] public float RangeMultiplier;
        [Persistent] public float MissionControlRangeMultiplier;
        [Persistent] public String ActiveVesselGuid;
        [Persistent] public String NoTargetGuid;
        [Persistent] public float SpeedOfLight;
        [Persistent] public MapFilter MapFilter;
        [Persistent] public bool EnableSignalDelay;
        [Persistent] public RangeModel.RangeModel RangeModelType;
        [Persistent] public double MultipleAntennaMultiplier;
        [Persistent] public bool ThrottleTimeWarp;
        [Persistent] public bool ThrottleZeroOnNoConnection;
        [Persistent] public bool HideGroundStationsBehindBody;
        [Persistent] public bool ControlAntennaWithoutConnection;
        [Persistent] public bool UpgradeableMissionControlAntennas;
        [Persistent] public bool HideGroundStationsOnDistance;
        [Persistent] public bool ShowMouseOverInfoGroundStations;
        [Persistent] public bool AutoInsertKaCAlerts;
        [Persistent] public int FCLeadTime;
        [Persistent] public bool FCOffAfterExecute;
        [Persistent] public float DistanceToHideGroundStations;
        [Persistent] public Color DishConnectionColor;
        [Persistent] public Color OmniConnectionColor;
        [Persistent] public Color ActiveConnectionColor;
        [Persistent] public Color RemoteStationColorDot;
        [Persistent(collectionIndex = "STATION")] public List<MissionControlSatellite> GroundStations;
        [Persistent(collectionIndex = "PRESETS")] public List<String> PreSets;

        /// <summary>
        /// Trigger to force a reloading of the settings if a selected save is running.
        /// </summary>
        public bool settingsLoaded = false;
        public bool firstStart = false;

        /// <summary>
        /// Temp Variable for all the Window Positions for each instance.
        /// </summary>
        public Dictionary<String, Rect> savedWindowPositions = new Dictionary<String, Rect>();

        /// <summary>
        /// Returns the current RemoteTech_Settings of an existing save full path. The path will be empty
        /// if no save is loaded or the game is a training mission
        /// </summary>
        private static String SaveSettingFile
        {
            get
            {
                if (HighLogic.CurrentGame == null || RTUtil.IsGameScenario)
                {
                    return "";
                }

                return KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/RemoteTech_Settings.cfg";
            }
        }

        /// <summary>
        /// Returns the full path of the Default_Settings of the RemoteTech mod
        /// </summary>
        private static String DefaultSettingFile
        {
            get
            {
                return KSPUtil.ApplicationRootPath + "/GameData/RemoteTech/Default_Settings.cfg";
            }
        }

        /// <summary>
        /// Saves the current RTSettings object to the RemoteTech_Settings.cfg
        /// </summary>
        public void Save()
        {
            try
            {
                String settingsFile = Settings.SaveSettingFile;

                // only save the settings if the file name is not empty (i.e. not on loading screen or in training)
                if (!String.IsNullOrEmpty(settingsFile) && this.RemoteTechEnabled)
                {
                    ConfigNode details = new ConfigNode("RemoteTechSettings");
                    ConfigNode.CreateConfigFromObject(this, 0, details);
                    ConfigNode save = new ConfigNode();
                    save.AddNode(details);
                    save.Save(Settings.SaveSettingFile);

                    RTSettings.OnSettingsSaved.Fire();
                }
            }
            catch (Exception e) { RTLog.Notify("An error occurred while attempting to save: {0}", RTLogLevel.LVL1, e.Message); }
        }

        public static Settings Load()
        {
            // Create a new settings object from the stored default settings
            Settings settings = new Settings();
            ConfigNode defaultLoad = ConfigNode.Load(Settings.DefaultSettingFile);
            if(defaultLoad == null) // disable itself and write explanation to KSP's log
            {
                settings.RemoteTechEnabled = false;
                settings.CommNetEnabled = true;
                RTLog.Notify("RemoteTech is disabled because the default file '{0}' is not found", Settings.DefaultSettingFile);
                return settings;
                // the main impact of returning the settings whose values are not initialised is several RT components glitch
                // out (log-error spam) but the save settings will not affected due to this.RemoteTechEnabled check
            }
            else
            {
                defaultLoad = defaultLoad.GetNode("RemoteTechSettings"); // defaultLoad has root{...} so need to traverse downwards
                bool success = ConfigNode.LoadObjectFromConfig(settings, defaultLoad);
                RTLog.Notify("Load default settings into object with {0}: LOADED {1}", defaultLoad, success?"OK":"FAIL");
            }

            // Disable RemoteTech on Training missions
            if (RTUtil.IsGameScenario)
            {
                settings.RemoteTechEnabled = false;
                settings.CommNetEnabled = true;
            }

            // stop and return default settings if we are on the KSP loading screen OR in training scenarios
            if (string.IsNullOrEmpty(Settings.SaveSettingFile))
            {
                return settings;
            }

            // try to load from the save-settings.cfg
            ConfigNode load = ConfigNode.Load(Settings.SaveSettingFile);
            if (load == null)
            {
                // write the RT settings to the player's save folder
                settings.Save();
                settings.firstStart = true;
            }
            else
            {
                // old or new format?
                if (load.HasNode("RemoteTechSettings"))
                    load = load.GetNode("RemoteTechSettings");
                
                // replace the default settings with save-setting file
                bool success = ConfigNode.LoadObjectFromConfig(settings, load);
                RTLog.Notify("Found and load save settings into object with {0}: LOADED {1}", load, success ? "OK" : "FAIL");
            }

            SearchAndPreparePresets(settings); // find third-party mods' RemoteTech settings
            settings.settingsLoaded = true;
            RTSettings.OnSettingsLoaded.Fire();

            return settings;
        }

        private static void SearchAndPreparePresets(Settings settings)
        {
            /* Note:
             * Scenario 1: RemoteTech_Settings.cfg of a save is of Default_Settings.cfg
             *     RemoteTech_Settings.cfg contains the PreSets node. Thus, settings.PreSets already has the Default_Setting.cfg
             * Scenario 2: RemoteTech_Settings.cfg of a save is of ExampleMod/RemoteTechSettings.cfg
             *     RemoteTech_Settings.cfg may not contain the PreSets node. Thus, Default_Settings.cfg should be added to settings.PreSets
             */

            bool presetsLoaded = false;
            // Exploit KSP's GameDatabase to find third-party mods' RemoteTechSetting node (from GameData/ExampleMod/RemoteTechSettings.cfg)
            UrlDir.UrlConfig[] RTSettingCFGs = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            for (int i = 0; i < RTSettingCFGs.Length; i++)
            {
                UrlDir.UrlConfig RTSettingCFG = RTSettingCFGs[i];

                // only new settings i.e. skip existing presets
                if (!settings.PreSets.Contains(RTSettingCFG.url))
                {
                    settings.PreSets.Add(RTSettingCFG.url);
                    RTLog.Notify("Add a new RemoteTechSettings from {0}", RTSettingCFG.url);
                    presetsLoaded = true;
                }
            }

            if (presetsLoaded) // only if new RT settings are found and added to the save-setting's PreSets node
                settings.Save();
        }

        /// <summary>
        /// Load a preset config into the RemoteTech settings object.
        /// </summary>
        public static Settings LoadPreset(Settings previousSettings, string presetCFGUrl)
        {
            Settings NewPreSetSettings = new Settings();
            bool SuccessLoadPreSet = false;

            // Exploit KSP's GameDatabase to find third-party mods' RemoteTechSetting node (from GameData/ExampleMod/RemoteTechSettings.cfg)
            UrlDir.UrlConfig[] RTSettingCFGs = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            for(int i=0;i<RTSettingCFGs.Length;i++)
            {
                UrlDir.UrlConfig RTSettingCFG = RTSettingCFGs[i];

                if (RTSettingCFG.url.Equals(presetCFGUrl))
                {
                    // Preserve important information of RT, such as the single ID
                    ConfigNode importantInfoNode = new ConfigNode();
                    importantInfoNode.AddValue("MapFilter", previousSettings.MapFilter);
                    importantInfoNode.AddValue("ActiveVesselGuid", previousSettings.ActiveVesselGuid);
                    importantInfoNode.AddValue("NoTargetGuid", previousSettings.NoTargetGuid);
                    importantInfoNode.AddValue("RemoteTechEnabled", previousSettings.RemoteTechEnabled);

                    SuccessLoadPreSet = ConfigNode.LoadObjectFromConfig(NewPreSetSettings, RTSettingCFG.config);
                    RTLog.Notify("Load the preset cfg into object with {0}: LOADED {1}", NewPreSetSettings, SuccessLoadPreSet ? "OK" : "FAIL");

                    // Restore backups
                    ConfigNode.LoadObjectFromConfig(NewPreSetSettings, importantInfoNode);
                    break;
                }
            }

            return SuccessLoadPreSet?NewPreSetSettings: previousSettings;
        }

        /// Adds a new ground station to the list and returns a new guid id for
        /// a successfull new station otherwise a Guid.Empty will be returned.
        /// </summary>
        /// <param name="name">Name of the ground station</param>
        /// <param name="latitude">latitude position</param>
        /// <param name="longitude">longitude position</param>
        /// <param name="height">height from asl</param>
        /// <param name="body">Referencebody 1=Kerbin etc...</param>
        public Guid AddGroundStation(string name, double latitude, double longitude, double height, int body)
        {
            RTLog.Notify("Trying to add groundstation({0})", RTLogLevel.LVL1, name);

            MissionControlSatellite newGroundStation = new MissionControlSatellite();
            newGroundStation.SetDetails(name, latitude, longitude, height, body);

            // Already on the list?
            var foundsat = this.GroundStations.Where(ms => ms.GetDetails().Equals(newGroundStation.GetDetails())).FirstOrDefault();
            if (foundsat != null)
            {
                RTLog.Notify("Groundstation already exists!", RTLogLevel.LVL1);
                return Guid.Empty;
            }

            this.GroundStations.Add(newGroundStation);
            this.Save();

            return newGroundStation.mGuid;
        }

        /// <summary>
        /// Removes a ground station from the list by its unique <paramref name="stationid"/>.
        /// Returns true for a successfull removed station, otherwise false
        /// </summary>
        /// <param name="stationid">Unique ground station id</param>
        public bool RemoveGroundStation(Guid stationid)
        {
            RTLog.Notify("Trying to remove groundstation {0}", RTLogLevel.LVL1, stationid);

            for (int i = this.GroundStations.Count - 1; i >= 0; i--)
            {
                if (this.GroundStations[i].mGuid.Equals(stationid))
                {
                    RTLog.Notify("Removing {0} ", RTLogLevel.LVL1, GroundStations[i].GetName());
                    this.GroundStations.RemoveAt(i);
                    this.Save();
                    return true;
                }
            }

            RTLog.Notify("Cannot find station {0}", RTLogLevel.LVL1, stationid);
            return false;
        }
    }
}
