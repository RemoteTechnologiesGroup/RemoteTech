using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RemoteTech
{
    public enum LinkType
    {
        None,
        Dish,
        Omni,
    }
    public class NetworkLink<T> : UnorderedPair<T>
    {
        public readonly IList<IAntenna> InterfacesA;
        public readonly IList<IAntenna> InterfacesB;
        public readonly LinkType LinkType;

        public NetworkLink(T a, T b, IList<IAntenna> interfacesA, IList<IAntenna> interfacesB, LinkType linkType)
            : this(a, b, new ReadOnlyCollection<IAntenna>(interfacesA), new ReadOnlyCollection<IAntenna>(interfacesB), linkType) { }

        private NetworkLink(T a, T b, ReadOnlyCollection<IAntenna> interfacesA, ReadOnlyCollection<IAntenna> interfacesB, LinkType linkType)
            : base(a, b)
        {
            this.InterfacesA = interfacesA;
            this.InterfacesB = interfacesB;
            this.LinkType = linkType;
        }

        public NetworkLink<T> Invert()
        {
            return new NetworkLink<T>(B, A, (ReadOnlyCollection<IAntenna>)InterfacesB, (ReadOnlyCollection<IAntenna>)InterfacesA, LinkType);
        }

        public override string ToString()
        {
            return String.Format("NetworkLink(A: {0}, B: {1}, InterfacesA: {2}, InterfacesB: {3})", 
                A, B, InterfacesA, InterfacesB);
        }
    }

    public class NetworkLink
    {
        public static NetworkLink<T> Empty<T>(T a, T b)
        {
            return new NetworkLink<T>(a, b, new IAntenna[]{}, new IAntenna[]{}, LinkType.None);
        }
    }
}