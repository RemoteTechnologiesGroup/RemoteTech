using System;
using System.Linq;

using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.FlightComputer
{
    public class EventCommand : AbstractCommand
    {
        // Guiname of the BaseEvent
        [Persistent] public string GUIName;
        // flight id of the part by this BaseEvent
        [Persistent] public uint flightID;
        // PartModule of the part by this BaseEvent
        [Persistent] public string Module;
        // BaseEvent to invoke
        public BaseEvent BaseEvent = null;

        public override String Description
        {
            get
            {
                return ((this.BaseEvent != null) ? this.BaseEvent.listParent.part.partInfo.title + ": " + this.BaseEvent.GUIName : "none") +
                        Environment.NewLine + base.Description;
            }
        }
        public override string ShortName
        {
            get
            {
                return (this.BaseEvent != null) ? this.BaseEvent.GUIName : "none";
            }
        }
         
        public override bool Pop(FlightComputer f)
        {
            if (this.BaseEvent != null)
                this.BaseEvent.Invoke();
            
            return false;
        }

        public static EventCommand Event(BaseEvent ev)
        {
            return new EventCommand()
            {
                BaseEvent = ev,
                TimeStamp = RTUtil.GameTime,
            };
        }

        /// <summary>
        /// Load infos into this object and create a new BaseEvent
        /// </summary>
        public override void Load(ConfigNode n, FlightComputer fc)
        {
            base.Load(n, fc);

            // deprecated since 1.6.2, we need this for upgrading from 1.6.x => 1.6.2
            int PartId = 0;
            {
                if (n.HasValue("PartId"))
                    PartId = int.Parse(n.GetValue("PartId"));
            }

            if (n.HasValue("flightID"))
                this.flightID = uint.Parse(n.GetValue("flightID"));

            Module = n.GetValue("Module");
            GUIName = n.GetValue("GUIName");

            RTLog.Notify("Try to load an EventCommand from persistent with {0},{1},{2},{3}", PartId, flightID, Module, GUIName);

            Part part = null;
            var partlist = FlightGlobals.ActiveVessel.parts;

            if (this.flightID == 0)
            {
                // only look with the partid if we've enough parts
                if (PartId < partlist.Count)
                    part = partlist.ElementAt(PartId);
            }
            else
            {
                part = partlist.Where(p => p.flightID == this.flightID).FirstOrDefault();
            }

            if (part != null)
            {
                PartModule partmodule = part.Modules[Module];
                if (partmodule != null)
                {
                    BaseEventList eventlist = new BaseEventList(part, partmodule);
                    if (eventlist.Count > 0)
                    {
                        this.BaseEvent = eventlist.Where(ba => ba.GUIName == this.GUIName).FirstOrDefault();
                    }
                }
            }
        }

        /// <summary>
        /// Save the BaseEvent to the persistent
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer fc)
        {
            GUIName = this.BaseEvent.GUIName;
            flightID = this.BaseEvent.listParent.module.part.flightID;
            Module = this.BaseEvent.listParent.module.ClassName.ToString();

            base.Save(n, fc);
        }
    }
}
