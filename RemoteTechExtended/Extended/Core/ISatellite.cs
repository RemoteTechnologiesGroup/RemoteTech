using System;

namespace RemoteTech
{
    public interface ISatellite {

        String Name { get; }

        IAntenna[] Antennas { get; }
        Vector3d Position { get; }

        float FindMaxOmniRange();

        long Enqueue(AttitudeChange change);
        long Enqueue(TrottleChange change);

    }
}

