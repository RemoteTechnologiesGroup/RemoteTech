using System;

namespace RemoteTech
{
    public interface IControlStation : ISatellite {

        bool Active { get; }
    }
}

