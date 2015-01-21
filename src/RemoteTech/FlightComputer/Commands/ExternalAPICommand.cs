using System;
using System.Reflection;

namespace RemoteTech.FlightComputer.Commands
{
    public class ExternalAPICommand : AbstractCommand
    {

        public ConfigNode externalData; //Config node passed to us be external group
        public string description;
        public string shortName;
        public string reflectionGetType;
        public string reflectionInvokeMember;
        public string vslGUIDstr;
        public Guid vslGUID;
        public Vessel vsl;

        public override string Description
        {
            get { return description; }
        }
        public override string ShortName { get { return shortName; } }

        public override bool Pop(FlightComputer f)
        {
            Type calledType = Type.GetType(reflectionGetType); //
            calledType.InvokeMember(reflectionInvokeMember, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { externalData });

            return false;
        }

        
    }
}
//public void AGXActivateGroup(ConfigNode externalData)
//        {
//            Type calledType = Type.GetType("ActionGroupsExtended.AGExtExternal, AGExt");
//           calledType.InvokeMember("AGXReceiveData", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { externalData });
//        }
//}
//}