using System;
using System.Text;
using System.Collections.Generic;

using KSP.IO;


namespace RemoteTech
{
    public static class Pathfinder {
        public delegate List<T> NeighbourDelegate<T>(T a);
        public delegate float CostDelegate<T>(T a,T b);
        public delegate float HeuristicDelegate<T>(T a,T b);

        // All sorting related data is immutable.
        public class Node<T> : IComparable<Node<T>> {
            public readonly T Item;
            public readonly float Cost;
            public readonly float Heuristic;
            public Node<T> From { get; set; }
            public bool Closed { get; set; }

            public Node(T item, float cost, float heuristic, Node<T> from, bool closed) {
                this.Item = item;
                this.Cost = cost;
                this.Heuristic = heuristic;
                this.From = from;
                this.Closed = closed;
            }

            public int CompareTo(Node<T> node) {
                return (Cost + Heuristic).CompareTo(node.Cost + node.Heuristic);
            }

        }

        public static Pair<List<T>, float> Solve<T>(T start, T goal, NeighbourDelegate<T> neighboursFunction,
                                                                       CostDelegate<T> costFunction,
                                                                       HeuristicDelegate<T> heuristicFunction) {

            Dictionary<T, Node<T>> nodeMap = new Dictionary<T, Node<T>>();
            PriorityQueue<Node<T>> priorityQueue = new PriorityQueue<Node<T>>();

            Node<T> nStart = new Node<T>(start, 0, heuristicFunction(start, goal), null, false);
            nodeMap[start] = nStart;
            priorityQueue.Enqueue(nStart);
            float cost = 0;

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
                    return new Pair<List<T>, float>(reversePath, cost);
                }

                foreach (T item in neighboursFunction(current.Item)) {
                    float new_cost = current.Cost + costFunction(current.Item, item);
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
            return new Pair<List<T>, float>(new List<T>(), Single.PositiveInfinity);
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

                List<T> neighbours = neighboursFunction(current);
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

