namespace RemoteTech.Common.Interfaces.FlightComputer
{
    public interface IPIDController
    {
        Vector3d Kp { get; set; }
        Vector3d Ki { get; set; }
        Vector3d Kd { get; set; }
        Vector3d IntAccum { get; set; }

        Vector3d Compute(Vector3d error, Vector3d omega, Vector3d Wlimit);
        void Reset();
    }
}