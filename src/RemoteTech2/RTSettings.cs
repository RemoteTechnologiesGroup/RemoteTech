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
        [Persistent] public float MissionControlRange = 75000000.0f;
        [Persistent] public Vector3 MissionControlPosition = new Vector3(-0.1313315f, -74.59484f, 75.0f);
        [Persistent] public String MissionControlGuid = "5105f5a9d62841c6ad4b21154e8fc488";
        [Persistent] public String ActiveVesselGuid = "35b89a0d664c43c6bec8d0840afc97b2";
        [Persistent] public int MissionControlBody = 1;
        [Persistent] public float ConsumptionMultiplier = 1.0f;
        [Persistent] public float RangeMultiplier = 1.0f;
        [Persistent] public float SpeedOfLight = 3e8f;
        [Persistent] public MapFilter MapFilter = MapFilter.Path | MapFilter.Omni | MapFilter.Dish;
        [Persistent] public bool EnableSignalDelay = false;
        [Persistent] public RangeModel RangeModelType = RangeModel.Standard;
        [Persistent] public bool NathanKell_MultipleAntennaSupport = false;
        [Persistent] public bool ThrottleTimeWarp = true;

        private static String File { get { return KSPUtil.ApplicationRootPath + "/GameData/RemoteTech2/RemoteTech_Settings.cfg"; } }

        public void Save()
        {
            ConfigNode save = ConfigNode.CreateConfigFromObject(this);
            save.Save(File);
        }

        public static Settings Load()
        {
            ConfigNode load = ConfigNode.Load(File);
            Settings settings = new Settings();
            if (load == null) return settings;
            ConfigNode.LoadObjectFromConfig(settings, load);
            settings.Save();
            return settings;
        }
    }
}
