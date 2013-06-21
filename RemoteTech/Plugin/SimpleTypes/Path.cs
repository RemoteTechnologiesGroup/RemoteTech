using System;
using System.Collections.Generic;

namespace RemoteTech {
    public class Path<T> where T : ISatellite {
        public T Goal { get { return Exists ? Nodes[Nodes.Count - 1] : default(T); } }
        public T Start { get { return Nodes[0]; } }
        public bool Exists { get { return Nodes != null && Nodes.Count > 1; } }

        public float Delay { get; private set; }
        public Path.Type State { get; private set; }
        public List<T> Nodes { get; private set; }

        public Path(List<T> nodes, float cost) {
            if (nodes == null || nodes.Count == 0) throw new ArgumentException();
            Nodes = nodes;
            Delay = cost / RTCore.Instance.Settings.SIGNAL_SPEED;
            if (Exists) {
                State = Path.Type.Connected;
            } else if (Start.LocalControl) {
                State = Path.Type.LocalControl;
            } else {
                State = Path.Type.NotConnected;
            }
        }
    }

    public class Path {
        public enum Type {
            NotConnected,
            LocalControl,
            Connected,
        }

        public static Path<T> Empty<T>(T start) where T : ISatellite {
            List<T> nodes = new List<T>();
            nodes.Add(start);
            return new Path<T>(nodes, Single.PositiveInfinity);
        }
    }
}
