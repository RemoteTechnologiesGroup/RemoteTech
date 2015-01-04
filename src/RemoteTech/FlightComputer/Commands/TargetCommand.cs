using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class TargetCommand : AbstractCommand
    {
        [Persistent]
        public String TargetId;
        [Persistent]
        public String TargetType;
        public override double ExtraDelay { get { return 0.0; } set { return; } }
        public ITargetable Target { get; set; }
        public override int Priority { get { return 1; } }

        public override String Description
        {
            get
            {
                return ShortName + Environment.NewLine + base.Description;
            }
        }
        public override string ShortName { get { return "Target: " + (Target != null ? Target.GetName() : "None"); } }

        public override bool Pop(FlightComputer f)
        {
            f.DelayedTarget = Target;
            f.lastTarget = this;
            FlightGlobals.fetch.SetVesselTarget(Target);

            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs) {

            return false;
        }

        public static TargetCommand WithTarget(ITargetable target)
        {
            return new TargetCommand()
            {
                Target = target,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public override void Load(ConfigNode n, FlightComputer fc)
        {
            base.Load(n, fc);

            switch (TargetType)
            {
                case "Vessel":
                    {
                        Guid Vesselid = new Guid(TargetId);
                        Target = FlightGlobals.Vessels.Where(v => v.id == Vesselid).FirstOrDefault();
                        break;
                    }
                case "CelestialBody":
                    {
                        Target = FlightGlobals.Bodies.ElementAt(int.Parse(TargetId));
                        break;
                    }
                default:
                    {
                        Target = null;
                        break;
                    }
            }
        }

        public override void Save(ConfigNode n, FlightComputer fc)
        {
            if (Target != null)
                TargetType = Target.GetType().ToString();

            switch (TargetType)
            {
                case "Vessel":
                    {
                        TargetId = ((Vessel)Target).id.ToString();
                        break;
                    }
                case "CelestialBody":
                    {
                        TargetId = FlightGlobals.Bodies.ToList().IndexOf(((CelestialBody)Target)).ToString();
                        break;
                    }
                default:
                    {
                        TargetId = null;
                        break;
                    }
            }
            base.Save(n, fc);
        }
    }
}
