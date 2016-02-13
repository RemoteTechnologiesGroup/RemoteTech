using System;
using System.Linq;

namespace RemoteTech.Modules
{
    public sealed class MissionControlAntenna : IAntenna
    {
        [Persistent] public float Omni = 75000000;
        [Persistent] public float Dish = 0.0f;
        [Persistent] public double CosAngle = 1.0;

        /// <summary>
        /// Semicolon seperated list with omni ranges for each tech lvl of the tracking station
        /// </summary>
        [Persistent] public string UpgradeableOmni = String.Empty;
        /// <summary>
        /// Semicolon seperated list with dish ranges for each tech lvl of the tracking station
        /// </summary>
        [Persistent] public string UpgradeableDish = String.Empty;
        /// <summary>
        /// Semicolon seperated list with CosAngle ranges for each tech lvl of the tracking station
        /// </summary>
        [Persistent] public string UpgradeableCosAngle = String.Empty;

        public ISatellite Parent { get; set; }

        float IAntenna.Omni { get { return Omni; } }
        Guid IAntenna.Guid { get { return Parent.Guid; } }
        String IAntenna.Name { get { return "Dummy Antenna"; } }
        bool IAntenna.Powered { get { return true; } }
        public bool Connected { get { return RTCore.Instance.Network.Graph [((IAntenna)this).Guid].Any (l => l.Interfaces.Contains (this)); } }
        bool IAntenna.Activated { get { return true; } set { return; } }
        float IAntenna.Consumption { get { return 0.0f; } }
        bool IAntenna.CanTarget { get { return false; } }
        Guid IAntenna.Target { get { return new Guid(RTSettings.Instance.ActiveVesselGuid); } set { return; } }
        float IAntenna.Dish { get { return Dish; } }
        double IAntenna.CosAngle { get { return CosAngle; } }
        
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
                string[] dishRanges = this.UpgradeableOmni.Split(';');
                if (missionControlTechLevelForDish > dishRanges.Count())
                {
                    missionControlTechLevelForDish = dishRanges.Count();
                }

                float.TryParse(dishRanges[missionControlTechLevelForDish - 1], out this.Dish);
            }

            if (this.UpgradeableCosAngle != String.Empty)
            {
                int missionControlTechLevelForCAngle = missionControlTechLevel;
                string[] cAngleRanges = this.UpgradeableOmni.Split(';');
                if (missionControlTechLevelForCAngle > cAngleRanges.Count())
                {
                    missionControlTechLevelForCAngle = cAngleRanges.Count();
                }

                double.TryParse(cAngleRanges[missionControlTechLevelForCAngle - 1], out this.CosAngle);
            }
        }

        public void OnConnectionRefresh() { }

        public int CompareTo(IAntenna antenna)
        {
            return ((IAntenna)this).Consumption.CompareTo(antenna.Consumption);
        }
    }
}