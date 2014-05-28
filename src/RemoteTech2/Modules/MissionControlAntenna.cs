using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTech
{
    public sealed class MissionControlAntenna : IAntenna
    {
        String IAntenna.Name            { get { return "Dummy Antenna"; } }
        Guid IAntenna.Guid              { get { return Parent.Guid; } }
        Vector3d IAntenna.Position      { get { return Parent.Position; } }
        bool IAntenna.Powered           { get { return true; } }
        bool IAntenna.Activated         { get { return true; } set { return; } }
        bool IAntenna.CanTarget         { get { return false; } }
        IList<Target> IAntenna.Targets  { get { return new Target[0]; } }
        float IAntenna.CurrentDishRange             { get { return 0.0f; } }
        double IAntenna.CurrentRadians         { get { return 1.0; } }
        float IAntenna.CurrentOmniRange             { get { return Omni * RTSettings.Instance.RangeMultiplier; } }
        float IAntenna.CurrentConsumption      { get { return 0.0f; } }
 
        public ISatellite Parent { get; set; }

        [Persistent]
        public float Omni = 75000000;
    }
}