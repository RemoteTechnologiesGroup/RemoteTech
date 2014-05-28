using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace RemoteTech
{
    public class NetworkRoute<T> : IComparable<NetworkRoute<T>>
    {
        public readonly T Start;
        public readonly T Goal;
        public readonly double Cost;
        public readonly IDictionary<T, NetworkLink<T>> Links;

        public double SignalDelay { get { return Cost / RTSettings.Instance.SpeedOfLight; } }
        public NetworkRoute(T a, T b, IDictionary<T, NetworkLink<T>> links, double cost)
        {
            this.Start = a;
            this.Goal = b;
            this.Cost = cost;
            this.Links = new ReadOnlyDictionary<T, NetworkLink<T>>(links);
        }

        public bool Contains(UnorderedPair<T> e)
        {
            return Links.Any(l => l.Equals(e));
        }

        public int CompareTo(NetworkRoute<T> other)
        {
            return Cost.CompareTo(other.Cost);
        }

        public override string ToString()
        {
            return String.Format("NetworkRoute(A: {0}, B: {1}, Hops: {2})", Start, Goal, Links.Count);
        }
    }
}