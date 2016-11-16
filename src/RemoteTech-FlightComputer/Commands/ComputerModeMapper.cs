using RemoteTech.FlightComputer.UI;

namespace RemoteTech.FlightComputer.Commands
{
    /// <summary>
    /// This class converts the FlightMode from a AttitudeCommand into a ComputerMode for the AttitudeFragment 
    /// </summary>
    public class ComputerModeMapper
    {
        public ComputerMode computerMode;
        public FlightAttitude computerAttitude;

        public void mapFlightMode(FlightMode flightMode, FlightAttitude flightAttitude, ReferenceFrame frame)
        {
            computerMode = ComputerMode.Off;
            computerAttitude = flightAttitude;

            switch (flightMode)
            {
                case FlightMode.Off: { computerMode = ComputerMode.Off; break; }
                case FlightMode.KillRot: { computerMode = ComputerMode.Kill; break; }
                case FlightMode.AttitudeHold:
                {
                    computerMode = ComputerMode.Custom;
                    switch (frame)
                    {
                        case ReferenceFrame.Maneuver: { computerMode = ComputerMode.Node; break; }
                        case ReferenceFrame.Orbit: { computerMode = ComputerMode.Orbital; break; }
                        case ReferenceFrame.Surface: { computerMode = ComputerMode.Surface; break; }
                        case ReferenceFrame.TargetParallel: { computerMode = ComputerMode.TargetPos; break; }
                        case ReferenceFrame.TargetVelocity: { computerMode = ComputerMode.TargetVel; break; }
                    }
                    break;
                }
            }
        }
    }
}
