using System;
using UnityEngine;

namespace RemoteTech
{
    public class DragModel : MonoBehaviour
    {
        public float dc = 0, maxDistance = 1000;
        public CelestialBody mb;
        
        public float atmDensity
        {
            get
            {
                return (float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(mb.GetAltitude(transform.position), mb));
            }
        }

        float area
        {
            get
            {
                if (transform.collider == null) return 0;

                Vector3
                    dir = -(Krakensbane.GetFrameVelocity() + rigidbody.velocity).normalized,
                    size = transform.collider.bounds.size;

                float
                    XY = (float)Math.Cos(Mathf.Deg2Rad * Vector3.Angle(transform.up, dir)) * size.x * size.y,
                    YZ = (float)Math.Cos(Mathf.Deg2Rad * Vector3.Angle(transform.right, dir)) * size.y * size.z,
                    XZ = (float)Math.Cos(Mathf.Deg2Rad * Vector3.Angle(transform.forward, dir)) * size.x * size.z;

                return XY + YZ + XZ;
            }
        }

        Vector3 dragForceDir
        {
            get
            {
                return -((Krakensbane.GetFrameVelocity() + rigidbody.velocity).normalized * dragForce);
            }
        }


        float dragForce
        {
            get
            {
                float spd = Speed;
                return Mathf.Clamp(((float)(Math.Pow(spd, 2) * atmDensity * 0.5F) * area * dc), 0, (spd / TimeWarp.deltaTime) * rigidbody.mass);
            }
        }

        float Speed
        {
            get
            {
                return (float)(Krakensbane.GetFrameVelocity() + rigidbody.velocity).magnitude;
            }
        }

        public void FixedUpdate()
        {
            if (mb.atmosphere && mb.GetAltitude(transform.position) < mb.maxAtmosphereAltitude)
                rigidbody.AddForce(dragForceDir);
        }
    }
}
