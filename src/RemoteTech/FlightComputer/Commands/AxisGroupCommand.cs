using System;

namespace RemoteTech.FlightComputer.Commands
{
    public class AxisGroupCommand : AbstractCommand
    {
#if !KSP131
        [Persistent] public KSPAxisGroup AxisGroup;
        [Persistent] public float AxisValue;
#endif

        public override string Description
        {
            get { return ShortName + Environment.NewLine + base.Description; }
        }

        public override string ShortName
        {
            get
            {
#if KSP131
                return "Unavailable in KSP 1.3.1";
#else
                return "Press Axis Group " + AxisGroup + " with value " + AxisValue;
#endif
            }
        }

        public override bool Pop(FlightComputer f)
        {
#if !KSP131
            for (int i = 0; i < f.Vessel.vesselModules.Count; i++)
            {
                if (f.Vessel.vesselModules[i] is AxisGroupsModule)
                {
                    var agModule = f.Vessel.vesselModules[i] as AxisGroupsModule;
                    agModule.UpdateAxisGroup(AxisGroup, AxisValue);
                }
            }
#endif

            return false;
        }

#if !KSP131
        public static AxisGroupCommand WithGroup(KSPAxisGroup group, float value)
        {
            return new AxisGroupCommand()
            {
                AxisGroup = group,
                AxisValue = value,
                TimeStamp = RTUtil.GameTime,
            };
        }
#endif
    }
}
