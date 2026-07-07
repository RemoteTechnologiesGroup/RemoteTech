using System;
using System.Collections.Generic;
using RemoteTech.Network;

namespace RemoteTech.SimpleTypes
{
    /// <summary>
    /// A connection route, as a value-type view: a lightweight handle into the
    /// persistent <see cref="NetworkState"/>. <see cref="Length"/>/<see cref="Goal"/>/
    /// <see cref="Exists"/> are answered O(1) from the Dijkstra forests, and the
    /// hop list is materialized lazily — and cached per tick by the state — only
    /// when <see cref="Links"/> is actually read.
    /// </summary>
    public readonly struct NetworkRoute<T> : IComparable<NetworkRoute<T>>
    {
        private readonly NetworkState _state;
        private readonly int _node;
        private readonly bool _groundOnly;

        internal NetworkRoute(NetworkState state, int node, bool groundOnly)
        {
            _state = state;
            _node = node;
            _groundOnly = groundOnly;
        }

        public T Start => (T)(object)_state.SatAt(_node);

        public double Length => _state.RouteLength(_node, _groundOnly);

        public bool Exists => _state.RouteExists(_node, _groundOnly);

        public double Delay => RTSettings.Instance.EnableSignalDelay
            ? Length / RTSettings.Instance.SpeedOfLight
            : 0.0;

        public IReadOnlyList<NetworkLink<T>> Links =>
            (IReadOnlyList<NetworkLink<T>>)(object)_state.RouteHops(_node, _groundOnly);

        public T Goal => Exists ? (T)(object)_state.RouteGoalSat(_node, _groundOnly) : default;

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
}
