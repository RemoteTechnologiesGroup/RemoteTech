using System;
using System.Linq;
using RemoteTech.SimpleTypes;

namespace RemoteTech.Modules
{
    public sealed class MissionControlAntenna : IAntenna, IConfigNode
    {
        public float Omni = 75000000;
        public float Dish = 0.0f;
        public double CosAngle = 1.0;

        /// <summary>
        /// Semicolon seperated list with omni ranges for each tech lvl of the tracking station
        /// </summary>
        public string UpgradeableOmni = String.Empty;
        /// <summary>
        /// Semicolon seperated list with dish ranges for each tech lvl of the tracking station
        /// </summary>
        public string UpgradeableDish = String.Empty;
        /// <summary>
        /// Semicolon seperated list with CosAngle ranges for each tech lvl of the tracking station
        /// </summary>
        public string UpgradeableCosAngle = String.Empty;

        public void Load(ConfigNode node)
        {
            node.TryGetValue("Omni", ref Omni);
            node.TryGetValue("Dish", ref Dish);
            node.TryGetValue("CosAngle", ref CosAngle);
            node.TryGetValue("UpgradeableOmni", ref UpgradeableOmni);
            node.TryGetValue("UpgradeableDish", ref UpgradeableDish);
            node.TryGetValue("UpgradeableCosAngle", ref UpgradeableCosAngle);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("Omni", Omni);
            node.AddValue("Dish", Dish);
            node.AddValue("CosAngle", CosAngle);
            node.AddValue("UpgradeableOmni", UpgradeableOmni);
            node.AddValue("UpgradeableDish", UpgradeableDish);
            node.AddValue("UpgradeableCosAngle", UpgradeableCosAngle);
        }

        public ISatellite Parent { get; set; }

        private readonly AntennaState mState = new();

        float IAntenna.Omni => Omni * MissionControlRangeMultiplier;
        Guid IAntenna.Guid => Parent.Guid;
        string IAntenna.Name => "Dummy Antenna";
        bool IAntenna.Powered => true;
        public bool Connected => RTCore.Instance.Network.IsAntennaConnected(this);
        bool IAntenna.Activated
        {
            get => true;
            set { }
        }
        float IAntenna.Consumption => 0.0f;
        bool IAntenna.CanTarget => false;
        Guid IAntenna.Target
        {
            get => RTSettings.Instance.ActiveVesselGuidParsed;
            set { }
        }
        float IAntenna.Dish => Dish * MissionControlRangeMultiplier;
        double IAntenna.CosAngle => CosAngle;
        private float MissionControlRangeMultiplier => RTSettings.Instance.MissionControlRangeMultiplier;

        public void reloadUpgradeableAntennas(int techlvl = 0)
        {
            if (this.UpgradeableCosAngle != String.Empty && this.UpgradeableDish != String.Empty && this.UpgradeableOmni != String.Empty)
                return;

            int missionControlTechLevel = techlvl;
            if(missionControlTechLevel == 0)
            {
                missionControlTechLevel = (int)((2 * ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation)) + 1);
            }

            // when the option is disabled, use always the thrid tech lvl
            if (!RTSettings.Instance.UpgradeableMissionControlAntennas)
            {
                missionControlTechLevel = 3;
            }

            RTLog.Verbose("Reload upgradeable Antennas, TechLvl: {0}", RTLogLevel.LVL4, missionControlTechLevel);

            if (this.UpgradeableOmni != String.Empty)
            {
                int missionControlTechLevelForOmni = missionControlTechLevel;
                string[] omniRanges = this.UpgradeableOmni.Split(';');
                if (missionControlTechLevelForOmni > omniRanges.Count())
                {
                    missionControlTechLevelForOmni = omniRanges.Count();
                }

                float.TryParse(omniRanges[missionControlTechLevelForOmni - 1], out this.Omni);
            }

            if (this.UpgradeableDish != String.Empty)
            {
                int missionControlTechLevelForDish = missionControlTechLevel;
                string[] dishRanges = this.UpgradeableDish.Split(';');
                if (missionControlTechLevelForDish > dishRanges.Count())
                {
                    missionControlTechLevelForDish = dishRanges.Count();
                }

                float.TryParse(dishRanges[missionControlTechLevelForDish - 1], out this.Dish);
            }

            if (this.UpgradeableCosAngle != String.Empty)
            {
                int missionControlTechLevelForCAngle = missionControlTechLevel;
                string[] cAngleRanges = this.UpgradeableCosAngle.Split(';');
                if (missionControlTechLevelForCAngle > cAngleRanges.Count())
                {
                    missionControlTechLevelForCAngle = cAngleRanges.Count();
                }

                double.TryParse(cAngleRanges[missionControlTechLevelForCAngle - 1], out this.CosAngle);
            }
        }

        internal void RegisterState()
        {
            UpdateState();
            mState.Register();
        }

        internal void UnregisterState() => mState.Dispose();

        internal void UpdateState()
        {
            IAntenna antenna = this;
            mState.Guid = antenna.Guid;
            mState.Target = antenna.Target;
            mState.Activated = antenna.Activated;
            mState.Powered = antenna.Powered;
            mState.Connected = RTCore.Instance != null
                && RTCore.Instance.Network != null
                && RTCore.Instance.Network.IsAntennaConnected(this);
            mState.CanTarget = antenna.CanTarget;
            mState.Dish = antenna.Dish;
            mState.CosAngle = antenna.CosAngle;
            mState.Omni = antenna.Omni;
            mState.Consumption = antenna.Consumption;
        }

        public void OnConnectionRefresh() { }

        public int CompareTo(IAntenna antenna)
        {
            return ((IAntenna)this).Consumption.CompareTo(antenna.Consumption);
        }

        public void SetOmniAntennaRange(float range)
        {
            this.Omni = range;
        }
    }
}