using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public interface ICelestialBodyProvider : IEnumerable<ICelestialBody>
    {
        IEnumerable<ICelestialBody> CelestialBodies { get; }
        ICelestialBody WithName(String name);
    }
}
