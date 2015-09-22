using System;
using System.Linq;

namespace RemoteTech.Modules
{
    public sealed class MissionControlAntenna : IAntenna
    {
        [Persistent] public float Omni = 75000000;
        [Persistent] public float Dish = 0.0f;
        [Persistent] public double CosAngle = 1.0;

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

        public void OnConnectionRefresh() { }

        public int CompareTo(IAntenna antenna)
        {
            return ((IAntenna)this).Consumption.CompareTo(antenna.Consumption);
        }
    }
}