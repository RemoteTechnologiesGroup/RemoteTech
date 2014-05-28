using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface IRangeModel
    {
        NetworkLink<ISatellite> GetLink(ISatellite sat_a, ISatellite sat_b);
    }
}
