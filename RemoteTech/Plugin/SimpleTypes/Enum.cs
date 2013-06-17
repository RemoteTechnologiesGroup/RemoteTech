using System;

namespace RemoteTech {
    [Flags]
    public enum GraphMode {
        None = 0x00,
        MapView = 0x01,
        TrackingStation = 0x02,
    }

    [Flags]
    public enum EdgeType {
        None = 0x00,
        Omni = 0x01,
        Dish = 0x02,
        Connection = 0x04,
    }

    public enum Attitude {
        KillRot,
        Prograde,
        Retrograde,
        NormalPlus,
        NormalMinus,
        RadialPlus,
        RadialMinus,
        ManeuverNode,
        Surface,
    }
}
