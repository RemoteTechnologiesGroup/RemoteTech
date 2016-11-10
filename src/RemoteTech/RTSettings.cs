using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class RTSettings
    {
        public static EventVoid OnSettingsChanged = new EventVoid("OnSettingsChanged");
        public static EventVoid OnSettingsLoaded = new EventVoid("OnSettingsLoaded");
        public static EventVoid OnSettingsSaved = new EventVoid("OnSettingsSaved");

        private static Settings _instance;
        public static Settings Instance
        {
            get
            {
                //check if there's an already loaded instance
                if (_instance != null && _instance.SettingsLoaded)
                    return _instance;
                
                // otherwise load settings to get the instance
                return _instance = Settings.Load();
            }
        }

        /// <summary>
        /// Replace the given settings with a new Settings object of the given setting preset, and save it
        /// </summary>
        public static void ReloadSettings(Settings previousSettings, string presetCfgUrl)
        {
            _instance = Settings.LoadPreset(previousSettings, presetCfgUrl);
            _instance.Save();
        }
    }

    public class Settings
    {
        // Global settings of the RemoteTech add-on, whose default values are to be read from Default_Settings.cfg
        // Note: do not rename any of those fields here except if you change the name in the configuration file; be careful though: this will render all previous saves incompatible!!!
        [Persistent] public bool RemoteTechEnabled;
        [Persistent] public bool CommNetEnabled;
        [Persistent] public float ConsumptionMultiplier;
        [Persistent] public float RangeMultiplier;
        [Persistent] public float MissionControlRangeMultiplier;
        [Persistent] public string ActiveVesselGuid;
        [Persistent] public string NoTargetGuid;
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
        [Persistent(collectionIndex = "PRESETS")] public List<string> PreSets;

        public const string SaveFileName = "RemoteTech_Settings.cfg";
        public const string DefaultSettingFileName = "Default_Settings.cfg";

        /// <summary>Trigger to force a reloading of the settings if a selected save is running.</summary>
        public bool SettingsLoaded;

        /// <summary>True if its the first start of RemoteTech for this save, false otherwise.</summary>
        public bool FirstStart;

        /// <summary>Temp Variable for all the Window Positions for each instance.</summary>
        public Dictionary<string, Rect> SavedWindowPositions = new Dictionary<string, Rect>();

        /// <summary>
        /// Returns the current RemoteTech_Settings of an existing save full path. The path will be empty
        /// if no save is loaded or the game is a training mission
        /// </summary>
        private static string SaveSettingFile
        {
            get
            {
                if (HighLogic.CurrentGame == null || RTUtil.IsGameScenario)
                    return string.Empty;

                return KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + Path.DirectorySeparatorChar + SaveFileName;
            }
        }

        /// <summary>
        /// Returns the full path of the Default_Settings of the RemoteTech mod
        /// </summary>
        private static string DefaultSettingFile => KSPUtil.ApplicationRootPath + "/GameData/RemoteTech/" + DefaultSettingFileName;

        /// <summary>
        /// Saves the current RTSettings object to the RemoteTech_Settings.cfg
        /// </summary>
        public void Save()
        {
            try
            {
                // only save the settings if the file name is not empty (i.e. not on loading screen or in training)
                if (string.IsNullOrEmpty(SaveSettingFile))
                    return;

                var details = new ConfigNode("RemoteTechSettings");
                ConfigNode.CreateConfigFromObject(this, 0, details);
                var save = new ConfigNode();
                save.AddNode(details);
                save.Save(SaveSettingFile);

                RTSettings.OnSettingsSaved.Fire();
            }
            catch (Exception e)
            {
                RTLog.Notify("An error occurred while attempting to save: {0}", RTLogLevel.LVL1, e.Message);
            }
        }

        public static Settings Load()
        {
            // Create a new settings object from the stored default settings
            var settings = new Settings();
            var defaultLoad = ConfigNode.Load(DefaultSettingFile);
            if(defaultLoad == null) // disable itself and write explanation to KSP's log
            {
                RTLog.Notify("RemoteTech is disabled because the default file '{0}' is not found", DefaultSettingFile);
                return null;
                // the main impact of returning null is the endless loop of invoking Load() in the KSP's loading screen
            }
            defaultLoad = defaultLoad.GetNode("RemoteTechSettings"); // defaultLoad has root{...} so need to traverse downwards
            var success = ConfigNode.LoadObjectFromConfig(settings, defaultLoad);
            RTLog.Notify("Load default settings into object with {0}: LOADED {1}", defaultLoad, success ? "OK" : "FAIL");

            settings.SettingsLoaded = true;

            // Disable RemoteTech on Training missions
            if (RTUtil.IsGameScenario)
            {
                settings.RemoteTechEnabled = false;
                settings.CommNetEnabled = true;
            }

            // stop and return default settings if we are on the KSP loading screen OR in training scenarios
            if (string.IsNullOrEmpty(SaveSettingFile))
            {
                return settings;
            }

            // try to load from the save-settings.cfg
            var load = ConfigNode.Load(SaveSettingFile);
            if (load == null)
            {
                // write the RT settings to the player's save folder
                settings.Save();
                settings.FirstStart = true;
            }
            else
            {
                // old or new format?
                if (load.HasNode("RemoteTechSettings"))
                    load = load.GetNode("RemoteTechSettings");
                
                // replace the default settings with save-setting file
                success = ConfigNode.LoadObjectFromConfig(settings, load);
                RTLog.Notify("Found and load save settings into object with {0}: LOADED {1}", load, success ? "OK" : "FAIL");
            }

            // find third-party mods' RemoteTech settings
            SearchAndPreparePresets(settings); 
            
            RTSettings.OnSettingsLoaded.Fire();

            return settings;
        }

        private static void SearchAndPreparePresets(Settings settings)
        {
            var presetsChanged = false;

            // Exploit KSP's GameDatabase to find third-party mods' RemoteTechSetting node (from GameData/ExampleMod/RemoteTechSettings.cfg)
            var cfgs = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            var rtSettingCfGs = cfgs.Select(x => x.url).ToList();

            //check for any invalid preset in the settings of a save
            for (var i=0; i < settings.PreSets.Count(); i++)
            {
                if (rtSettingCfGs.Contains(settings.PreSets[i]))
                    continue;

                RTLog.Notify("Remove an invalid setting preset {0}", settings.PreSets[i]);
                settings.PreSets.RemoveAt(i);
                presetsChanged = true;
            }

            //find and add new presets to the settings of a save
            for (var i = 0; i < rtSettingCfGs.Count(); i++)
            {
                if (settings.PreSets.Contains(rtSettingCfGs[i]))
                    continue;

                RTLog.Notify("Add a new setting preset {0}", rtSettingCfGs[i]);
                settings.PreSets.Add(rtSettingCfGs[i]);
                presetsChanged = true;
            }

            if (presetsChanged) // only if new RT settings are found and added to the save-setting's PreSets node
                settings.Save();
        }

        /// <summary>
        /// Load a preset configuration into the RemoteTech settings object.
        /// </summary>
        public static Settings LoadPreset(Settings previousSettings, string presetCfgUrl)
        {
            var newPreSetSettings = new Settings();
            var successLoadPreSet = false;

            // Exploit KSP's GameDatabase to find third-party mods' RemoteTechSetting node (from GameData/ExampleMod/RemoteTechSettings.cfg)
            var rtSettingCfGs = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            for(var i = 0; i < rtSettingCfGs.Length; i++)
            {
                var rtSettingCfg = rtSettingCfGs[i];

                if (!rtSettingCfg.url.Equals(presetCfgUrl))
                    continue;

                // Preserve important information of RT, such as the single ID
                var importantInfoNode = new ConfigNode();
                importantInfoNode.AddValue("MapFilter", previousSettings.MapFilter);
                importantInfoNode.AddValue("ActiveVesselGuid", previousSettings.ActiveVesselGuid);
                importantInfoNode.AddValue("NoTargetGuid", previousSettings.NoTargetGuid);

                successLoadPreSet = ConfigNode.LoadObjectFromConfig(newPreSetSettings, rtSettingCfg.config);
                RTLog.Notify("Load the preset cfg into object with {0}: LOADED {1}", newPreSetSettings, successLoadPreSet ? "OK" : "FAIL");

                // Restore backups
                ConfigNode.LoadObjectFromConfig(newPreSetSettings, importantInfoNode);
                break;
            }

            return successLoadPreSet?newPreSetSettings: previousSettings;
        }

        /// <summary>
        /// Adds a new ground station to the list. 
        /// </summary>
        /// <param name="name">Name of the ground station</param>
        /// <param name="latitude">Latitude position</param>
        /// <param name="longitude">Longitude position</param>
        /// <param name="height">Height above sea level</param>
        /// <param name="body">Reference body 1=Kerbin etc...</param>
        /// <returns>A new <see cref="Guid"/> if a new station was successfully added otherwise a Guid.Empty.</returns>
        public Guid AddGroundStation(string name, double latitude, double longitude, double height, int body)
        {
            RTLog.Notify("Trying to add ground station({0})", RTLogLevel.LVL1, name);

            var newGroundStation = new MissionControlSatellite();
            newGroundStation.SetDetails(name, latitude, longitude, height, body);

            // Already on the list?
            var foundGroundStation = GroundStations.FirstOrDefault(ms => ms.GetDetails().Equals(newGroundStation.GetDetails()));
            if (foundGroundStation != null)
            {
                RTLog.Notify("Ground station already exists!");
                return Guid.Empty;
            }

            GroundStations.Add(newGroundStation);
            Save();

            return newGroundStation.mGuid;
        }

        /// <summary>Removes a ground station from the list by its unique <paramref name="stationid"/>.</summary>
        /// <param name="stationid">Unique ground station id</param>
        /// <returns>Returns true for a successful removed station, otherwise false.</returns>
        public bool RemoveGroundStation(Guid stationid)
        {
            RTLog.Notify("Trying to remove ground station {0}", RTLogLevel.LVL1, stationid);

            for (var i = 0; i < GroundStations.Count; i++)
            {
                if (!GroundStations[i].mGuid.Equals(stationid))
                    continue;

                RTLog.Notify("Removing {0} ", RTLogLevel.LVL1, GroundStations[i].GetName());
                GroundStations.RemoveAt(i);
                Save();
                return true;
            }

            RTLog.Notify("Cannot find station {0}", RTLogLevel.LVL1, stationid);
            return false;
        }
    }
}
