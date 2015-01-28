using System;
using System.Linq;

using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.FlightComputer
{
    public class EventCommand : AbstractCommand
    {
        // Guiname of the BaseEvent
        [Persistent] public string GUIName;
        // Name of the BaseEvent
        [Persistent] public string Name;
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
                return ((this.BaseEvent != null) ? this.BaseEvent.listParent.part.partInfo.title + ": " + this.GUIName : "none") +
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
                GUIName = ev.GUIName,
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
                        
            this.Module = n.GetValue("Module");
            this.GUIName = n.GetValue("GUIName");
            this.Name = n.GetValue("Name");

            RTLog.Notify("Try to load an EventCommand from persistent with {0},{1},{2},{3},{4}",
                         PartId, this.flightID, this.Module, this.GUIName, this.Name);

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
                        this.BaseEvent = eventlist.Where(ba => (ba.GUIName == this.GUIName || ba.name == this.Name)).FirstOrDefault();
                    }
                }
            }
        }

        /// <summary>
        /// Save the BaseEvent to the persistent
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer fc)
        {
            this.GUIName = this.BaseEvent.GUIName;
            this.flightID = this.BaseEvent.listParent.module.part.flightID;
            this.Module = this.BaseEvent.listParent.module.ClassName.ToString();
            this.Name = this.BaseEvent.name;

            base.Save(n, fc);
        }
    }
}
