using System;

namespace RemoteTech
{
    public interface ISatellite {

        IAntenna[] Antennas { get; }
        Vector3d Position { get; }

        float FindMaxOmniRange();

        long Enqueue(AttitudeChange change);
        long Enqueue(TrottleChange change);

    }
}

