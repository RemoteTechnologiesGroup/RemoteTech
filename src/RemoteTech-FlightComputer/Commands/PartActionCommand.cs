using System;
using System.Text;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Utils;
using static RemoteTech.FlightComputer.UIPartActionMenuPatcher;

namespace RemoteTech.FlightComputer.Commands
{
    
    public class PartActionCommand : AbstractCommand
    {
        // GUI name of the BaseField
        [Persistent]
        public string GUIName;
        // Name of the BaseField
        [Persistent]
        public string Name;
        // flight id of the part
        [Persistent]
        public uint flightID;
        // PartModule of the part
        [Persistent]
        public string Module;

        // new value for the field
        public object NewValue;
        // the value as a string, once loaded from configuration file
        public string NewValueString;
        // the original BaseField
        public BaseField BaseField;
        


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
        public override string ShortName => (BaseField != null) ? BaseField.guiName : "none";

        public override bool Pop(FlightComputer f)
        {
            if (BaseField == null)
                return false;

            try
            {
                var field = (BaseField as WrappedField);
                if (field == null) // we lost the Wrapped field instance, this is due to the fact that the command was loaded from a save
                {                        
                    if (NewValue != null)
                    {
                        var newfield = new WrappedField(BaseField, WrappedField.KspFieldFromBaseField(BaseField));
                        if(newfield.NewValueFromString(NewValueString))
                        {
                            newfield.Invoke();
                        }
                    }
                }
                else
                {
                    // invoke the field value change 
                    field.Invoke();
                }

                if (UIPartActionController.Instance != null)
                    UIPartActionController.Instance.UpdateFlight();
            }
            catch (Exception invokeException)
            {
                RTLog.Notify("BaseField InvokeAction() by '{0}' with message: {1}",
                    RTLogLevel.LVL1, BaseField.guiName, invokeException.Message);
            }

            return false;
        }

        public static PartActionCommand Field(BaseField baseField, object newValue)
        {
            return new PartActionCommand()
            {
                BaseField = baseField,
                GUIName = baseField.guiName,
                TimeStamp = TimeUtil.GameTime,
                NewValue = newValue
            };
        }

        /// <summary>
        /// Load infos into this object and create a new BaseEvent
        /// </summary>
        /// <returns>true if loaded successfully, false otherwise.</returns>
        public override bool Load(ConfigNode n, FlightComputer fc)
        {
            if (!base.Load(n, fc))
                return false;


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
            NewValueString = n.GetValue("NewValue");

            RTLog.Notify("Try to load an PartActionCommand from persistent with {0},{1},{2},{3},{4}",
                partId, flightID, Module, GUIName, Name);

            Part part = null;
            var partlist = FlightGlobals.ActiveVessel.parts;

            if (flightID == 0)
            {
                // only look with the part ID if we've enough parts
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

            var fieldList = new BaseFieldList(partmodule);
            if (fieldList.Count <= 0) return false;

            BaseField = fieldList[Name];
            return (BaseField != null);
        }

        /// <summary>
        /// Save the BaseEvent to the persistent
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer fc)
        {
            var pm = (BaseField.host as PartModule);
            if(pm == null)
            {
                RTLog.Notify("On PartActionCommand.Save(): Can't save because BaseField.host is not a PartModule instance. Type is: {0}", BaseField.host.GetType());
                return;
            }

            GUIName = BaseField.guiName;
            flightID = pm.part.flightID;
            Module = pm.ClassName;
            Name = BaseField.name;

            n.AddValue("NewValue", NewValue);

            base.Save(n, fc);
        }
    }    
}
