using System;
using System.Linq;
using System.Collections.Generic;

namespace RemoteTech
{
    public class NetworkPathfinder<T>
    {
        private class NodeInfo : IComparable<NodeInfo>
        {
            public T Node { get; private set; }
            public NetworkLink<T> From { get; private set; }
            public double CurrentCost { get; private set; }
            public bool Closed { get; set; }

            public NodeInfo(T node, NetworkLink<T> previous, double cost)
            {
                this.Node = node;
                this.From = previous;
                this.CurrentCost = cost;
            }

            public int CompareTo(NodeInfo other)
            {
                // 0.0 precedes 1.0.
                return CurrentCost.CompareTo(other.CurrentCost);
            }
        }

        public readonly Func<T, IEnumerable<NetworkLink<T>>> FindNeighbors;
        public readonly Func<T, T, double> CalculateCost;

        public NetworkPathfinder(Func<T, IEnumerable<NetworkLink<T>>> neighbors,
                                 Func<T, T, double> cost)
        {
            this.FindNeighbors = neighbors;
            this.CalculateCost = cost;
        }

        public IDictionary<T, NetworkRoute<T>> GenerateConnections(T commandStation)
        {
            var tree = GenerateMinimumSpanningTree(commandStation);
            var connections = new Dictionary<T, NetworkRoute<T>>();
            var linkMap = tree.ToDictionary(p => p.Key, p => p.Value.From);
            // Construct every path from leaf (satellite) to root (command station).
            foreach (var pair in tree)
            {
                var node = pair.Key;
                var info = pair.Value;
                if (pair.Key.Equals(commandStation)) continue;

                connections[node] = new NetworkRoute<T>(commandStation, node, linkMap, info.CurrentCost);
            }

            return connections;
        }
        private IDictionary<T, NodeInfo> GenerateMinimumSpanningTree(T commandStation)
        {
            var nodeMap = new Dictionary<T, NodeInfo>();
            var priorityQueue = new PriorityQueue<NodeInfo>();

            priorityQueue.Enqueue(nodeMap[commandStation] = new NodeInfo(commandStation, null, 0.0));

            // Continue to run as long there are nodes to process.
            while (priorityQueue.Count > 0)
            {
                // Process the node with the current minimum cost.
                var top = priorityQueue.Dequeue();
                if (top.Closed) continue;
                // No solutions if the minimum cost is infinity.
                if (top.CurrentCost == Double.PositiveInfinity)
                    break;

                // Parse connections to neighbors.
                foreach (var neighbor in FindNeighbors(top.Node))
                {
                    var cost = CalculateCost(top.Node, neighbor.B);
                    var alternativeCost = top.CurrentCost + cost;
                    var contains = nodeMap.ContainsKey(neighbor.B);
                    if (!contains || alternativeCost < nodeMap[neighbor.B].CurrentCost)
                    {
                        nodeMap[neighbor.B].Closed = contains;
                        priorityQueue.Enqueue(nodeMap[neighbor.B] = new NodeInfo(neighbor.B, neighbor, alternativeCost));
                    }
                }
            }

            return nodeMap;
        }
    }
}