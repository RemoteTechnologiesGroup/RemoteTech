using System;

namespace RemoteTech.FlightComputer.Commands
{
    public interface ICommand : IComparable<ICommand>
    {
        double TimeStamp { get; set; }
        double ExtraDelayScheduledTimeStamp { get; set; } //required for precise time operations
        double ExtraDelay { get; set; }
        Guid CmdGuid { get; }
        double Delay { get; }
        // The command description displayed in the flight computer
        String Description { get; }
        // An abbreviated version of the description for inline inclusion in messages
        String ShortName { get; }
        int Priority { get; }

        bool Pop(FlightComputer f);
        bool Execute(FlightComputer f, FlightCtrlState fcs);
        void Abort();
        /// 
        void Save(ConfigNode n, FlightComputer fc);
        bool Load(ConfigNode n, FlightComputer fc);
        ///
        void CommandEnqueued(FlightComputer computer);
        void CommandCanceled(FlightComputer computer);
    }
}
