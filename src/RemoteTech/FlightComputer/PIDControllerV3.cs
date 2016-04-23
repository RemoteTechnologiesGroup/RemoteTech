/// <summary>
/// Proportional controller for KSP autopilots
/// </summary>

using System;

namespace RemoteTech.FlightComputer
{
    // -----------------------------------------------
    // Copied from MechJeb master on 18.04.2016
    public class PIDControllerV3 : IConfigNode 
    {
        public Vector3d Kp, Ki, Kd, intAccum, derivativeAct, propAct;
        public double max, min;

        public PIDControllerV3(Vector3d Kp, Vector3d Ki, Vector3d Kd, double max = double.MaxValue, double min = double.MinValue) 
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error, Vector3d omega, Vector3d Wlimit) 
        {
            derivativeAct = Vector3d.Scale(omega, Kd);
            Wlimit = Vector3d.Scale(Wlimit, Kd);

            // integral actíon + Anti Windup
            intAccum.x = (Math.Abs(derivativeAct.x) < 0.6 * max) ? intAccum.x + (error.x * Ki.x * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.x;
            intAccum.y = (Math.Abs(derivativeAct.y) < 0.6 * max) ? intAccum.y + (error.y * Ki.y * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.y;
            intAccum.z = (Math.Abs(derivativeAct.z) < 0.6 * max) ? intAccum.z + (error.z * Ki.z * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.z;

            propAct = Vector3d.Scale(error, Kp);

            Vector3d action = propAct + intAccum;

            // Clamp (propAct + intAccum) to limit the angular velocity:
            action = new Vector3d(Math.Max(-Wlimit.x, Math.Min(Wlimit.x, action.x)),
                                  Math.Max(-Wlimit.y, Math.Min(Wlimit.y, action.y)),
                                  Math.Max(-Wlimit.z, Math.Min(Wlimit.z, action.z)));

            // add. derivative action 
            action += derivativeAct;

            // action clamp
            action = new Vector3d(Math.Max(min, Math.Min(max, action.x)),
                                  Math.Max(min, Math.Min(max, action.y)),
                                  Math.Max(min, Math.Min(max, action.z)));
            return action;
        }

        public void Reset() 
        {
            intAccum = Vector3d.zero;
        }

        public void Load(ConfigNode node) 
        {
            if (node.HasValue("Kp")) 
            {
                Kp = ConfigNode.ParseVector3D(node.GetValue("Kp"));
            }
            if (node.HasValue("Ki")) 
            {
                Ki = ConfigNode.ParseVector3D(node.GetValue("Ki"));
            }
            if (node.HasValue("Kd")) 
            {
                Kd = ConfigNode.ParseVector3D(node.GetValue("Kd"));
            }
        }

        public void Save(ConfigNode node) 
        {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
        // end MechJeb import
        //---------------------------------------
    }
}
