using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RemoteTech
{
    public class RTSettings
    {
        private static Settings mInstance;
        public static Settings Instance
        {
            get
            {
                return mInstance = mInstance ?? Settings.Load();
            }
        }
    }

    public class Settings
    {
        public Dictionary<String, Rect> savedWindowPositions = new Dictionary<String, Rect>();
        [Persistent] public float ConsumptionMultiplier = 1.0f;
        [Persistent] public float RangeMultiplier = 1.0f;
        [Persistent] public String ActiveVesselGuid = "35b89a0d664c43c6bec8d0840afc97b2";
        [Persistent] public float SpeedOfLight = 3e8f;
        [Persistent] public MapFilter MapFilter = MapFilter.Path | MapFilter.Omni | MapFilter.Dish;
        [Persistent] public bool EnableSignalDelay = true;
        [Persistent] public RangeModel.RangeModel RangeModelType = RangeModel.RangeModel.Standard;
        [Persistent] public double MultipleAntennaMultiplier = 0.0;
        [Persistent] public bool ThrottleTimeWarp = true;
        [Persistent] public bool ThrottleZeroOnNoConnection = true;
        [Persistent] public bool HideGroundStationsBehindBody = false;
        [Persistent] public Color DishConnectionColor = XKCDColors.Amber;
        [Persistent] public Color OmniConnectionColor = XKCDColors.BrownGrey;
        [Persistent] public Color ActiveConnectionColor = XKCDColors.ElectricLime;

        [Persistent(collectionIndex="STATION")]
        public MissionControlSatellite[] GroundStations = new MissionControlSatellite[] { new MissionControlSatellite() };
        /// <summary>
        /// Backup config node
        /// </summary>
        private ConfigNode backupNode;


        private static String File
        {
            get { return KSPUtil.ApplicationRootPath + "/GameData/RemoteTech/RemoteTech_Settings.cfg"; }
        }

        /// <summary>
        /// Saves the current RTSettings object to the RemoteTech_Settings.cfg
        /// </summary>
        public void Save()
        {
            try
            {
                ConfigNode details = new ConfigNode("RemoteTechSettings");
                ConfigNode.CreateConfigFromObject(this, 0, details);
                ConfigNode save = new ConfigNode();
                save.AddNode(details);
                save.Save(File);
            }
            catch (Exception e) { RTLog.Notify("An error occurred while attempting to save: " + e.Message); }
        }

        /// <summary>
        /// Stores the MapFilter Value for overriding with third party settings
        /// </summary>
        public void backupFields()
        {
            backupNode = new ConfigNode();
            backupNode.AddValue("MapFilter", MapFilter);
            backupNode.AddValue("ActiveVesselGuid", ActiveVesselGuid);
        }

        /// <summary>
        /// Restores the backuped values
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
            // try to load from the base settings.cfg
            ConfigNode load = ConfigNode.Load(File);

            if (load == null)
            {
                // write new base file to the rt folder
                settings.Save();
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

            // Prefer to load from GameDatabase, to allow easier user customization
            UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            foreach (UrlDir.UrlConfig curSet in configList)
            {
                // only third party files
                if (!curSet.url.Equals("RemoteTech/RemoteTech_Settings/RemoteTechSettings"))
                {
                    RTLog.Notify("Override RTSettings with configs from {0}", curSet.url);
                    settings.backupFields();
                    ConfigNode.LoadObjectFromConfig(settings, curSet.config);
                    settings.restoreBackups();
                }
            }

            return settings;
        }
    }
}
