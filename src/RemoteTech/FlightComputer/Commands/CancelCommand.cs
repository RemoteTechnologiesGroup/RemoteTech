using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class CancelCommand : AbstractCommand
    {
        public override double ExtraDelay { get { return base.ExtraDelay; } set { return; } }
        public ICommand Command;
        [Persistent] public int queueIndex;

        public override string Description { get { return "Cancelling a command." + Environment.NewLine + base.Description; } }
        public override string ShortName { get { return "Cancel command"; } }

        public override bool Pop(FlightComputer f)
        {
            if (Command == null)
            {
                f.Reset();
            }
            else
            {
                f.Remove(Command);
            }
            return false;
        }

        public static CancelCommand WithCommand(ICommand cmd)
        {
            
            return new CancelCommand()
            {
                Command = cmd,
                TimeStamp = RTUtil.GameTime,
            };
        }

        public static CancelCommand ResetActive()
        {
            return new CancelCommand()
            {
                Command = null,
                TimeStamp = RTUtil.GameTime,
            };
        }

        /// <summary>
        /// Load the saved CancelCommand and find the element to cancel, based on the saved queue position
        /// </summary>
        public override void Load(ConfigNode n, FlightComputer fc)
        {
            base.Load(n, fc);
            Command = fc.QueuedCommands.ElementAt(queueIndex);
        }

        /// <summary>
        /// Saves the queue index for this command to the persist
        /// </summary>
        public override void Save(ConfigNode n, FlightComputer fc)
        {
            queueIndex = fc.QueuedCommands.ToList().IndexOf(Command);
            base.Save(n, fc);
        }
    }
}
