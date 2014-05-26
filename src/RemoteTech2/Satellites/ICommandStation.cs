using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface ICommandStation : IConfigNode
    {
        int CrewRequirement { get; }
        bool IsActive { get; set; }
    }
}
