using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class TargetCommand : AbstractCommand
    {
        public override double ExtraDelay { get { return 0.0; } set { return; } }
        public ITargetable Target { get; set; }
        public override int Priority { get { return 1; } }

        public override String Description
        {
            get
            {
                return ("Target: " + (Target != null ? Target.GetName() : "None")) + Environment.NewLine + base.Description;
            }
        }

        public override bool Pop(FlightComputer f)
        {
            f.DelayedTarget = Target;
            return true;
        }

        public override bool Execute(FlightComputer f, FlightCtrlState fcs) { return false; }

        public static TargetCommand WithTarget(ITargetable target)
        {
            return new TargetCommand()
            {
                Target = target,
                TimeStamp = RTUtil.GameTime,
            };
        }
    }
}
