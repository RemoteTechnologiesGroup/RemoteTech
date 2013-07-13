using System;
using System.Collections.Generic;

namespace RemoteTech {
    public class Path<T> : IComparable<Path<T>> where T : ISatellite {
        public T Goal { get { return (Nodes.Count > 1) ? Nodes[Nodes.Count - 1] : default(T); } }
        public T Start { get { return Nodes[0]; } }
        public bool Exists { get { return Nodes.Count > 1 || RTCore.Instance.Settings.DebugAlwaysConnected; } }

        public float Delay { get; private set; }
        public List<T> Nodes { get; private set; }

        public Path(List<T> nodes, float cost) {
            if (nodes == null || nodes.Count == 0) throw new ArgumentException();
            Nodes = nodes;
            Delay = cost / RTCore.Instance.Network.SignalSpeed;
            Delay += RTCore.Instance.Settings.DebugOffsetDelay;
        }

        public int CompareTo(Path<T> other) {
            return Delay.CompareTo(other.Delay);
        }
    }

    public class Path {
        public static Path<T> Empty<T>(T start) where T : ISatellite {
            List<T> nodes = new List<T>();
            nodes.Add(start);
            return new Path<T>(nodes, Single.PositiveInfinity);
        }
    }
}
