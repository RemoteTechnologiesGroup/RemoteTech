using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class TargetCommand : AbstractCommand
    {
        /// Defines which target we have. Can be CelestialBody or Vessel
        [Persistent] public String TargetType;
        /// Target identifier, CelestialBody=Body-id or Vessel=GUID. Depends on TargetType
        [Persistent] public String TargetId;

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
            // Switch the vessels target
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

        /// <summary>
        /// Load the saved TargetCommand. Open a new ITargetable object by
        /// the objects id.
        /// </summary>
        /// <param name="n">Node with the command infos</param>
        /// <param name="fc">Current flightcomputer</param>
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

        /// <summary>
        /// Save the TargetCommand. By targeting a vessel we'll save the guid,
        /// by a CelestialBody we save the bodys id.
        /// </summary>
        /// <param name="n">Node to save in</param>
        /// <param name="fc">Current flightcomputer</param>
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
