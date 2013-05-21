using System;

namespace RemoteTech
{
    public interface ISatellite {

        String Name { get; }

        IAntenna[] Antennas { get; }
        Vector3d Position { get; }

        double FindMaxOmniRange();
        double IsPointingAt(ISatellite a);
    }
}

