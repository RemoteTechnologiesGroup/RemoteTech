using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace RemoteTech
{
    public class NetworkRoute<T> : IComparable<NetworkRoute<T>>
    {
        public T Goal { get { return Exists ? Links[Links.Count - 1].Target : default(T); } }
        public T Start { get; private set; }
        public bool Exists { get { return Links.Count > 0; } }

        public double Length { get; private set; }
        public double Delay { get { return RTSettings.Instance.EnableSignalDelay 
                    ? Length / RTSettings.Instance.SpeedOfLight 
                    : 0.0; } }
        public List<NetworkLink<T>> Links { get; private set; }

        public NetworkRoute(T start, List<NetworkLink<T>> links, double dist)
        {
            if (start == null) throw new ArgumentNullException("start");
            if (links == null) links = new List<NetworkLink<T>>();
            Start = start;
            Links = links;
            Length = dist;
        }

        public bool Contains(BidirectionalEdge<T> edge)
        {
            if (Links.Count == 0) return false;
            if ((Start.Equals(edge.A) && Links[0].Target.Equals(edge.B)) || 
                (Start.Equals(edge.B) && Links[0].Target.Equals(edge.A))) return true;
            for (int i = 0; i < Links.Count - 1; i++)
            {
                if (Links[i].Target.Equals(edge.A) && Links[i+1].Target.Equals(edge.B)) return true;
                if (Links[i].Target.Equals(edge.B) && Links[i+1].Target.Equals(edge.A)) return true;
            }
            return false;
        }

        public int CompareTo(NetworkRoute<T> other)
        {
            return Length.CompareTo(other.Length);
        }

        public override string ToString()
        {
            return String.Format("NetworkRoute(Route: {{0}}, Length: {1})", 
                String.Join("→", Links.Select(t => t.ToString()).ToArray()),
                Length.ToString("F2") + "m");
        }
    }

    public class NetworkRoute
    {
        public static NetworkRoute<T> Empty<T>(T start)
        {
            return new NetworkRoute<T>(start, null, Single.PositiveInfinity);
        }
    }
}