using UnityEngine;

namespace RemoteTech.Common.Extensions
{
    public static class QuaternionExtension
    {
        //  Functions copied from MechJeb master on 18.04.2016
        //TODO: remove this on FlightComputer overhauling if we don't need those functions anymore.

        public static Vector3d DeltaEuler(this Quaternion delta)
        {
            return new Vector3d(
                (delta.eulerAngles.x > 180) ? (delta.eulerAngles.x - 360.0F) : delta.eulerAngles.x,
                -((delta.eulerAngles.y > 180) ? (delta.eulerAngles.y - 360.0F) : delta.eulerAngles.y),
                (delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z
                );
        }
    }
}
