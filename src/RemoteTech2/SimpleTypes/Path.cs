using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace RemoteTech
{
    public class Path<T> : IComparable<Path<T>> where T : class
    {
        public const float SIGNAL_SPEED = 3.0e8f;

        public T Goal { get { return Exists ? Nodes[Nodes.Count - 1] : default(T); } }
        public T Start { get { return Nodes[0]; } }
        public bool Exists { get { return Nodes.Count > 1; } }

        public float Delay { get; private set; }
        public List<T> Nodes { get; private set; }

        public Path(List<T> nodes, float cost)
        {
            if (nodes == null || nodes.Count == 0) throw new ArgumentException("List of nodes cannot be null/empty");
            Nodes = nodes;
            Delay = cost / SIGNAL_SPEED;
        }

        public int CompareTo(Path<T> other)
        {
            return Delay.CompareTo(other.Delay);
        }

        public override string ToString()
        {
            return String.Format("Path(Route: {{0}}, Delay: {1})", 
                String.Join("→", Nodes.Select(t => t.ToString()).ToArray()),
                Delay.ToString("F2") + "s");
        }
    }

    public class Path
    {
        public static Path<T> Empty<T>(T start) where T : class
        {
            List<T> nodes = new List<T>();
            nodes.Add(start);
            return new Path<T>(nodes, Single.PositiveInfinity);
        }
    }
}