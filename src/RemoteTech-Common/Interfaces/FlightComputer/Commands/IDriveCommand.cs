
namespace RemoteTech.Common.Interfaces.FlightComputer.Commands
{
    public interface IDriveCommand : ICommand
    {
        float Steering { get; }
        float Target { get; }
        float Target2 { get; }
        float Speed { get; }
        DriveMode Mode { get; set; }
    }

    public enum DriveMode
    {
        Off,
        Turn,
        Distance,
        DistanceHeading,
        Coord
    }
}
