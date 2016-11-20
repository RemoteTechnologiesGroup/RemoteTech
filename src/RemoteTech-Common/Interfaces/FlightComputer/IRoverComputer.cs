namespace RemoteTech.Common.Interfaces.FlightComputer
{
    public interface IRoverComputer
    {
        float Delta { get; }
        float DeltaT { get; }
        void SetVessel(Vessel v);
        void InitMode(Commands.DriveCommand dc);
        bool Drive(Commands.DriveCommand dc, FlightCtrlState fs);
    }
}