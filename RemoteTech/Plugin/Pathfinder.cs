using System;
using System.Collections.Generic;

namespace RemoteTech {
    public static class Pathfinder {
        public delegate float CostDelegate<T>(T a, T b);

        public delegate float HeuristicDelegate<T>(T a, T b);

        public delegate List<T> NeighbourDelegate<T>(T a);

        // All sorting related data is immutable.

        public static Path<T> Solve<T>(T start, T goal, NeighbourDelegate<T> neighboursFunction,
                                       CostDelegate<T> costFunction,
                                       HeuristicDelegate<T> heuristicFunction)
                where T : ISatellite {
            var nodeMap = new Dictionary<T, Node<T>>();
            var priorityQueue = new PriorityQueue<Node<T>>();

            var nStart = new Node<T>(start, 0, heuristicFunction.Invoke(start, goal), null, false);
            nodeMap[start] = nStart;
            priorityQueue.Enqueue(nStart);
            float cost = 0;

            while (priorityQueue.Count > 0) {
                Node<T> current = priorityQueue.Dequeue();
                if (current.Closed) continue;
                current.Closed = true;
                RTUtil.Log("Current: " + current.Item);
                if (current.Item.Equals(goal)) {
                    // Return path and cost
                    var reversePath = new List<T>();
                    for (Node<T> node = current; node != null; node = node.From) {
                        reversePath.Add(node.Item);
                        cost += node.Cost;
                    }
                    reversePath.Reverse();
                    return new Path<T>(reversePath, cost);
                }

                foreach (T item in neighboursFunction.Invoke(current.Item)) {
                    float new_cost = current.Cost + costFunction.Invoke(current.Item, item);
                    // If the item has a node, it will either be in the closedSet, or the openSet
                    if (nodeMap.ContainsKey(item)) {
                        Node<T> n = nodeMap[item];
                        if (new_cost <= n.Cost) {
                            // Cost via current is better than the old one, discard old node, queue new one.
                            var new_node = new Node<T>(n.Item, new_cost, n.Heuristic, current, false);
                            n.Closed = true;
                            nodeMap[item] = new_node;
                            priorityQueue.Enqueue(new_node);
                        }
                    }
                    else {
                        // It is not in the openSet, create a node and add it
                        var new_node = new Node<T>(item, new_cost,
                                                   heuristicFunction.Invoke(item, goal), current,
                                                   false);
                        priorityQueue.Enqueue(new_node);
                        nodeMap[item] = new_node;
                    }
                }
            }
            return Path.Empty<T>();
        }

        public class Node<T> : IComparable<Node<T>> {
            public Node<T> From { get; set; }
            public bool Closed { get; set; }

            public int CompareTo(Node<T> node) {
                return (Cost + Heuristic).CompareTo(node.Cost + node.Heuristic);
            }

            public readonly float Cost;
            public readonly float Heuristic;
            public readonly T Item;

            public Node(T item, float cost, float heuristic, Node<T> from, bool closed) {
                Item = item;
                Cost = cost;
                Heuristic = heuristic;
                From = from;
                Closed = closed;
            }
        }
    }
}
