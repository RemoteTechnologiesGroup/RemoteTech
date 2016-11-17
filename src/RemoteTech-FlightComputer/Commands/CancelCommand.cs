using System;
using System.Linq;
using RemoteTech.Common;
using RemoteTech.Common.Utils;

namespace RemoteTech.FlightComputer.Commands
{
    public class CancelCommand : AbstractCommand
    {
        public override double ExtraDelay { get { return base.ExtraDelay; } set { return; } }
        private Guid _cancelCmdGuid;

        public override string Description => "Canceling a command." + Environment.NewLine + base.Description;
        public override string ShortName => "Cancel command";

        public override bool Pop(FlightComputer computer)
        {
            if (_cancelCmdGuid != Guid.Empty)
            {
                CancelQueuedCommand(_cancelCmdGuid, computer);
            }
            else
            {
                // we've no CancelCmdGuid for an active command. But
                // maybe we'll use this later
                CancelActiveCommand(_cancelCmdGuid, computer);
            }

            return false;
        }

        public static CancelCommand WithCommand(ICommand cmd)
        {
            
            return new CancelCommand()
            {
                _cancelCmdGuid = cmd.CmdGuid,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static CancelCommand ResetActive()
        {
            return new CancelCommand()
            {
                _cancelCmdGuid = Guid.Empty,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        /// <summary>
        /// Load the saved CancelCommand and find the element to cancel, based on the saved queue position
        /// </summary>
        /// <returns>true - loaded successful</returns>
        public override bool Load(ConfigNode n, FlightComputer computer)
        {
            if (!base.Load(n, computer))
                return false;

            if (n.HasValue("CancelCmdGuid"))
            {
                _cancelCmdGuid = new Guid(n.GetValue("CancelCmdGuid"));
            }

            // old way to cancel a command
            if (n.HasValue("queueIndex"))
            {
                try
                {
                    var queueIndex = int.Parse(n.GetValue("queueIndex"));
                    // try to find the command to cancel
                    _cancelCmdGuid = computer.QueuedCommands.ElementAt(queueIndex).CmdGuid;
                }
                catch (Exception ex)
                {
                    RTLog.Notify($"CancelCommand.Load(): An exception occurred while trying to find the command: {ex}");
                }
            }

            // loaded successfully?
            return _cancelCmdGuid != Guid.Empty;
        }

        /// <summary>
        /// Saves the queue index for this command to the persist
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer computer)
        {
            base.Save(n, computer);
            n.AddValue("CancelCmdGuid", _cancelCmdGuid);
        }

        /// <summary>
        /// Cancels a queued command by it's <see cref="Guid"/>.
        /// </summary>
        /// <param name="cmdGuid"><see cref="Guid"/> for the command to cancel</param>
        /// <param name="computer">Current FlightComputer</param>
        /// <returns>True if we canceled the command, false otherwise.</returns>
        private static bool CancelQueuedCommand(Guid cmdGuid, FlightComputer computer)
        {
            var searchCmd = computer.QueuedCommands.FirstOrDefault(cmd => cmd.CmdGuid == cmdGuid);
            if (searchCmd == null)
                return false;

            searchCmd.CommandCanceled(computer);
            computer.Remove(searchCmd);
            return true;
        }

        /// <summary>
        /// Cancels the current active command.
        /// </summary>
        /// <param name="cmdGuid">Unused right now</param>
        /// <param name="computer">Current FlightComputer</param>
        /// <returns></returns>
        private static bool CancelActiveCommand(Guid cmdGuid, FlightComputer computer)
        {
            computer.Reset();

            return true;
        }
    }
}
