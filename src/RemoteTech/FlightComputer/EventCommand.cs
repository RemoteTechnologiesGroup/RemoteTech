using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class EventCommand : AbstractCommand
    {
        public class FakeEventFromLoad
        {
            public string Name;
            public string GUIName;
            public string PartInfo;
            public int PartId;

            public void Invoke()
            {
                BaseEvent b = FlightGlobals.ActiveVessel.parts.ElementAt(PartId).Events.Where(e => e.GUIName == GUIName).FirstOrDefault();
                if (b != null)
                    b.Invoke();
            }
        };

        public BaseEvent BaseEvent;
        public FakeEventFromLoad loadedEvent;

        public override String Description
        {
            get
            {
                string desc = "";

                if (loadedEvent != null)
                {
                    desc = loadedEvent.PartInfo + ": " + loadedEvent.GUIName;
                }
                else
                {
                    desc = BaseEvent.listParent.part.partInfo.title + ": " + BaseEvent.GUIName;
                }

                desc += Environment.NewLine + base.Description;

                return desc;

            }
        }
        public override string ShortName
        {
            get
            {
                if (loadedEvent != null)
                {
                    return loadedEvent.GUIName;
                }
                return BaseEvent.GUIName;
            }
        }
         
        public override bool Pop(FlightComputer f)
        {
            RTLog.Notify("############ BaseEvent Debug ##########");
            var partid = FlightGlobals.ActiveVessel.parts.ToList().IndexOf(BaseEvent.listParent.part);
            RTLog.Notify("PartId: {0}",partid);
            Part part = FlightGlobals.ActiveVessel.parts[partid];
            RTLog.Notify("partname: {0}", part.name);
            RTLog.Notify("----- Actions ----");
            foreach(var a in part.Actions){
                RTLog.Notify("- Action: ",a.name);
            }
            RTLog.Notify("----- Events ----");
            foreach (var e in part.Events)
            {
                RTLog.Notify("- Event: ", e.name);
                RTLog.Notify("- Event: ", e.GUIName);
            }

            if (loadedEvent != null)
            {
                loadedEvent.Invoke();
            }
            else
            {
                //BaseEvent.Invoke();
            }
            
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

        public override void Load(ConfigNode n, FlightComputer fc)
        {
            base.Load(n, fc);
            var bn = n.GetNode("BaseEvent");

            var fe = new FakeEventFromLoad();
            fe.Name = bn.GetValue("Name");
            fe.PartInfo = bn.GetValue("PartInfo");
            fe.PartId = int.Parse(bn.GetValue("PartId"));
            fe.GUIName = bn.GetValue("GuiName");

            loadedEvent = fe;
        }

        public override void Save(ConfigNode n, FlightComputer fc)
        {
            base.Save(n, fc);
            var bn = this.getCommandConfigNode(n).AddNode("BaseEvent");

            bn.AddValue("Name", BaseEvent.name);
            bn.AddValue("GuiName", BaseEvent.GUIName);
            bn.AddValue("PartInfo", BaseEvent.listParent.part.partInfo.title);
            int partid = FlightGlobals.ActiveVessel.parts.ToList().IndexOf(BaseEvent.listParent.part);
            bn.AddValue("PartId", partid);
        }
    }
}
