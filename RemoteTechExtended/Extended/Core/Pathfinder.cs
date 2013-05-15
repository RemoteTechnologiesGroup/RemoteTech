using System;
using System.Text;
using System.Collections.Generic;

using KSP.IO;


namespace RemoteTech
{
    public static class Pathfinder {
        public delegate IList<T> NeighbourDelegate<T>(T a);
        public delegate float CostDelegate<T>(T a,T b);
        public delegate float HeuristicDelegate<T>(T a,T b);

        public enum Flag {
            Closed,
            Open,
        }

        public class Node<T> : IComparable {
            public T Item { get; private set; }
            public float Cost { get; set; }
            public float Heuristic { get; set; }
            public Node<T> From { get; set; }
            public Flag State { get; set; }

            public Node(T item, float cost, float heuristic, Node<T> from, Flag state) {
                this.Item = item;
                this.Cost = cost;
                this.Heuristic = heuristic;
                this.From = from;
                this.State = state;
            }

            public override bool Equals(Object obj) {
                if (obj == null) {
                    return false;
                }
                Node<T> node = obj as Node<T>;
                if (node == null) {
                    return false;
                }
                return Item.Equals(node.Item);
            }

            // Based solely on immutable Item, the rest of the parameters may safely change inside a hashed container
            public override int GetHashCode() {
                return Item.GetHashCode();
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

        public static IList<T> Solve<T>(T start, T goal, out float cost, NeighbourDelegate<T> neighboursFunction,
                                                                         CostDelegate<T> costFunction,
                                                                         HeuristicDelegate<T> heuristicFunction) {

            Dictionary<T, Node<T>> nodeMap = new Dictionary<T, Node<T>>();

            // We don't have SortedSet<T> in Unity's Mono so we use a custom PriorityQueue
            // A node is in the closed set if it is in the nodeMap but not in openQueue;
            MutablePriorityQueue<Node<T>> openQueue = new MutablePriorityQueue<Node<T>>();

            Node<T> nStart = new Node<T>(start, 0, heuristicFunction(start, goal), null, Flag.Open);
            nodeMap[start] = nStart;
            openQueue.Enqueue(nStart);
            cost = 0;
            while (openQueue.Count > 0) {
                Node<T> current = openQueue.Dequeue();
                System.Diagnostics.Debug.Write("current = " + current.Item.ToString());
                current.State = Flag.Closed;
                if (current.Item.Equals(goal)) {
                    // Return path and cost
                    List<T> reversePath = new List<T>();
                    for (Node<T> node = current; node != null; node = node.From) {
                        reversePath.Add(node.Item);
                        cost += node.Cost;
                    }
                    reversePath.Reverse();
                    return reversePath;
                }
                foreach (T item in neighboursFunction(current.Item)) {
                    float new_cost = current.Cost + costFunction(current.Item, item);
                    // If the item has a node, it will either be in the closedSet, or the openSet
                    if (nodeMap.ContainsKey(item)) {
                        Node<T> n = nodeMap[item];
                        if (new_cost < n.Cost) {
                            // Cost via current is better than the old one
                            n.From = current;
                            n.Cost = new_cost;
                            if(n.State == Flag.Open) {
                                // Update priority queue
                                openQueue.Increase(n);
                            } else if(n.State == Flag.Closed) {
                                // Re-open node
                                n.State = Flag.Open;
                                openQueue.Enqueue(n);
                            }
                        }
                    } else {
                        // It is not in the openSet, create a node and add it
                        Node<T> n = new Node<T>(item, new_cost, heuristicFunction(item, goal), current, Flag.Open);
                        openQueue.Enqueue(n);
                        nodeMap[item] = n;
                    }
                }
            }
            return new List<T>();
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

