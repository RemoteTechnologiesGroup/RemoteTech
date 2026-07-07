using System;
using System.Collections.Generic;

namespace RemoteTech.SimpleTypes
{
    /// <summary>
    /// A single directed hop in the network: the satellite reached
    /// (<see cref="Target"/>) and the port type. A value-type view — it carries
    /// no state of its own beyond these, so handing one out costs no heap
    /// allocation.
    /// </summary>
    public readonly struct NetworkLink<T> : IEquatable<NetworkLink<T>>
    {
        public readonly T Target;
        public readonly LinkType Port;

        public NetworkLink(T sat, LinkType port)
        {
            Target = sat;
            Port = port;
        }

        public bool Equals(NetworkLink<T> o)
        {
            return EqualityComparer<T>.Default.Equals(Target, o.Target);
        }

        public override bool Equals(object obj) => obj is NetworkLink<T> o && Equals(o);

        public override int GetHashCode() => Target == null ? 0 : Target.GetHashCode();

        public override string ToString()
        {
            return String.Format("NetworkLink(T: {0}, P: {1})", Target, Port);
        }
    }
}
