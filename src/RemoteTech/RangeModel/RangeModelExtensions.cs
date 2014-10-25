using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelExtensions
    {
        /// <summary>Determines if an antenna has a specific satellite as its target.</summary>
        /// <returns><c>true</c> if a's target is set to <paramref name="target"/>; false otherwise.</returns>
        /// <param name="dish">The antenna being queried.</param>
        /// <param name="target">The satellite being tested for being the antenna target.</param>
        public static bool IsTargetingDirectly(this IAntenna dish, ISatellite target)
        {
            return dish.Target == target.Guid;
        }

        /// <summary>Determines if an antenna can connect to a target through active vessel targeting.</summary>
        /// <returns><c>true</c> if a's target is set to "Active Vessel" and <paramref name="target"/> is active; false otherwise.</returns>
        /// <param name="dish">The antenna being queried.</param>
        /// <param name="target">The satellite being tested for being the antenna target.</param>
        public static bool IsTargetingActiveVessel(this IAntenna dish, ISatellite target)
        {
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            if (activeVessel == null && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                activeVessel = MapView.MapCamera.target.vessel;
            }

            return dish.Target == NetworkManager.ActiveVesselGuid 
                && activeVessel != null && target.Guid == activeVessel.id;
        }

        public static bool IsTargetingPlanet(this IAntenna a, ISatellite sat_b, ISatellite sat_a)
        {
            var planets = RTCore.Instance.Network.Planets;
            if (!planets.ContainsKey(a.Target) || sat_b.Body != planets[a.Target]) return false;
            var dir_cb = (planets[a.Target].position - sat_a.Position);
            var dir_b = (sat_b.Position - sat_a.Position);
            if (Vector3d.Dot(dir_cb.normalized, dir_b.normalized) >= a.CosAngle) return true;
            return false;
        }

        /// <summary>Finds the distance between two ISatellites</summary>
        /// <returns>The distance in meters.</returns>
        public static double DistanceTo(this ISatellite a, ISatellite b)
        {
            return Vector3d.Distance(a.Position, b.Position);
        }

        /// <summary>Finds the distance between an ISatellite and the target of a connection</summary>
        /// <returns>The distance in meters.</returns>
        /// <param name="sat">The satellite from which the distance is to be measured.
        /// <param name="link">The network node to whose destination the distance is to be measured.
        public static double DistanceTo(this ISatellite sat, NetworkLink<ISatellite> link)
        {
            return Vector3d.Distance(sat.Position, link.Target.Position);
        }

        /// <summary>Tests whether two satellites have line of sight to each other</summary>
        /// <returns><c>true</c> if a straight line from a to b is not blocked by any celestial body; 
        /// otherwise, <c>false</c>.</returns>
        public static bool HasLineOfSightWith(this ISatellite satA, ISatellite satB)
        {
            const double minHeight = 5.0;
            Vector3d satAPos = satA.Position;
            Vector3d satBPos = satB.Position;

            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                Vector3d bodyFromA = referenceBody.position - satAPos;
                Vector3d bFromA = satBPos - satAPos;

                // Is body at least roughly between satA and satB?
                if (Vector3d.Dot(bodyFromA, bFromA) <= 0) continue;
                Vector3d bFromANorm = bFromA.normalized;
                if (Vector3d.Dot(bodyFromA, bFromANorm) >= bFromA.magnitude) continue;

                // Above conditions guarantee that Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm 
                // lies between the origin and bFromA
                Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm;
                if (lateralOffset.magnitude < referenceBody.Radius - minHeight) return false;
            }
            return true;
        }
    }
}
