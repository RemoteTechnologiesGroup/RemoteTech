using System;

namespace RemoteTech.Common.Interfaces.FlightComputer.Commands
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

        bool Pop(IFlightComputer f);
        bool Execute(IFlightComputer f, FlightCtrlState fcs);
        void Abort();
        /// 
        void Save(ConfigNode n, IFlightComputer fc);
        bool Load(ConfigNode n, IFlightComputer fc);
        ///
        void CommandEnqueued(IFlightComputer computer);
        void CommandCanceled(IFlightComputer computer);
    }
}
