using System;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Interfaces.FlightComputer;
using RemoteTech.Common.Utils;
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

        public override string Description
        {
            get
            {
                return ((BaseEvent != null) ? BaseEvent.listParent.part.partInfo.title + ": " + GUIName : "none") +
                        Environment.NewLine + base.Description;
            }
        }
        public override string ShortName => (BaseEvent != null) ? BaseEvent.GUIName : "none";

        public override bool Pop(IFlightComputer f)
        {
            if (BaseEvent == null)
                return false;

            try
            {
                // invoke the base event
                BaseEvent.Invoke();
            }
            catch (Exception invokeException)
            {
                RTLog.Notify("BaseEvent invokeException by '{0}' with message: {1}",
                    RTLogLevel.LVL1, BaseEvent.guiName, invokeException.Message);
            }

            return false;
        }

        public static EventCommand Event(BaseEvent ev)
        {
            return new EventCommand()
            {
                BaseEvent = ev,
                GUIName = ev.GUIName,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        /// <summary>
        /// Load infos into this object and create a new BaseEvent.
        /// </summary>
        /// <returns>true if loaded successfully.</returns>
        public override bool Load(ConfigNode n, IFlightComputer fc)
        {
            if (!base.Load(n, fc))
                return false;

            //TODO remove the part of the code that is really deprecated now
            // deprecated since 1.6.2, we need this for upgrading from 1.6.x => 1.6.2
            var partId = 0;
            {
                if (n.HasValue("PartId"))
                    partId = int.Parse(n.GetValue("PartId"));
            }

            if (n.HasValue("flightID"))
                flightID = uint.Parse(n.GetValue("flightID"));

            Module = n.GetValue("Module");
            GUIName = n.GetValue("GUIName");
            Name = n.GetValue("Name");

            RTLog.Notify("Try to load an EventCommand from persistent with {0},{1},{2},{3},{4}",
                partId, flightID, Module, GUIName, Name);

            Part part = null;
            var partlist = FlightGlobals.ActiveVessel.parts;

            if (flightID == 0)
            {
                // only look with the partid if we've enough parts
                if (partId < partlist.Count)
                    part = partlist.ElementAt(partId);
            }
            else
            {
                part = partlist.FirstOrDefault(p => p.flightID == flightID);
            }

            if (part == null) return false;

            var partmodule = part.Modules[Module];
            if (partmodule == null) return false;

            var eventlist = new BaseEventList(part, partmodule);
            if (eventlist.Count <= 0) return false;

            BaseEvent = eventlist.FirstOrDefault(ba => (ba.GUIName == GUIName || ba.name == Name));
            return true;
        }

        /// <summary>
        /// Save the BaseEvent to the persistent
        /// </summary>
        public override void Save(ConfigNode n, IFlightComputer fc)
        {
            GUIName = BaseEvent.GUIName;
            flightID = BaseEvent.listParent.module.part.flightID;
            Module = BaseEvent.listParent.module.ClassName.ToString();
            Name = BaseEvent.name;

            base.Save(n, fc);
        }
    }
}
