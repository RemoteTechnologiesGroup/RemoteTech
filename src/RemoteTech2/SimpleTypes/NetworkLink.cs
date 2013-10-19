using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public class NetworkLink<T>
    {
        public readonly T Target;
        public readonly List<IAntenna> Interfaces;
        public readonly LinkType Port;

        public NetworkLink(T sat, List<IAntenna> ant, LinkType port)
        {
            Target = sat;
            Interfaces = ant;
            Port = port;
        }

        public override string ToString()
        {
            return String.Format("NetworkLink(T: {0}, I: {1}, P: {2})", Target, Interfaces, Port);
        }
    }
}