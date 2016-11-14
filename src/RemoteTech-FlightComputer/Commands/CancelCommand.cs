using System;
using System.Linq;
using RemoteTech.Common.Utils;

namespace RemoteTech.FlightComputer.Commands
{
    public class CancelCommand : AbstractCommand
    {
        public override double ExtraDelay { get { return base.ExtraDelay; } set { return; } }
        private Guid CancelCmdGuid;

        public override string Description { get { return "Cancelling a command." + Environment.NewLine + base.Description; } }
        public override string ShortName { get { return "Cancel command"; } }

        public override bool Pop(FlightComputer computer)
        {
            if (this.CancelCmdGuid != Guid.Empty)
            {
                this.cancelQueuedCommand(this.CancelCmdGuid, computer);
            }
            else
            {
                // we've no CancelCmdGuid for an active command. But
                // maybe we'll use this later
                this.cancelActiveCommand(this.CancelCmdGuid, computer);
            }

            return false;
        }

        public static CancelCommand WithCommand(ICommand cmd)
        {
            
            return new CancelCommand()
            {
                CancelCmdGuid = cmd.CmdGuid,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        public static CancelCommand ResetActive()
        {
            return new CancelCommand()
            {
                CancelCmdGuid = Guid.Empty,
                TimeStamp = TimeUtil.GameTime,
            };
        }

        /// <summary>
        /// Load the saved CancelCommand and find the element to cancel, based on the saved queue position
        /// </summary>
        /// <returns>true - loaded successfull</returns>
        public override bool Load(ConfigNode n, FlightComputer computer)
        {
            if(base.Load(n, computer))
            {
                if (n.HasValue("CancelCmdGuid"))
                {
                    this.CancelCmdGuid = new Guid(n.GetValue("CancelCmdGuid"));
                }

                // old way to cancel a command
                if (n.HasValue("queueIndex"))
                {
                    try
                    {
                        int queueIndex = int.Parse(n.GetValue("queueIndex"));
                        // try to find the command to cancel
                        this.CancelCmdGuid = computer.QueuedCommands.ElementAt(queueIndex).CmdGuid;
                    }
                    catch (Exception)
                    { }
                }

                // loaded successfull
                if (this.CancelCmdGuid != Guid.Empty)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Saves the queue index for this command to the persist
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer computer)
        {
            base.Save(n, computer);
            n.AddValue("CancelCmdGuid", this.CancelCmdGuid);
        }

        /// <summary>
        /// Cancels a queued command by it's guid
        /// </summary>
        /// <param name="cmdGuid">Guid for the command to cancel</param>
        /// <param name="computer">Current flightcomputer</param>
        /// <returns>True if we canceld the command</returns>
        private bool cancelQueuedCommand(Guid cmdGuid, FlightComputer computer)
        {
            ICommand searchCmd = computer.QueuedCommands.Where(cmd => cmd.CmdGuid == cmdGuid).FirstOrDefault();
            if (searchCmd != null)
            {
                searchCmd.CommandCanceled(computer);
                computer.Remove(searchCmd);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancels the current active command.
        /// </summary>
        /// <param name="cmdGuid">Unused right now</param>
        /// <param name="computer">Current flightcomputer</param>
        /// <returns></returns>
        private bool cancelActiveCommand(Guid cmdGuid, FlightComputer computer)
        {
            computer.Reset();

            return true;
        }
    }
}
