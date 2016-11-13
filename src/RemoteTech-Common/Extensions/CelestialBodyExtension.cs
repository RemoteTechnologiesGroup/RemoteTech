using System;
using System.Text;
using UnityEngine;

namespace RemoteTech.Common.Extensions
{
    public static class CelestialBodyExtension
    {
        public static Guid Guid(this CelestialBody cb)
        {
            var name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (var i = 0; i < 16; i++)
            {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            return new Guid(s.ToString());
        }

        public static bool CBhit(CelestialBody body, out Vector2 latlon)
        {

            Vector3d hitA;
            Vector3 origin, dir;

            if (MapView.MapIsEnabled)
            {
                //Use Scaled camera and don't attempt physics raycast if in map view.
                var ray = ScaledCamera.Instance.galaxyCamera.ScreenPointToRay(Input.mousePosition);
                origin = ScaledSpace.ScaledToLocalSpace(ray.origin);
                dir = ray.direction.normalized;
            }
            else
            {
                //Attempt ray cast and return results if successfull.
                var ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitB;
                var dist = (float)(Vector3.Distance(body.position, ray.origin) - body.Radius / 2);
                if (Physics.Raycast(ray, out hitB, dist))
                {
                    latlon = new Vector2((float)body.GetLatitude(hitB.point), (float)body.GetLongitude(hitB.point));
                    return true;
                }
                //if all else fails, try with good oldfashioned arithmetic.
                origin = ray.origin;
                dir = ray.direction.normalized;
            }

            if (CBhit(body, origin, dir, out hitA))
            {
                latlon = new Vector2((float)body.GetLatitude(hitA), (float)body.GetLongitude(hitA));
                return true;
            }
            latlon = Vector2.zero;
            return false;
        }

        public static bool CBhit(CelestialBody body, Vector3d originalOrigin, Vector3d direction, out Vector3d hit)
        {
            var r = body.Radius;
            //convert the origin point from world space to body local space and assume body center as (0,0,0).
            var origin = originalOrigin - body.position;

            //Compute A, B and C coefficients
            var a = Vector3d.Dot(direction, direction);
            var b = 2 * Vector3d.Dot(direction, origin);
            var c = Vector3d.Dot(origin, origin) - (r * r);

            //Find discriminant
            var disc = b * b - 4 * a * c;

            // if discriminant is negative there are no real roots, so return 
            // false as ray misses sphere
            if (disc < 0)
            {
                hit = Vector3d.zero;
                return false;
            }

            // compute q.
            var distSqrt = Math.Sqrt(disc);
            double q;
            if (b < 0)
                q = (-b - distSqrt) / 2.0;
            else
                q = (-b + distSqrt) / 2.0;

            // compute t0 and t1
            var t0 = q / a;
            var t1 = c / q;

            // make sure t0 is smaller than t1
            if (t0 > t1)
            {
                // if t0 is bigger than t1 swap them around
                var temp = t0;
                t0 = t1;
                t1 = temp;
            }

            // if t1 is less than zero, the body is in the ray's negative direction
            // and consequently the ray misses the sphere
            if (t1 < 0)
            {
                hit = Vector3d.zero;
                return false;
            }

            // if t0 is less than zero, the intersection point is at t1
            if (t0 < 0)
            {
                hit = originalOrigin + (t1 * direction);
                return true;
            }

            // the intersection point is at t0
            hit = originalOrigin + (t0 * direction);
            return true;
        }
    }
}
