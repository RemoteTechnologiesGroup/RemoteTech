using System;
using System.Linq;

namespace RemoteTech.Modules
{
    public sealed class MissionControlAntenna : IAntenna
    {
        [Persistent] public float Omni = 75000000;
        public ISatellite Parent { get; set; }

        float IAntenna.Omni { get { return Omni; } }
        Guid IAntenna.Guid { get { return Parent.Guid; } }
        String IAntenna.Name { get { return "Dummy Antenna"; } }
        bool IAntenna.Powered { get { return true; } }
        public bool Connected { get { return RTCore.Instance.Network.Graph [((IAntenna)this).Guid].Any (l => l.Interfaces.Contains (this)); } }
        bool IAntenna.Activated { get { return true; } set { return; } }
        float IAntenna.Consumption { get { return 0.0f; } }
        bool IAntenna.CanTarget { get { return false; } }
        Guid IAntenna.Target { get { return Guid.Empty; } set { return; } }
        float IAntenna.Dish { get { return 0.0f; } }
        double IAntenna.CosAngle { get { return 1.0; } }

        public void OnConnectionRefresh() { }

        public int CompareTo(IAntenna antenna)
        {
            return ((IAntenna)this).Consumption.CompareTo(antenna.Consumption);
        }
    }
}