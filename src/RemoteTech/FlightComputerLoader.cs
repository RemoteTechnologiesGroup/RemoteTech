using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RemoteTech.Common;
using RemoteTech.Common.Interfaces.FlightComputer;
using RemoteTech.Common.Interfaces.SignalProcessor;

namespace RemoteTech
{
    public class FlightComputerLoader
    {
        public static ConstructorInfo Constructor;
        public FlightComputerLoader()
        {
            
        }

        public static void GetTypes()
        {
            var fcList = AssemblyLoader.GetModulesImplementingInterface<IFlightComputer>(new []{typeof(ISignalProcessor)});
            foreach (var ctor in fcList)
            {
                RTLog.Notify($"'{ctor.Name}' flight computer interface was found in module {ctor.Module.Name}" );
            }
            
            if(fcList.Count > 0)
                Constructor = fcList[0];
        }
    }
}
