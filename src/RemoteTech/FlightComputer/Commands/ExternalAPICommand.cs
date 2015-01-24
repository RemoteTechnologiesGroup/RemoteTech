using System;
using System.Reflection;

namespace RemoteTech.FlightComputer.Commands
{
    public class ExternalAPICommand : AbstractCommand
    {

        public ConfigNode externalData; //Config node passed to us be external group
        public string description; //What displays on GUI
        public string shortName; //no clue what this does
        public string reflectionGetType; //needed for relfection method to pass data back
        public string reflectionInvokeMember; //ditto
        public string vslGUIDstr; //GUID of vessel, 99% of the time will be FlightGlobals.ActiveVessel
        public Guid vslGUID; //GUID of vessel, as GUID
        public Vessel vsl; 

        public override string Description
        {
            get { return description + Environment.NewLine + base.Description; }
        }
        public override string ShortName { get { return shortName; } }

        public override bool Pop(FlightComputer f)
        {
            Type calledType = Type.GetType(reflectionGetType); //reflection methods
            calledType.InvokeMember(reflectionInvokeMember, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { externalData });

            return false; //ActionGroupCommand returns false here, don't know why but do the same
        }

        public override void Save(ConfigNode n, FlightComputer fc)
        {
            base.Save(n, fc); //run the remotetech save stuff
            ConfigNode ExtAPI = n.GetNode("ExternalAPICommand"); //save passes a config node one level higher then the load method gets, compensate for that
            ExtAPI.AddNode(externalData); //add our configNode of data
            n.RemoveNode("ExternalAPICommand"); //ConfigNode.setNode does not work, use this instead
            n.AddNode(ExtAPI);
            
        }
        public override void Load(ConfigNode n, FlightComputer fc)
        {
            base.Load(n, fc); //load our basic remotetech stuff
            externalData = n.nodes[0]; //load our data noe
                description = externalData.GetValue("Description"); //string on GUI
                shortName = externalData.GetValue("ShortName"); //???
                reflectionGetType = externalData.GetValue("ReflectionGetType"); //required for reflection back
                reflectionInvokeMember = externalData.GetValue("ReflectionInvokeMember"); //required 
                vslGUIDstr = externalData.GetValue("GUIDString");

                foreach (Vessel vsl2 in FlightGlobals.Vessels) //can not find a Guid.Parse method, so do it this way
                {
                    if (vsl2.id.ToString() == vslGUIDstr)
                    {
                        vslGUID = vsl2.id;
                        vsl = vsl2;
                    }
                }
            
        }
        
        
    }
}

//Example method from Action Groups Extended to pass configNode to Remotetech. Ref: https://github.com/SirDiazo/AGExt/blob/master/AGExt/Flight.cs#L1556
//
//public void AGXActivateGroup(ConfigNode externalData)
//        {
//            Type calledType = Type.GetType("ActionGroupsExtended.AGExtExternal, AGExt");
//           calledType.InvokeMember("AGXReceiveData", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { externalData });
//        }
//}
//}

//Example Method in AGX to receive data node back from Remtotech. Ref: https://github.com/SirDiazo/AGExt/blob/master/AGExt/External.cs#L584

//public static void RTDataReceive(ConfigNode node) //receive data back from RT
//        {
//            Debug.Log("AGX Call: RemoteTechCallback");
//            if (HighLogic.LoadedSceneIsFlight)
//            {
//                if (FlightGlobals.ActiveVessel.rootPart.flightID == Convert.ToUInt32(node.GetValue("FlightID")))
//                {

//                    AGXFlight.ActivateActionGroupActivation(Convert.ToInt32(node.GetValue("Group")), Convert.ToBoolean(node.GetValue("Force")), Convert.ToBoolean(node.GetValue("ForceDir")));
                    
//                }
//                else
//                {
//                    AGXOtherVessel otherVsl = new AGXOtherVessel(Convert.ToUInt32(node.GetValue("FlightID")));
//                    otherVsl.ActivateActionGroupActivation(Convert.ToInt32(node.GetValue("Group")), Convert.ToBoolean(node.GetValue("Force")), Convert.ToBoolean(node.GetValue("ForceDir")));
//                }
//            }
//            else
//            {
//                ScreenMessages.PostScreenMessage("AGX Action Not Activated, Remotetech passed invalid vessel", 10F, ScreenMessageStyle.UPPER_CENTER);
                
//            }
//        }