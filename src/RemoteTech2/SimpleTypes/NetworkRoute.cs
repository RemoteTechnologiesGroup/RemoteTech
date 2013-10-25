using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace RemoteTech
{
    public class NetworkRoute<T> : IComparable<NetworkRoute<T>>
    {
        public const float SIGNAL_SPEED = 3.0e8f;

        public T Goal { get { return Exists ? Links[Links.Count - 1].Target : default(T); } }
        public T Start { get; private set; }
        public bool Exists { get { return Links.Count > 0; } }

        public float Delay { get; private set; }
        public List<NetworkLink<T>> Links { get; private set; }

        public NetworkRoute(T start, List<NetworkLink<T>> links, float cost)
        {
            if (start == null) throw new ArgumentNullException("start");
            if (links == null) links = new List<NetworkLink<T>>();
            Start = start;
            Links = links;
            Delay = 0;// cost / SIGNAL_SPEED;
        }

        public int CompareTo(NetworkRoute<T> other)
        {
            return Delay.CompareTo(other.Delay);
        }

        public override string ToString()
        {
            return String.Format("NetworkRoute(Route: {{0}}, Delay: {1})", 
                String.Join("→", Links.Select(t => t.ToString()).ToArray()),
                Delay.ToString("F2") + "s");
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