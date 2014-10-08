using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelExtensions
    {
        public static bool IsTargetingDirectly(this IAntenna a, ISatellite sat_b)
        {
            return a.Target == sat_b.Guid;
        }

        public static bool IsTargetingActiveVessel(this IAntenna a, ISatellite sat_b)
        {
            var active_vessel = FlightGlobals.ActiveVessel;
            if (active_vessel == null && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                active_vessel = MapView.MapCamera.target.vessel;
            }

            return a.Target == NetworkManager.ActiveVesselGuid && active_vessel != null && sat_b.Guid == active_vessel.id;
        }

        public static bool IsTargetingPlanet(this IAntenna a, ISatellite sat_b, ISatellite sat_a)
        {
            var planets = RTCore.Instance.Network.Planets;
            if (!planets.ContainsKey(a.Target) || sat_b.Body != planets[a.Target]) return false;
            var dir_cb = (planets[a.Target].position - sat_a.Position);
            var dir_b = (sat_b.Position - sat_a.Position);
            if (Vector3d.Dot(dir_cb.normalized, dir_b.normalized) >= a.Radians) return true;
            return false;
        }

        public static double DistanceTo(this ISatellite a, ISatellite b)
        {
            return Vector3d.Distance(a.Position, b.Position);
        }

        public static double DistanceTo(this ISatellite a, NetworkLink<ISatellite> b)
        {
            return Vector3d.Distance(a.Position, b.Target.Position);
        }

        public static bool HasLineOfSightWith(this ISatellite a, ISatellite b)
        {
            var aPos = a.Position;
            var bPos = b.Position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                Vector3d bodyFromA = referenceBody.position - aPos;
                Vector3d bFromA = bPos - aPos;
                if (Vector3d.Dot(bodyFromA, bFromA) <= 0) continue;
                Vector3d bFromAnorm = bFromA.normalized;
                if (Vector3d.Dot(bodyFromA, bFromAnorm) >= bFromA.magnitude) continue;
                Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromAnorm) * bFromAnorm;
                if (lateralOffset.magnitude < referenceBody.Radius - 5) return false;
            }
            return true;
        }
    }
}
