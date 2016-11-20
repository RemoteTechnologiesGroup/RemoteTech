using RemoteTech.Common.Interfaces.FlightComputer.Commands;

namespace RemoteTech.Common.Interfaces.FlightComputer
{
    public interface IRoverComputer
    {
        float Delta { get; }
        float DeltaT { get; }
        void SetVessel(Vessel v);
        void InitMode(IDriveCommand dc);
        bool Drive(IDriveCommand dc, FlightCtrlState fs);
    }
}