using System;
using System.Text;
using System.Linq;

using RemoteTech.FlightComputer.Commands;
using static RemoteTech.FlightComputer.UIPartActionMenuPatcher;

namespace RemoteTech.FlightComputer
{
    
    public class PartActionCommand : AbstractCommand
    {
        // Guiname of the BaseEvent
        [Persistent]
        public string GUIName;
        // Name of the BaseEvent
        [Persistent]
        public string Name;
        // flight id of the part by this BaseEvent
        [Persistent]
        public uint flightID;
        // PartModule of the part by this BaseEvent
        [Persistent]
        public string Module;
        // BaseEvent to invoke
        public BaseField BaseField = null;

        public override string Description
        {
            get
            {
                var sb = new StringBuilder();
                if (BaseField != null)
                {
                    sb.Append(BaseField.guiName + ": " + BaseField.name);
                }
                else
                {
                    sb.Append("none");
                }
                sb.Append(Environment.NewLine + base.Description);
                return sb.ToString();
            }
        }
        public override string ShortName
        {
            get
            {
                return (BaseField != null) ? BaseField.guiName : "none";
            }
        }

        public override bool Pop(FlightComputer f)
        {
            if (BaseField != null)
            {
                try
                {
                    var field = (BaseField as WrappedField);
                    if (field != null)
                    {
                        // invoke the field value change 
                        field.Invoke();
                    }
                }
                catch (Exception invokeException)
                {
                    RTLog.Notify("BaseField InvokeAction() by '{0}' with message: {1}",
                                 RTLogLevel.LVL1, this.BaseField.guiName, invokeException.Message);
                }
            }

            return false;
        }

        public static PartActionCommand Field(BaseField baseField)
        {
            return new PartActionCommand()
            {
                BaseField = baseField,
                GUIName = baseField.guiName,
                TimeStamp = RTUtil.GameTime,
            };
        }

        /*
        /// <summary>
        /// Load infos into this object and create a new BaseEvent
        /// </summary>
        /// <returns>true - loaded successfull</returns>
        public override bool Load(ConfigNode n, FlightComputer fc)
        {
            if (base.Load(n, fc))
            {
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

                if (part == null) return false;

                PartModule partmodule = part.Modules[Module];
                if (partmodule == null) return false;

                BaseEventList eventlist = new BaseEventList(part, partmodule);
                if (eventlist.Count <= 0) return false;

                this.BaseField = eventlist.Where(ba => (ba.GUIName == this.GUIName || ba.name == this.Name)).FirstOrDefault();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Save the BaseEvent to the persistent
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer fc)
        {
            this.GUIName = this.BaseField.GUIName;
            this.flightID = this.BaseField.listParent.module.part.flightID;
            this.Module = this.BaseField.listParent.module.ClassName.ToString();
            this.Name = this.BaseField.name;

            base.Save(n, fc);
        }*/
    }
    
}
