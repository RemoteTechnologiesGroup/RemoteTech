using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class EventCommand : AbstractCommand
    {
        public BaseEvent BaseEvent { get; private set; }

        public override String Description
        {
            get
            {
                return BaseEvent.listParent.part.partInfo.title + ": " +
                       BaseEvent.GUIName + Environment.NewLine + base.Description;
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
                TimeStamp = RTUtil.GameTime,
            };
        }
    }
}
