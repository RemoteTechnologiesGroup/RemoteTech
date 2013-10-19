using System;

namespace RemoteTech
{
    public class NetworkLink<T> : IEquatable<NetworkLink<T>>
    {
        public readonly T Target;
        public readonly IAntenna Interface;
        public readonly LinkType Port;

        public NetworkLink(T sat, IAntenna ant, LinkType port)
        {
            Target = sat;
            Interface = ant;
            Port = port;
        }

        public bool Equals(NetworkLink<T> other)
        {
            return Target.Equals(other.Target) && Interface.Equals(other.Interface);
        }

        public override string ToString()
        {
            return String.Format("NetworkLink(T: {0}, I: {1}, P: {2})", Target, Interface, Port);
        }
    }
}