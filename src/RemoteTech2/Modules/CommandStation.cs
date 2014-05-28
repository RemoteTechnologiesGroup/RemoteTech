using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class CommandStation : ICommandStation
    {
        [Persistent] public int CrewRequirement = -1;
        [Persistent] public bool IsActive = false;

        int ICommandStation.CrewRequirement { get { return CrewRequirement; } }
        bool ICommandStation.IsActive { get { return IsActive; } set { IsActive = value; } }

        void IConfigNode.Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        void IConfigNode.Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, 0, node);
        }
    }
}
