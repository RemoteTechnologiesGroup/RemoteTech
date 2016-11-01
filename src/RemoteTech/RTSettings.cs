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
        /// Saves the Settings, kills the current instance and creates a new instance.
        /// </summary>
        /// <returns>Reloaded Settings</returns>
        public static Settings ReloadSettings()
        {
            if (RTSettings.mInstance != null)
            {
                RTSettings.mInstance.Save();
            }

            RTSettings.mInstance = null;
            return RTSettings.Instance;
        }
    }

    public class Settings
    {
        [Persistent] public bool RemoteTechEnabled = true;
        [Persistent] public bool CommNetEnabled = false;
        [Persistent] public float ConsumptionMultiplier = 1.0f;
        [Persistent] public float RangeMultiplier = 1.0f;
        [Persistent] public float MissionControlRangeMultiplier = 1.0f;
        [Persistent] public String ActiveVesselGuid = "35b89a0d664c43c6bec8d0840afc97b2";
        [Persistent] public String NoTargetGuid = Guid.Empty.ToString();
        [Persistent] public float SpeedOfLight = 3e8f;
        [Persistent] public MapFilter MapFilter = MapFilter.Path | MapFilter.Omni | MapFilter.Dish;
        [Persistent] public bool EnableSignalDelay = true;
        [Persistent] public RangeModel.RangeModel RangeModelType = RangeModel.RangeModel.Standard;
        [Persistent] public double MultipleAntennaMultiplier = 0.0;
        [Persistent] public bool ThrottleTimeWarp = true;
        [Persistent] public bool ThrottleZeroOnNoConnection = true;
        [Persistent] public bool HideGroundStationsBehindBody = true;
        [Persistent] public bool ControlAntennaWithoutConnection = false;
        [Persistent] public bool UpgradeableMissionControlAntennas = true;
        [Persistent] public bool HideGroundStationsOnDistance = true;
        [Persistent] public bool ShowMouseOverInfoGroundStations = true;
        [Persistent] public bool AutoInsertKaCAlerts = true;
        [Persistent] public int FCLeadTime = 180;
        [Persistent] public bool FCOffAfterExecute = false;
        [Persistent] public float DistanceToHideGroundStations = 3e7f;
        [Persistent] public Color DishConnectionColor = XKCDColors.Amber;
        [Persistent] public Color OmniConnectionColor = XKCDColors.BrownGrey;
        [Persistent] public Color ActiveConnectionColor = XKCDColors.ElectricLime;
        [Persistent] public Color RemoteStationColorDot = new Color(0.996078f, 0, 0, 1);
        [Persistent(collectionIndex = "STATION")]
        public List<MissionControlSatellite> GroundStations = new List<MissionControlSatellite>() { new MissionControlSatellite() };
        [Persistent(collectionIndex = "PRESETS")]
        public List<String> PreSets = new List<String>();

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
        /// Backup config node
        /// </summary>
        private ConfigNode backupNode;

        /// <summary>
        /// Returns the current RemoteTech_Settings full path. The path will be empty
        /// if no save is loaded or the game is a training mission
        /// </summary>
        private static String File
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
        /// Saves the current RTSettings object to the RemoteTech_Settings.cfg
        /// </summary>
        public void Save()
        {
            try
            {
                String settingsFile = Settings.File;

                // only save the settings if the file name is not empty (=not loading screen or training)
                if (!String.IsNullOrEmpty(settingsFile))
                {
                    ConfigNode details = new ConfigNode("RemoteTechSettings");
                    ConfigNode.CreateConfigFromObject(this, 0, details);
                    ConfigNode save = new ConfigNode();
                    save.AddNode(details);
                    save.Save(Settings.File);

                    RTSettings.OnSettingsSaved.Fire();
                }
            }
            catch (Exception e) { RTLog.Notify("An error occurred while attempting to save: {0}", RTLogLevel.LVL1, e.Message); }
        }

        /// <summary>
        /// Stores the MapFilter, ActiveVesselGuid, NoTargetGuid and RemoteTechEnabled Value for overriding
        /// with third party settings
        /// </summary>
        public void backupFields()
        {
            backupNode = new ConfigNode();
            backupNode.AddValue("MapFilter", MapFilter);
            backupNode.AddValue("ActiveVesselGuid", ActiveVesselGuid);
            backupNode.AddValue("NoTargetGuid", NoTargetGuid);
            backupNode.AddValue("RemoteTechEnabled", RemoteTechEnabled);
        }

        /// <summary>
        /// Restores the backuped values from backupFields()
        /// </summary>
        public void restoreBackups()
        {
            if (backupNode != null)
            {
                // restore backups
                ConfigNode.LoadObjectFromConfig(this, backupNode);
            }
        }

        public static Settings Load()
        {
            // Create a new settings object
            Settings settings = new Settings();

            // Disable RemoteTech on Training missions
            if (RTUtil.IsGameScenario)
            {
                settings.RemoteTechEnabled = false;
            }

            // skip loading if we are on the loading screen
            // and return the default object and also for
            // scenario games.
            if (string.IsNullOrEmpty(Settings.File))
            {
                return settings;
            }

            settings.settingsLoaded = true;

            // try to load from the base settings.cfg
            ConfigNode load = ConfigNode.Load(Settings.File);

            if (load == null)
            {
                // write new base file to the rt folder
                settings.Save();
                settings.firstStart = true;
            }
            else
            {
                // old or new format?
                if (load.HasNode("RemoteTechSettings"))
                {
                    load = load.GetNode("RemoteTechSettings");
                }
                RTLog.Notify("Load base settings into object with {0}", load);
                // load basic file
                ConfigNode.LoadObjectFromConfig(settings, load);
            }

            bool presetsLoaded = false;
            // Prefer to load from GameDatabase, to allow easier user customization
            UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            // find the default_settings from remote tech to load as the first settings
            UrlDir.UrlConfig defConfig = Array.Find(configList, cl => cl.url.Equals("RemoteTech/Default_Settings/RemoteTechSettings") && !settings.PreSets.Contains(cl.url));
            if(defConfig != null)
            {
                RTLog.Notify("Load default remotetech settings", RTLogLevel.LVL1);
                settings = Settings.LoadPreset(settings, defConfig);
            }

            foreach (UrlDir.UrlConfig curSet in configList)
            {
                // only third party files
                if (!curSet.url.Equals("RemoteTech/RemoteTech_Settings/RemoteTechSettings") && !settings.PreSets.Contains(curSet.url))
                {
                    settings = Settings.LoadPreset(settings, curSet);
                    // trigger to save the settings again
                    presetsLoaded = true;
                }
            }

            if (presetsLoaded)
            {
                settings.Save();
            }
            RTSettings.OnSettingsLoaded.Fire();

            return settings;
        }

        /// <summary>
        /// Load a preset config into the remotetech settings object.
        /// </summary>
        private static Settings LoadPreset(Settings settings, UrlDir.UrlConfig curSet)
        {
            settings.PreSets.Add(curSet.url);
            RTLog.Notify("Override RTSettings with configs from {0}", curSet.url);
            settings.backupFields();
            ConfigNode.LoadObjectFromConfig(settings, curSet.config);
            settings.restoreBackups();

            return settings;
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

    public class RemoteTechGeneralSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "RemoteTech General Options";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;
        public override string Section => "RemoteTech";
        public override int SectionOrder => 1;

        // not a setting per se
        [GameParameters.CustomStringParameterUI("RemoteTech Version", autoPersistance = false)]
        public string RemoteTechVersion = $"<b><color=#928ccc>{RTUtil.Version}</color></b>";

        [GameParameters.CustomParameterUI("RemoteTech Enabled", autoPersistance = true, toolTip = "<b><color=#14e356>ON</color></b>: RemoteTech is enabled on this save.\n<b><color=#e51a03>OFF</color></b>: none of the RemoteTech features are enabled.")]
        public bool RemoteTechEnabled = true;

        // not a setting per se ; just a reminder
        [GameParameters.CustomStringParameterUI("RemoteTech enabled", autoPersistance = false)]
        public string RemoteTechEnabledTip = "If RemoteTech is disabled, the default CommNet behavior will be used.";

    }

    public class RemoteTechWorldScaleSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "RemoteTech World Scale Options";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;
        public override string Section => "RemoteTech";
        public override int SectionOrder => 2;

        [GameParameters.CustomFloatParameterUI("Consumption Multiplier", autoPersistance = true, 
            toolTip = "If set to a value other than 1, the power consumption of all antennas will be increased or decreased by this factor.\nDoes not affect energy consumption for science transmissions.",
            minValue = 0f, maxValue = 2f, displayFormat = "F2", stepCount = 400)]
        public float ConsumptionMultiplier = 1;

        [GameParameters.CustomFloatParameterUI("Antennas Range Multiplier", autoPersistance = true,
            toolTip = "If set to a value other than 1, the range of all <b><color=#dd4949>antennas</color></b> will be increased or decreased by this factor.\nDoes not affect Mission Control range.",
            minValue = 0f, maxValue = 5f, displayFormat = "F2")]
        public float RangeMultiplier = 1;

        [GameParameters.CustomFloatParameterUI("Mission Control Range Multiplier", autoPersistance = true,
            toolTip = "If set to a value other than 1, the range of all <b><color=#dd4949>Mission Controls</color></b> will be increased or decreased by this factor.\nDoes not affect antennas range.",
            minValue = 0f, maxValue = 5f, displayFormat = "F2")]
        public float MissionControlRangeMultiplier = 1;
    }

    public class RemoteTechAlternativeRulesSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "RemoteTech Alternative Rules Options";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;
        public override string Section => "RemoteTech";
        public override int SectionOrder => 3;

        [GameParameters.CustomParameterUI("Signal Delay", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: All commands sent to RemoteTech-compatible probe cores are limited by the speed of light and have a delay before executing, based on distance.\n<color=#e51a03>OFF</color></b>: All commands will be executed immediately, although a working connection to Mission Control is still required.")]
        public bool EnableSignalDelay = true;

        [GameParameters.CustomParameterUI("Range Model Mode", toolTip = "This setting controls how the game determines whether two antennas are in range of each other.\nRead more on our online manual about the difference for each rule.")]
        public RangeModel.RangeModel RangeModelType = RangeModel.RangeModel.Standard;

        [GameParameters.CustomFloatParameterUI("Multiple Antenna Multiplier", autoPersistance = true,
            toolTip = "Multiple omnidirectional antennas on the same craft work together.\nThe default value of 0 means this is disabled.\nThe largest value of 1.0 sums the range of all omnidirectional antennas to provide a greater effective range.\nThe effective range scales linearly and this option works with both the Standard and Root range models.",
            minValue = 0f, maxValue = 1f, displayFormat = "F2")]
        public float MultipleAntennaMultiplier = 0;
    }

    public class RemoteTechVisualContentSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "RemoteTech Visual Content Options";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;
        public override string Section => "RemoteTech";
        public override int SectionOrder => 4;

        [GameParameters.CustomParameterUI("Hide Ground Stations behind body", autoPersistance = true, 
            toolTip = "<color=#14e356>ON</color></b>: Ground Stations are occluded by the planet or body, and are not visible behind it.\n<color=#e51a03>OFF</color></b>: Ground Stations are always shown (see 'Hide Ground Stations on distance' below).")]
        public bool HideGroundStationsBehindBody = true;

        [GameParameters.CustomParameterUI("Hide Ground Stations on distance", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: Ground Stations will not be shown past a defined distance to the mapview camera.\n<color=#e51a03>OFF</color></b>: Ground Stations are shown regardless of distance.")]
        public bool HideGroundStationsOnDistance = true;

        [GameParameters.CustomParameterUI("Show info on Ground Station mouse over", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: Some useful information is shown when you mouseover a Ground Station on the map view or Tracking Station.\n<color=#e51a03>OFF</color></b>: Information isn't shown during mouseover.")]
        public bool ShowMouseOverInfoGroundStations = true;

        // not a setting per se ; just a reminder
        [GameParameters.CustomStringParameterUI("Connection Line Colors", autoPersistance = false, lines = 3, toolTip = "")]
        public string VisualLineColorTip = "Note that you can change RemoteTech line colors (in the Tracking Station or the Map scene) in the RemoteTech option window.\nSetting available by clicking the RT button in the KSP center scene.";
    }

    public class RemoteTechMiscellaneousSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "RemoteTech Miscellaneous Options";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;
        public override string Section => "RemoteTech";
        public override int SectionOrder => 5;

        [GameParameters.CustomParameterUI("Throttle Time Warp", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: The flight computer will automatically stop time warp a few seconds before executing a queued command.\n<color=#e51a03>OFF</color></b>: The player is responsible for controlling time warp during scheduled actions.")]
        public bool ThrottleTimeWarp = true;

        [GameParameters.CustomParameterUI("No Throttle on no Connection", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: The flight computer cuts the thrust if you lose connection to Mission Control.\n<color=#e51a03>OFF</color></b>: The throttle is not adjusted automatically.")]
        public bool ThrottleZeroOnNoConnection = true;

        [GameParameters.CustomParameterUI("Upgradeable Mission Control Antennas", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: Mission Control antenna range is upgraded when the Tracking Center is upgraded.\n<color=#e51a03>OFF</color></b>: Mission Control antenna range isn't upgradeable.")]
        public bool UpgradeableMissionControlAntennas = true;

        [GameParameters.CustomParameterUI("Add Kerbal Alarm Clock Alarms", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: The flight computer will automatically add alarms to the Kerbal Alarm Clock mod for burn and maneuver commands.\n\tThe alarm goes off 3 minutes before the command executes.\n<color=#e51a03>OFF</color></b>: No alarms are added to Kerbal Alarm Clock.")]
        public bool AutoInsertKaCAlerts = true;
    }

    public class RemoteTecCheatSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "RemoteTech Cheat Options";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;
        public override string Section => "RemoteTech";
        public override int SectionOrder => 6;

        [GameParameters.CustomParameterUI("Control Antennas Without Connection", autoPersistance = true,
            toolTip = "<color=#14e356>ON</color></b>: antennas can be activated, deactivated and targeted without a connection.\n<color=#e51a03>OFF</color></b>: No control without a working connection.")]
        public bool ControlAntennaWithoutConnection = false;
    }
}
