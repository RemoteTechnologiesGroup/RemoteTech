using System;
using System.Reflection;

namespace RemoteTech.FlightComputer.Commands
{
    public class ExternalAPICommand : AbstractCommand
    {
        /// <summary>Name of the mod who passed this command</summary>
        [Persistent] private string Executor;
        /// <summary>Label for this command on the queue</summary>
        [Persistent] private string QueueLabel;
        /// <summary>Label for this command if its active</summary>
        [Persistent] private string ActiveLabel;
        /// <summary>Label for alert on no power</summary>
        [Persistent] private string ShortLabel;
        /// <summary>The ReflectionType for the methods to invoke</summary>
        [Persistent] private string ReflectionType;
        /// <summary>Name of the Pop-method on the ReflectionType</summary>
        [Persistent] private string ReflectionPopMethod = "";
        /// <summary>Name of the Execution-method on the ReflectionType</summary>
        [Persistent] private string ReflectionExecuteMethod = "";
        /// <summary>Name of the Abort-method on the ReflectionType</summary>
        [Persistent] private string ReflectionAbortMethod = "";
        /// <summary>GUID of the vessel</summary>
        [Persistent] private string GUIDString;
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
        /// fallback commnd is "KillRot".
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
                    // enqueue killRot after aborting this command
                    computer.Enqueue(AttitudeCommand.KillRot(), true, true, true);
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
            command.Executor = externalData.GetValue("Executor");
            command.ReflectionType = externalData.GetValue("ReflectionType");
            command.GUIDString = externalData.GetValue("GUIDString");

            if (externalData.HasValue("QueueLabel"))
                command.QueueLabel = externalData.GetValue("QueueLabel");
            if (externalData.HasValue("ActiveLabel"))
                command.ActiveLabel = externalData.GetValue("ActiveLabel");
            if (externalData.HasValue("ShortLabel"))
                command.ShortLabel = externalData.GetValue("ShortLabel");


            if (externalData.HasValue("ReflectionPopMethod"))
                command.ReflectionPopMethod = externalData.GetValue("ReflectionPopMethod");
            if (externalData.HasValue("ReflectionExecuteMethod"))
                command.ReflectionExecuteMethod = externalData.GetValue("ReflectionExecuteMethod");
            if (externalData.HasValue("ReflectionAbortMethod"))
                command.ReflectionAbortMethod = externalData.GetValue("ReflectionAbortMethod");

            return command;
        }

        /// <summary>
        /// Loads the ExternalAPICommand from the persistent file. If the loading
        /// can't find the <see cref="ReflectionType"/> we'll notify a ScreenMessage
        /// and remove the command.
        /// </summary>
        /// <param name="n">Node with the command infos</param>
        /// <param name="computer">Current flightcomputer</param>
        /// <returns>true - loaded successfull</returns>
        public override bool Load(ConfigNode n, FlightComputer computer)
        {
            try
            {
                if(base.Load(n, computer))
                {
                    // try loading the reflectionType
                    this.getReflectionType(this.ReflectionType);
                    return true;
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
            ConfigNode externalData = ConfigNode.CreateConfigFromObject(this);
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