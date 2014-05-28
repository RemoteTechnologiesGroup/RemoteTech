using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public static class RangeModelExtensions
    {
        public static bool IsTargeting(this IAntenna a, ISatellite sat_b)
        {
            return a.Targets.Any(t => t.IsMultiple ? t.Contains(sat_b) 
                                                   : IsInsideCone(t, a, sat_b));
        }


        private static bool IsInsideCone(Target t, IAntenna a, ISatellite sat_b)
        {
            var pos_a = a.Position;
            var pos_b = sat_b.Position;
            foreach (var sat in t)
            {
                var dir_direct = (sat.Position - pos_a);
                var dir_b = (pos_b - pos_a);
                if (Vector3d.Dot(dir_direct.normalized, dir_b.normalized) >= a.CurrentRadians) return true;
            }
            return false;
        }

        public static double DistanceTo(this ISatellite a, ISatellite b)
        {
            return Vector3d.Distance(a.Position, b.Position);
        }

        public static bool HasLineOfSightWith(this ISatellite a, ISatellite b)
        {
            var aPos = a.Position;
            var bPos = b.Position;
            foreach (var referenceBody in RTCore.Instance.Bodies)
            {
                var bodyFromA = referenceBody.Position - aPos;
                var bFromA = bPos - aPos;
                if (Vector3d.Dot(bodyFromA, bFromA) <= 0) continue;
                var bFromAnorm = bFromA.normalized;
                if (Vector3d.Dot(bodyFromA, bFromAnorm) >= bFromA.magnitude) continue;
                var lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromAnorm) * bFromAnorm;
                if (lateralOffset.magnitude < referenceBody.Radius - 5) return false;
            }
            return true;
        }
    }
}
