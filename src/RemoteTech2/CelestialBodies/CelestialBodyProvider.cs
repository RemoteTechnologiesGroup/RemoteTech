using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class CelestialBodyProvider : ICelestialBodyProvider
    {
        public IEnumerable<ICelestialBody> CelestialBodies
        {
            get { return FlightGlobals.Bodies.Select(b => (ICelestialBody)(CelestialBodyProxy) b); }
        }

        public ICelestialBody WithName(String name)
        {
            return FlightGlobals.Bodies.Where(b => b.bodyName.Equals(name))
                                       .Select(b => (ICelestialBody)(CelestialBodyProxy) b)
                                       .FirstOrDefault();
        }

        public IEnumerator<ICelestialBody> GetEnumerator()
        {
            return CelestialBodies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return CelestialBodies.GetEnumerator();
        }
    }
}
