using System;
using System.Collections.Generic;

namespace RemoteTech {
    public class Path<T> where T : ISatellite {
        public T Goal { get { return Exists ? Nodes[Nodes.Count - 1] : default(T); } }
        public T Start { get { return Exists ? Nodes[0] : default(T); } }
        public bool Exists { get { return Nodes != null && Nodes.Count > 0; } }
        public float Delay { get { return Cost/RTCore.Instance.Settings.SIGNAL_SPEED; } }

        public Path.Type State { get; private set; }
        public float Cost { get; private set; }
        public List<T> Nodes { get; private set; }

        public Path(List<T> nodes, float cost) {
            Nodes = nodes;
            Cost = cost;
            if (Start != null && Goal != null) {
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

        public static Path<T> Empty<T>() where T : ISatellite {
            return new Path<T>(new List<T>(), Single.PositiveInfinity);
        }
    }
}
