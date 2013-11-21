using System;
using System.Linq;

namespace RemoteTech
{
    public class MissionControlAntenna : IAntenna, IConfigNode
    {
        [Persistent] public float Omni = 75000000;

        float IAntenna.Omni { get { return Omni; } }
        Guid IAntenna.Guid { get { return MissionControlSatellite.Guid; } }
        String IAntenna.Name { get { return "Dummy Antenna"; } }
        bool IAntenna.Powered { get { return true; } }
        bool IAntenna.Activated { get { return true; } set { return; } }
        float IAntenna.Consumption { get { return 0.0f; } }
        bool IAntenna.CanTarget { get { return false; } }
        Guid IAntenna.Target { get { return Guid.Empty; } set { return; } }
        float IAntenna.Dish { get { return 0.0f; } }
        double IAntenna.Radians { get { return 1.0; } }


        public void Save(ConfigNode node)
        {
            var save = ConfigNode.CreateConfigFromObject(this);
            node.CopyTo(node);
        }

        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        public void OnConnectionRefresh() { }

        public int CompareTo(IAntenna antenna)
        {
            return ((IAntenna)this).Consumption.CompareTo(antenna.Consumption);
        }
    }
}