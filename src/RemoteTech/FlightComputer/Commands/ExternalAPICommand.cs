using System;
using System.Reflection;

namespace RemoteTech.FlightComputer.Commands
{
    public class ExternalAPICommand : AbstractCommand
    {
        /// <summary>original ConfigNode object passed from the api</summary>
        private ConfigNode externalData;
        /// <summary>Name of the mod who passed this command</summary>
        private string Executor;
        /// <summary>Label for this command on the queue</summary>
        private string QueueLabel;
        /// <summary>Label for this command if its active</summary>
        private string ActiveLabel;
        /// <summary>Label for alert on no power</summary>
        private string ShortLabel;
        /// <summary>The ReflectionType for the methods to invoke</summary>
        private string ReflectionType;
        /// <summary>Name of the Pop-method on the ReflectionType</summary>
        private string ReflectionPopMethod = "";
        /// <summary>Name of the Execution-method on the ReflectionType</summary>
        private string ReflectionExecuteMethod = "";
        /// <summary>Name of the Abort-method on the ReflectionType</summary>
        private string ReflectionAbortMethod = "";
        /// <summary>GUID of the vessel</summary>
        private string GUIDString;
        /// <summary>true - when this command will be aborted</summary>
        private bool AbortCommand = false;

        public override int Priority { get { return 0; } }

        /// <summary>
        /// Returns the <see cref="QueueLabel"/> of this command on the queue and the
        /// <see cref="ActiveLabel"/> if this command is active. If no one is defined
        /// the <see cref="Executor"/> will be returned.
        /// </summary>
        public override string Description
        {
            get
            {
                var desc = (this.QueueLabel == "") ? this.Executor : this.QueueLabel;

                if(this.Delay <= 0 && this.ExtraDelay <= 0)
                    desc = (this.ActiveLabel == "") ? this.Executor : this.ActiveLabel;

                return desc + Environment.NewLine + base.Description;
            }
        }

        /// <summary>
        /// Returns the <see cref="ShortLabel"/> of this command. If no <see cref="ShortLabel"/>
        /// is defined the name of the <see cref="Executor"/> will be returned.
        /// </summary>
        public override string ShortName
        {
            get { return (this.ShortLabel == "") ? this.Executor : this.ShortLabel; }
        }

        /// <summary>
        /// Pops the command and invokes the <see cref="ReflectionPopMethod"/> on the
        /// <see cref="ReflectionType"/>.
        /// </summary>
        /// <param name="computer">Current flightcomputer</param>
        /// <returns>true: move to active.</returns>
        public override bool Pop(FlightComputer computer)
        {
            bool moveToActive = false;

            if (this.ReflectionPopMethod != "")
            {
                // invoke the Pop-method
                moveToActive = (bool)this.callReflectionMember(this.ReflectionPopMethod);

                // set moveToActive to false if we've no Execute-method.
                if (this.ReflectionExecuteMethod == "") moveToActive = false;
            }

            return moveToActive;
        }

        /// <summary>
        /// Executes this command and invokes the <see cref="ReflectionExecuteMethod"/>
        /// on the <see cref="ReflectionType"/>. When this command is aborted the
        /// fallback commnd is "Off".
        /// </summary>
        /// <param name="computer">Current flightcomputer</param>
        /// <param name="ctrlState">Current FlightCtrlState</param>
        /// <returns>true: delete afterwards.</returns>
        public override bool Execute(FlightComputer computer, FlightCtrlState ctrlState)
        {
            bool finished = true;

            if (this.ReflectionExecuteMethod != "")
            {
                // invoke the Execution-method
                finished = (bool)this.callReflectionMember(this.ReflectionExecuteMethod);
                if (this.AbortCommand)
                {
                    // disable FC after aborting this command
                    computer.Enqueue(AttitudeCommand.Off(), true, true, true);
                    finished = true;
                }
            }

            return finished;
        }

        /// <summary>
        /// Aborts the active external command and invokes the
        /// <see cref="ReflectionAbortMethod"/> on the <see cref="ReflectionType"/>. 
        /// </summary>
        public override void Abort()
        {
            this.AbortCommand = true;

            if (this.ReflectionAbortMethod != "")
            {
                this.callReflectionMember(this.ReflectionAbortMethod);
            }
        }

        /// <summary>
        /// Configures an ExternalAPICommand.
        /// </summary>
        /// <param name="externalData">Data passed by the Api.QueueCommandToFlightComputer</param>
        /// <returns>Configured ExternalAPICommand</returns>
        public static ExternalAPICommand FromExternal(ConfigNode externalData)
        {
            ExternalAPICommand command =  new ExternalAPICommand();
            command.TimeStamp = RTUtil.GameTime;
            command.ConfigNodeToObject(command,externalData);

            return command;
        }

        /// <summary>
        /// Maps the ConfigNode object passed from the api or loading to an ExternalAPICommand.
        /// </summary>
        /// <param name="command">Map the data to this object</param>
        /// <param name="data">Data to map onto the command</param>
        private void ConfigNodeToObject(ExternalAPICommand command, ConfigNode data)
        {
            command.externalData = new ConfigNode("ExternalData").AddNode(data);
            command.Executor = data.GetValue("Executor");
            command.ReflectionType = data.GetValue("ReflectionType");
            command.GUIDString = data.GetValue("GUIDString");

            if (data.HasValue("QueueLabel"))
                command.QueueLabel = data.GetValue("QueueLabel");
            if (data.HasValue("ActiveLabel"))
                command.ActiveLabel = data.GetValue("ActiveLabel");
            if (data.HasValue("ShortLabel"))
                command.ShortLabel = data.GetValue("ShortLabel");

            if (data.HasValue("ReflectionPopMethod"))
                command.ReflectionPopMethod = data.GetValue("ReflectionPopMethod");
            if (data.HasValue("ReflectionExecuteMethod"))
                command.ReflectionExecuteMethod = data.GetValue("ReflectionExecuteMethod");
            if (data.HasValue("ReflectionAbortMethod"))
                command.ReflectionAbortMethod = data.GetValue("ReflectionAbortMethod");
        }

        /// <summary>
        /// Saves the original configNode <see cref="externalData"/> passed from the api
        /// to the persistent
        /// </summary>
        /// <param name="node">Node with the command infos to save in</param>
        /// <param name="computer">Current flightcomputer</param>
        public override void Save(ConfigNode node, FlightComputer computer)
        {
            base.Save(node, computer);

            node.AddNode("ExternalData");
            node.SetNode("ExternalData", this.externalData);
        }

        /// <summary>
        /// Loads the ExternalAPICommand from the persistent file. If the loading
        /// can't find the <see cref="ReflectionType"/> we'll notify a ScreenMessage
        /// and remove the command.
        /// </summary>
        /// <param name="node">Node with the command infos to load</param>
        /// <param name="computer">Current flightcomputer</param>
        /// <returns>true - loaded successfull</returns>
        public override bool Load(ConfigNode node, FlightComputer computer)
        {
            try
            {
                if(base.Load(node, computer))
                {
                    if (node.HasNode("ExternalData"))
                    {
                        this.ConfigNodeToObject(this, node.GetNode("ExternalData"));
                        // try loading the reflectionType
                        this.getReflectionType(this.ReflectionType);
                        return true;
                    }
                }
            }
            catch(Exception)
            {
                RTUtil.ScreenMessage(string.Format("Mod '{0}' not found. Queued command '{1}' will be removed.", this.Executor, this.ShortName));
            }
            return false;
        }

        /// <summary>
        /// Prepare the data to pass back to the mod.
        /// </summary>
        /// <returns>ConfigNode to pass over to the reflection methods</returns>
        private ConfigNode prepareDataForExternalMod()
        {
            ConfigNode externalData = new ConfigNode();
            this.externalData.CopyTo(externalData);
            externalData.AddValue("AbortCommand",this.AbortCommand);

            return externalData;
        }

        /// <summary>
        /// Calls the rflection method.
        /// </summary>
        /// <param name="reflectionMember">Name of the reflection method</param>
        /// <returns>Object from the invoked reflection method</returns>
        private object callReflectionMember(string reflectionMember)
        {
            Type externalType = this.getReflectionType(this.ReflectionType);
            var result = externalType.InvokeMember(reflectionMember, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new System.Object[] { this.prepareDataForExternalMod() });
            return result;
        }

        /// <summary>
        /// Trys to load the reflectionType. Throws an exception if we can't
        /// find the type.
        /// </summary>
        /// <param name="reflectionType">Type to load</param>
        /// <returns>loaded type</returns>
        private Type getReflectionType(string reflectionType)
        {
            Type type = Type.GetType(reflectionType);
            if (type == null) throw new NullReferenceException();

            return type;
        }
        
    }
}