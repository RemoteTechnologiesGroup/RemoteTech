using System;
using System.Collections.Generic;
using RemoteTech.Common;

namespace RemoteTech.SimpleTypes
{
    public class NetworkLink<T> : IEquatable<NetworkLink<T>>
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

        public bool Equals(NetworkLink<T> o)
        {
            if (o == null) return false;
            if (!Target.Equals(o.Target)) return false;
            return true;
        }

        public override string ToString()
        {
            return String.Format("NetworkLink(T: {0}, I: {1}, P: {2})", Target, Interfaces.ToDebugString(), Port);
        }
    }
}