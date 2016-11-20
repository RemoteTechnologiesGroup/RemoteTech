using System;

namespace RemoteTech.FlightComputer.Commands
{
    public interface ICommand : IComparable<ICommand>
    {
        double TimeStamp { get; set; }
        double ExtraDelay { get; set; }
        Guid CmdGuid { get; }
        double Delay { get; }
        // The command description displayed in the flight computer
        string Description { get; }
        // An abbreviated version of the description for in-line inclusion in messages
        string ShortName { get; }
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
