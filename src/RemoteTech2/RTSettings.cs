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
        private static Settings instance;
        public static Settings Instance
        {
            get
            {
                return instance ?? (instance = Settings.Load());
            }
        }
    }

    public class Settings
    {
        private static ILogger Logger = RTLogger.CreateLogger(typeof(RTSettings));

        [Persistent] public float ConsumptionMultiplier = 1.0f;
        [Persistent] public float RangeMultiplier = 1.0f;
        [Persistent] public String ActiveVesselGuid = "35b89a0d664c43c6bec8d0840afc97b2";
        [Persistent] public float SpeedOfLight = 3e8f;
        [Persistent] public MapFilter MapFilter = MapFilter.Path | MapFilter.Omni | MapFilter.Dish;
        [Persistent] public bool EnableSignalDelay = true;
        [Persistent] public RangeModelType RangeModelType = RangeModelType.Standard;
        [Persistent] public double MultipleAntennaMultiplier = 0.0;
        [Persistent] public bool ThrottleTimeWarp = true;
        [Persistent] public Color DishConnectionColor = XKCDColors.Amber;
        [Persistent] public Color OmniConnectionColor = XKCDColors.BrownGrey;
        [Persistent] public Color ActiveConnectionColor = XKCDColors.ElectricLime;

        [Persistent(collectionIndex="STATION")] 
        public MissionControlSatellite[] GroundStations = new MissionControlSatellite[] { new MissionControlSatellite() };

        private static String File { 
            get { return KSPUtil.ApplicationRootPath + "/GameData/RemoteTech2/RemoteTech_Settings.cfg"; }
        }

        public void Save()
        {
            try
            {
                var save = new ConfigNode();
                ConfigNode.CreateConfigFromObject(this, 0, save);
                save.Save(File);
            }
            catch (Exception e) { Logger.Error("An error occurred while attempting to save: " + e.Message); }
        }

        public static Settings Load()
        {
            var load = ConfigNode.Load(File);
            var settings = new Settings();
            if (load == null)
            {
                settings.Save();
                return settings;
            }
            ConfigNode.LoadObjectFromConfig(settings, load);
            
            return settings;
        }
    }
}
