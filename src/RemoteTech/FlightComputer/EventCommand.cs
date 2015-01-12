using System;
using System.Linq;

using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.FlightComputer
{
    public class EventCommand : AbstractCommand
    {
        [Persistent] public string GUIName;
        [Persistent] public int PartId;
        [Persistent] public string Module;

        public BaseEvent BaseEvent = null;

        public override String Description
        {
            get
            {
                return BaseEvent.listParent.part.partInfo.title + ": " + BaseEvent.GUIName +
                        Environment.NewLine + base.Description;
            }
        }
        public override string ShortName
        {
            get
            {
                return BaseEvent.GUIName;
            }
        }
         
        public override bool Pop(FlightComputer f)
        {
            BaseEvent.Invoke();
            
            return false;
        }

        public static EventCommand Event(BaseEvent ev)
        {
            return new EventCommand()
            {
                BaseEvent = ev,
                GUIName = ev.GUIName,
                PartId = FlightGlobals.ActiveVessel.parts.ToList().IndexOf(ev.listParent.part),
                Module = ev.listParent.module.ClassName.ToString(),
                TimeStamp = RTUtil.GameTime,
            };
        }

        /// <summary>
        /// Load infos into this object and create a new BaseEvent
        /// </summary>
        public override void Load(ConfigNode n, FlightComputer fc)
        {
            base.Load(n, fc);

            PartId = int.Parse(n.GetValue("PartId"));
            Module = n.GetValue("Module");
            GUIName = n.GetValue("GUIName");

            Part part = FlightGlobals.ActiveVessel.parts[PartId];
            PartModule partmodule = part.Modules[Module];
            BaseEventList eventlist = new BaseEventList(part, partmodule);
            if (eventlist.Count > 0)
            {
                BaseEvent = eventlist.Where(ba => ba.GUIName == GUIName).FirstOrDefault();
            }
        }
    }
}
