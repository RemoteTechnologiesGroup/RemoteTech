using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech 
{
    public class CelestialBodyProxy : ICelestialBody
    {
        private static Dictionary<CelestialBody, Guid> cache = new Dictionary<CelestialBody, Guid>();
        public String Name { get { return celestialBody.bodyName; } }
        public Guid Guid {
            get
            {
                try
                {
                    return cache[celestialBody];
                }
                catch (ArgumentException)
                {
                    return cache[celestialBody] = celestialBody.GenerateGuid();
                }
            }
        }

        public Vector3d Position { get { return celestialBody.position; } }
        public Vector3d Up       { get { return celestialBody.transform.up; } }
        public ICelestialBody Parent { get { return (CelestialBodyProxy) celestialBody.referenceBody; } }
        public Color Color { get { return celestialBody.GetOrbitDriver() != null ? celestialBody.GetOrbitDriver().orbitColor : Color.yellow; } }
        public double Radius     { get { return celestialBody.Radius; } }

        public double SemiMajorAxis { get { return celestialBody.orbit.semiMajorAxis; } }

        private readonly CelestialBody celestialBody;

        private CelestialBodyProxy(CelestialBody celestialBody) {
            this.celestialBody = celestialBody;
        }

        public static explicit operator CelestialBodyProxy(CelestialBody celestialBody)
        {
            return new CelestialBodyProxy(celestialBody);
        }
    }
}
