using System;
using System.Text;
using System.Collections.Generic;

using KSP.IO;


namespace RemoteTech
{
    public static class Pathfinder {
        public delegate IList<T> NeighbourDelegate<T>(T a);
        public delegate double CostDelegate<T>(T a,T b);
        public delegate double HeuristicDelegate<T>(T a,T b);

        // All sorting related data is immutable.
        public class Node<T> : IComparable {
            public readonly T Item;
            public readonly double Cost;
            public readonly double Heuristic;
            public Node<T> From { get; set; }
            public bool Closed { get; set; }

            public Node(T item, double cost, double heuristic, Node<T> from, bool closed) {
                this.Item = item;
                this.Cost = cost;
                this.Heuristic = heuristic;
                this.From = from;
                this.Closed = closed;
            }

            public int CompareTo(Object obj) {
                if (obj == null) {
                    return 1;
                }
                Node<T> node = obj as Node<T>;
                if (node == null) {
                    throw new ArgumentException();
                }
                return (Cost + Heuristic).CompareTo(node.Cost + node.Heuristic);
            }

        }

        public static Pair<IList<T>, double> Solve<T>(T start, T goal, NeighbourDelegate<T> neighboursFunction,
                                                                       CostDelegate<T> costFunction,
                                                                       HeuristicDelegate<T> heuristicFunction) {

            Dictionary<T, Node<T>> nodeMap = new Dictionary<T, Node<T>>();
            PriorityQueue<Node<T>> priorityQueue = new PriorityQueue<Node<T>>();

            Node<T> nStart = new Node<T>(start, 0, heuristicFunction(start, goal), null, false);
            nodeMap[start] = nStart;
            priorityQueue.Enqueue(nStart);
            double cost = 0;

            while (priorityQueue.Count > 0) {
                Node<T> current = priorityQueue.Dequeue();
                if (current.Closed == true)
                    continue;
                current.Closed = true;
                RTUtil.Log("Current: " + current.Item.ToString());
                if (current.Item.Equals(goal)) {
                    // Return path and cost
                    List<T> reversePath = new List<T>();
                    for (Node<T> node = current; node != null; node = node.From) {
                        reversePath.Add(node.Item);
                        cost += node.Cost;
                    }
                    reversePath.Reverse();
                    return new Pair<IList<T>, double>(reversePath, cost);
                }

                foreach (T item in neighboursFunction(current.Item)) {
                    double new_cost = current.Cost + costFunction(current.Item, item);
                    // If the item has a node, it will either be in the closedSet, or the openSet
                    if (nodeMap.ContainsKey(item)) {
                        Node<T> n = nodeMap[item];
                        if (new_cost <= n.Cost) {
                            // Cost via current is better than the old one, discard old node, queue new one.
                            Node<T> new_node = new Node<T>(n.Item, new_cost, n.Heuristic, current, false);
                            n.Closed = true;
                            nodeMap[item] = new_node;
                            priorityQueue.Enqueue(new_node);
                        }
                    } else {
                        // It is not in the openSet, create a node and add it
                        Node<T> new_node = new Node<T>(item, new_cost, heuristicFunction(item, goal), current, false);
                        priorityQueue.Enqueue(new_node);
                        nodeMap[item] = new_node;
                    }
                }
            }
            return new Pair<IList<T>, double>(new List<T>(), Double.PositiveInfinity);
        }

        public static void GenerateGraphDescription<T>(String fileName, T start, NeighbourDelegate<T> neighboursFunction,
                                                                          CostDelegate<T> costFunction) {
            StringBuilder dotGraph = new StringBuilder();
            dotGraph.AppendLine("graph RemoteTech {");

            HashSet<T> closed = new HashSet<T>();
            HashSet<T> open = new HashSet<T>();

            open.Add(start);

            while (open.Count > 0) {
                HashSet<T>.Enumerator it = open.GetEnumerator();
                it.MoveNext();
                T current = it.Current;
                open.Remove(current);
                closed.Add(current);

                IList<T> neighbours = neighboursFunction(current);
                foreach (T n in neighbours) {
                    dotGraph.Append('\t')
                        .Append("\"" + current.ToString() + "\"")
                            .Append("--")
                            .Append("\"" + n.ToString() + "\"")
                            .AppendLine(";");
                    if (!closed.Contains(n)) {
                        open.Add(n);
                    }
                }
            }
            
            dotGraph.AppendLine("}");

            //File.WriteAllText(dotGraph.ToString(), fileName, (Vessel)null);
        }
    }
}

