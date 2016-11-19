using System;
using System.Reflection;
using RemoteTech.Common.Settings;
using RemoteTech.Common.Utils;
using RemoteTech.FlightComputer.Settings;

namespace RemoteTech.FlightComputer.Commands
{
    public class ExternalAPICommand : AbstractCommand
    {
        /// <summary>original ConfigNode object passed from the API</summary>
        private ConfigNode _externalData;
        /// <summary>Name of the mod who passed this command</summary>
        private string _executor;
        /// <summary>Label for this command on the queue</summary>
        private string _queueLabel;
        /// <summary>Label for this command if its active</summary>
        private string _activeLabel;
        /// <summary>Label for alert on no power</summary>
        private string _shortLabel;
        /// <summary>The ReflectionType for the methods to invoke</summary>
        private string _reflectionType;
        /// <summary>Name of the Pop-method on the ReflectionType</summary>
        private string _reflectionPopMethod = "";
        /// <summary>Name of the Execution-method on the ReflectionType</summary>
        private string _reflectionExecuteMethod = "";
        /// <summary>Name of the Abort-method on the ReflectionType</summary>
        private string _reflectionAbortMethod = "";
        /// <summary>GUID of the vessel</summary>
        private string _guidString;
        /// <summary>true - when this command will be aborted</summary>
        private bool _abortCommand;

        private FlightComputerSettings FcSettingsInstance => FlightComputerSettingsManager.Instance;

        public override int Priority => 0;

        /// <summary>
        /// Returns the <see cref="_queueLabel"/> of this command on the queue and the
        /// <see cref="_activeLabel"/> if this command is active. If no one is defined
        /// the <see cref="_executor"/> will be returned.
        /// </summary>
        public override string Description
        {
            get
            {
                var desc = (_queueLabel == "") ? _executor : _queueLabel;

                if(Delay <= 0 && ExtraDelay <= 0)
                    desc = (_activeLabel == "") ? _executor : _activeLabel;

                return desc + Environment.NewLine + base.Description;
            }
        }

        /// <summary>
        /// Returns the <see cref="_shortLabel"/> of this command. If no <see cref="_shortLabel"/>
        /// is defined the name of the <see cref="_executor"/> will be returned.
        /// </summary>
        public override string ShortName => (_shortLabel == "") ? _executor : _shortLabel;

        /// <summary>
        /// Pops the command and invokes the <see cref="_reflectionPopMethod"/> on the
        /// <see cref="_reflectionType"/>.
        /// </summary>
        /// <param name="computer">Current FlightComputer</param>
        /// <returns>true: move to active.</returns>
        public override bool Pop(FlightComputer computer)
        {
            var moveToActive = false;

            if (_reflectionPopMethod != "")
            {
                // invoke the Pop-method
                moveToActive = (bool)CallReflectionMember(_reflectionPopMethod);

                // set moveToActive to false if we've no Execute-method.
                if (_reflectionExecuteMethod == "") moveToActive = false;
            }

            return moveToActive;
        }

        /// <summary>
        /// Executes this command and invokes the <see cref="_reflectionExecuteMethod"/>
        /// on the <see cref="_reflectionType"/>. When this command is aborted the
        /// fall-back command is "KillRot".
        /// </summary>
        /// <param name="computer">Current FlightComputer.</param>
        /// <param name="ctrlState">Current FlightCtrlState</param>
        /// <returns>true: delete afterwards.</returns>
        public override bool Execute(FlightComputer computer, FlightCtrlState ctrlState)
        {
            var finished = true;

            if (_reflectionExecuteMethod != "")
            {
                // invoke the Execution-method
                finished = (bool)CallReflectionMember(_reflectionExecuteMethod);
                if (_abortCommand)
                {
                    if (FcSettingsInstance.FCOffAfterExecute)
                    {
                        computer.Enqueue(AttitudeCommand.Off(), true, true, true);
                        finished = true;
                    }
                    if (!FcSettingsInstance.FCOffAfterExecute)
                    {
                        computer.Enqueue(AttitudeCommand.KillRot(), true, true, true);
                        finished = true;
                    }
                }
            }

            return finished;
        }

        /// <summary>
        /// Aborts the active external command and invokes the
        /// <see cref="_reflectionAbortMethod"/> on the <see cref="_reflectionType"/>. 
        /// </summary>
        public override void Abort()
        {
            _abortCommand = true;

            if (_reflectionAbortMethod != "")
            {
                CallReflectionMember(_reflectionAbortMethod);
            }
        }

        /// <summary>
        /// Configures an ExternalAPICommand.
        /// </summary>
        /// <param name="externalData">Data passed by the Api.QueueCommandToFlightComputer</param>
        /// <returns>Configured ExternalAPICommand</returns>
        public static ExternalAPICommand FromExternal(ConfigNode externalData)
        {
            var command = new ExternalAPICommand {TimeStamp = TimeUtil.GameTime};
            command.ConfigNodeToObject(command,externalData);

            return command;
        }

        /// <summary>
        /// Maps the ConfigNode object passed from the API or loading to an ExternalAPICommand.
        /// </summary>
        /// <param name="command">Map the data to this object</param>
        /// <param name="data">Data to map onto the command</param>
        private void ConfigNodeToObject(ExternalAPICommand command, ConfigNode data)
        {
            command._externalData = new ConfigNode("ExternalData").AddNode(data);
            command._executor = data.GetValue("Executor");
            command._reflectionType = data.GetValue("ReflectionType");
            command._guidString = data.GetValue("GUIDString");

            if (data.HasValue("QueueLabel"))
                command._queueLabel = data.GetValue("QueueLabel");
            if (data.HasValue("ActiveLabel"))
                command._activeLabel = data.GetValue("ActiveLabel");
            if (data.HasValue("ShortLabel"))
                command._shortLabel = data.GetValue("ShortLabel");

            if (data.HasValue("ReflectionPopMethod"))
                command._reflectionPopMethod = data.GetValue("ReflectionPopMethod");
            if (data.HasValue("ReflectionExecuteMethod"))
                command._reflectionExecuteMethod = data.GetValue("ReflectionExecuteMethod");
            if (data.HasValue("ReflectionAbortMethod"))
                command._reflectionAbortMethod = data.GetValue("ReflectionAbortMethod");
        }

        /// <summary>
        /// Saves the original configNode <see cref="_externalData"/> passed from the API.
        /// to the persistent
        /// </summary>
        /// <param name="node">Node with the command infos to save in</param>
        /// <param name="computer">Current FlightComputer</param>
        public override void Save(ConfigNode node, FlightComputer computer)
        {
            base.Save(node, computer);

            node.AddNode("ExternalData");
            node.SetNode("ExternalData", _externalData);
        }

        /// <summary>
        /// Loads the ExternalAPICommand from the persistent file. If the loading
        /// can't find the <see cref="_reflectionType"/> we'll notify a ScreenMessage
        /// and remove the command.
        /// </summary>
        /// <param name="node">Node with the command infos to load</param>
        /// <param name="computer">Current FlightComputer</param>
        /// <returns>true if loaded successfully, false otherwise.</returns>
        public override bool Load(ConfigNode node, FlightComputer computer)
        {
            try
            {
                if(base.Load(node, computer))
                {
                    if (node.HasNode("ExternalData"))
                    {
                        ConfigNodeToObject(this, node.GetNode("ExternalData"));
                        // try loading the reflectionType
                        getReflectionType(_reflectionType);
                        return true;
                    }
                }
            }
            catch(Exception)
            {
                GuiUtil.ScreenMessage($"Mod '{_executor}' not found. Queued command '{ShortName}' will be removed.");
            }
            return false;
        }

        /// <summary>
        /// Prepare the data to pass back to the mod.
        /// </summary>
        /// <returns>ConfigNode to pass over to the reflection methods</returns>
        private ConfigNode PrepareDataForExternalMod()
        {
            var externalData = new ConfigNode();
            _externalData.CopyTo(externalData);
            externalData.AddValue("AbortCommand",_abortCommand);

            return externalData;
        }

        /// <summary>
        /// Calls the reflection method.
        /// </summary>
        /// <param name="reflectionMember">Name of the reflection method</param>
        /// <returns>Object from the invoked reflection method</returns>
        private object CallReflectionMember(string reflectionMember)
        {
            var externalType = getReflectionType(_reflectionType);
            var result = externalType.InvokeMember(reflectionMember, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { PrepareDataForExternalMod() });
            return result;
        }

        /// <summary>
        /// Try to load the reflectionType. Throws an exception if we can't
        /// find the type.
        /// </summary>
        /// <param name="reflectionType">Type to load</param>
        /// <returns>loaded type</returns>
        private Type getReflectionType(string reflectionType)
        {
            var type = Type.GetType(reflectionType);
            if (type == null) throw new NullReferenceException();

            return type;
        }
    }
}
