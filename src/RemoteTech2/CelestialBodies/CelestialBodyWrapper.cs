using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    internal class CelestialBodyWrapper : ISatellite
    {
        public bool IsVisible { get { return true; } }
        public String Name { get { return celestialBody.Name; } set { } }
        public Guid Guid { get { return celestialBody.Guid; } }
        public Vector3d Position { get { return celestialBody.Position; } }
        public ICelestialBody Body { get { return celestialBody; } }
        public bool IsPowered { get { return true; } }
        public  Group Group { get { return Group.Empty; } set { } }
        public bool IsCommandStation { get { return false; } }
        public bool HasLocalControl { get { return false; } }
        public IEnumerable<IAntenna> Antennas { get { return Enumerable.Empty<IAntenna>(); } }

        private readonly ICelestialBody celestialBody;
        private readonly int hash;
        public CelestialBodyWrapper(ICelestialBody celestialBody)
        {
            this.celestialBody = celestialBody;
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }

    public static class CelestialBodyExtensions
    {
        public static ISatellite AsSatellite(this ICelestialBody cb)
        {
            return new CelestialBodyWrapper(cb);
        }
    }
}
