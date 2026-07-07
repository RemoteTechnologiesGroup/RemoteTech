using System;
using System.Collections.Generic;
using RemoteTech.Network;

namespace RemoteTech.SimpleTypes
{
    /// <summary>
    /// A connection route, as a value-type view. In <b>view mode</b> the route is
    /// a lightweight handle into the persistent <see cref="NetworkState"/>
    /// (<c>_state != null</c>): <see cref="Length"/>/<see cref="Goal"/>/
    /// <see cref="Exists"/> are answered O(1) from the Dijkstra forests, and the
    /// hop list is materialized lazily — and cached per tick by the state — only
    /// when <see cref="Links"/> is actually read. In <b>explicit mode</b>
    /// (<c>_state == null</c>) it wraps a freestanding path, e.g. the result of the
    /// A* <see cref="NetworkPathfinder"/> or <see cref="NetworkRoute.Empty{T}"/>.
    /// </summary>
    public readonly struct NetworkRoute<T> : IComparable<NetworkRoute<T>>
    {
        // View mode.
        private readonly NetworkState _state;
        private readonly int _node;
        private readonly bool _groundOnly;

        // Explicit mode.
        private readonly T _start;
        private readonly IReadOnlyList<NetworkLink<T>> _links;
        private readonly double _length;

        private static readonly NetworkLink<T>[] NoLinks = new NetworkLink<T>[0];

        public NetworkRoute(T start, IReadOnlyList<NetworkLink<T>> links, double dist)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            _state = null;
            _node = -1;
            _groundOnly = false;
            _start = start;
            _links = links;
            _length = dist;
        }

        internal NetworkRoute(NetworkState state, int node, bool groundOnly)
        {
            _state = state;
            _node = node;
            _groundOnly = groundOnly;
            _start = default;
            _links = null;
            _length = 0.0;
        }

        public T Start => _state != null ? (T)(object)_state.SatAt(_node) : _start;

        public double Length => _state != null ? _state.RouteLength(_node, _groundOnly) : _length;

        public bool Exists => _state != null
            ? _state.RouteExists(_node, _groundOnly)
            : _links != null && _links.Count > 0;

        public double Delay => RTSettings.Instance.EnableSignalDelay
            ? Length / RTSettings.Instance.SpeedOfLight
            : 0.0;

        public IReadOnlyList<NetworkLink<T>> Links => _state != null
            ? (IReadOnlyList<NetworkLink<T>>)(object)_state.RouteHops(_node, _groundOnly)
            : _links ?? NoLinks;

        public T Goal
        {
            get
            {
                if (!Exists) return default;
                if (_state != null) return (T)(object)_state.RouteGoalSat(_node, _groundOnly);
                return _links[_links.Count - 1].Target;
            }
        }

        public bool Contains(BidirectionalEdge<T> edge)
        {
            var links = Links;
            if (links.Count == 0) return false;
            T start = Start;
            if ((start.Equals(edge.A) && links[0].Target.Equals(edge.B)) ||
                (start.Equals(edge.B) && links[0].Target.Equals(edge.A))) return true;
            for (int i = 0; i < links.Count - 1; i++)
            {
                if (links[i].Target.Equals(edge.A) && links[i + 1].Target.Equals(edge.B)) return true;
                if (links[i].Target.Equals(edge.B) && links[i + 1].Target.Equals(edge.A)) return true;
            }
            return false;
        }

        public int CompareTo(NetworkRoute<T> other) => Length.CompareTo(other.Length);

        public override string ToString()
        {
            var links = Links;
            var parts = new string[links.Count];
            for (int i = 0; i < links.Count; i++) parts[i] = links[i].ToString();
            return String.Format("NetworkRoute(Route: {0}, Length: {1})",
                String.Join("→", parts), Length.ToString("F2") + "m");
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
