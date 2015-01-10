/// <summary>
/// Proportional controller for KSP autopilots
/// </summary>
/// <description>
/// This is a copy of MechJeb's proportional controller, downloaded by Starstrider42 
///     from master on June 27, 2014
/// </description>

using System;

namespace RemoteTech.FlightComputer
{
    // This PID Controler is used by Raf04 patch for the attitude controler. They have a separate implementation since they use
    // a different set of argument and do more (and less) than the other PID controler
    public class PIDControllerV2 : IConfigNode
    {
        public Vector3d intAccum, derivativeAct, propAct;
        public double Kp, Ki, Kd, max, min;

        public PIDControllerV2(double Kp = 0, double Ki = 0, double Kd = 0, double max = double.MaxValue, double min = double.MinValue)
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
            this.max = max;
            this.min = min;
            Reset();
        }

        public Vector3d Compute(Vector3d error, Vector3d omega )
        {
            derivativeAct = omega * Kd;

            // integral actíon + Anti Windup
            intAccum.x = (Math.Abs(derivativeAct.x) < 0.6 * max) ? intAccum.x + (error.x * Ki * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.x;
            intAccum.y = (Math.Abs(derivativeAct.y) < 0.6 * max) ? intAccum.y + (error.y * Ki * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.y;
            intAccum.z = (Math.Abs(derivativeAct.z) < 0.6 * max) ? intAccum.z + (error.z * Ki * TimeWarp.fixedDeltaTime) : 0.9 * intAccum.z;

            propAct = error * Kp;

            Vector3d action = propAct + derivativeAct + intAccum;

            // action clamp
            action = new Vector3d(Math.Max(min, Math.Min(max, action.x)),
                Math.Max(min, Math.Min(max, action.y)),
                Math.Max(min, Math.Min(max, action.z)) );
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
                Kp = Convert.ToDouble(node.GetValue("Kp"));
            }
            if (node.HasValue("Ki"))
            {
                Ki = Convert.ToDouble(node.GetValue("Ki"));
            }
            if (node.HasValue("Kd"))
            {
                Kd = Convert.ToDouble(node.GetValue("Kd"));
            }
        }

        public void Save(ConfigNode node)
        {
            node.SetValue("Kp", Kp.ToString());
            node.SetValue("Ki", Ki.ToString());
            node.SetValue("Kd", Kd.ToString());
        }
    }
}
