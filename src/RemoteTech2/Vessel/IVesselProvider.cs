using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface IVesselProvider : IEnumerable<IVessel>
    {
        event Action<IVessel> VesselCreated;
        event Action<IVessel> VesselDestroyed;
        IEnumerable<IVessel> Vessels { get; }
        IVessel ActiveVessel { get; }
        IVessel SelectedVessel { get; set; }
    }
}
