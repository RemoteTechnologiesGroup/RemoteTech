using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class CancelCommand : AbstractCommand
    {
        public override double ExtraDelay { get { return base.ExtraDelay; } set { return; } }
        public ICommand Command { get; set; }

        public override string Description { get { return "Cancelling a command." + Environment.NewLine + base.Description; } }

        public override bool Pop(FlightComputer f)
        {
            f.Remove(Command);
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
    }
}
