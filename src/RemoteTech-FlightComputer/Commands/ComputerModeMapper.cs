using RemoteTech.FlightComputer.UI;

namespace RemoteTech.FlightComputer.Commands
{
    /// <summary>
    /// This class converts the FlightMode from a AttitudeCommand into a ComputerMode for the AttitudeFragment 
    /// </summary>
    public class ComputerModeMapper
    {
        public ComputerMode ComputerMode;
        public FlightAttitude ComputerAttitude;

        public void MapFlightMode(FlightMode flightMode, FlightAttitude flightAttitude, ReferenceFrame frame)
        {
            ComputerMode = ComputerMode.Off;
            ComputerAttitude = flightAttitude;

            switch (flightMode)
            {
                case FlightMode.Off: { ComputerMode = ComputerMode.Off; break; }
                case FlightMode.KillRot: { ComputerMode = ComputerMode.Kill; break; }
                case FlightMode.AttitudeHold:
                {
                    ComputerMode = ComputerMode.Custom;
                    switch (frame)
                    {
                        case ReferenceFrame.Maneuver: { ComputerMode = ComputerMode.Node; break; }
                        case ReferenceFrame.Orbit: { ComputerMode = ComputerMode.Orbital; break; }
                        case ReferenceFrame.Surface: { ComputerMode = ComputerMode.Surface; break; }
                        case ReferenceFrame.TargetParallel: { ComputerMode = ComputerMode.TargetPos; break; }
                        case ReferenceFrame.TargetVelocity: { ComputerMode = ComputerMode.TargetVel; break; }
                                //TODO: North, World, default
                    }
                    break;
                }
            }
        }
    }
}
