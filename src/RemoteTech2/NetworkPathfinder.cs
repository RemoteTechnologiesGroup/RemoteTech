using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public static class NetworkPathfinder
    {
        // All sorting related data is immutable.

        public static NetworkRoute<T> Solve<T>(T start, T goal, 
                                       Func<T, IEnumerable<NetworkLink<T>>> neighborsFunction,
                                       Func<T, NetworkLink<T>, double> costFunction,
                                       Func<T, T, double> heuristicFunction) where T : class
        {
            var nodeMap = new Dictionary<T, Node<NetworkLink<T>>>();
            var priorityQueue = new PriorityQueue<Node<NetworkLink<T>>>();

            var nStart = new Node<NetworkLink<T>>(new NetworkLink<T>(start, null, LinkType.None), 0, heuristicFunction.Invoke(start, goal), null, false);
            nodeMap[start] = nStart;
            priorityQueue.Enqueue(nStart);
            double cost = 0;

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();
                if (current.Closed) continue;
                current.Closed = true;
                if (current.Item.Target.Equals(goal))
                {
                    // Return path and cost
                    var reversePath = new List<NetworkLink<T>>();
                    for (var node = current; node.From != null; node = node.From)
                    {
                        reversePath.Add(node.Item);
                        cost += node.Cost;
                    }
                    reversePath.Reverse();
                    return new NetworkRoute<T>(start, reversePath, cost);
                }

                foreach (var link in neighborsFunction.Invoke(current.Item.Target))
                {
                    double new_cost = current.Cost + costFunction.Invoke(current.Item.Target, link);
                    // If the item has a node, it will either be in the closedSet, or the openSet
                    if (nodeMap.ContainsKey(link.Target))
                    {
                        Node<NetworkLink<T>> n = nodeMap[link.Target];
                        if (new_cost <= n.Cost)
                        {
                            // Cost via current is better than the old one, discard old node, queue new one.
                            var new_node = new Node<NetworkLink<T>>(n.Item, new_cost, n.Heuristic, current, false);
                            n.Closed = true;
                            nodeMap[link.Target] = new_node;
                            priorityQueue.Enqueue(new_node);
                        }
                    }
                    else
                    {
                        // It is not in the openSet, create a node and add it
                        var new_node = new Node<NetworkLink<T>>(link, new_cost,
                                                   heuristicFunction.Invoke(link.Target, goal), current,
                                                   false);
                        priorityQueue.Enqueue(new_node);
                        nodeMap[link.Target] = new_node;
                    }
                }
            }
            return NetworkRoute.Empty<T>(start);
        }

        private class Node<T> : IComparable<Node<T>>
        {
            public Node<T> From { get; set; }
            public bool Closed { get; set; }

            public int CompareTo(Node<T> node)
            {
                return (Cost + Heuristic).CompareTo(node.Cost + node.Heuristic);
            }

            public readonly double Cost;
            public readonly double Heuristic;
            public readonly T Item;

            public Node(T item, double cost, double heuristic, Node<T> from, bool closed)
            {
                Item = item;
                Cost = cost;
                Heuristic = heuristic;
                From = from;
                Closed = closed;
            }
        }
    }
}