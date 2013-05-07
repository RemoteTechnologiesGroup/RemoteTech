using System;
using System.Collections.Generic;


namespace RemoteTech
{
    public static class Pathfinder {
        public delegate IList<T> NeighbourDelegate<T>(T a);
        public delegate float CostDelegate<T>(T a,T b);
        public delegate float HeuristicDelegate<T>(T a,T b);

        public class Node<T> where T : Object, IComparable {

            public T Item { get; }
            public float Cost { get; set; }
            public float Heuristic { get; set; }
            public Node<T> From { get; set; }

            public Node(T item, float cost, float heuristic, Node<T> from) {
                this.Item = item;
                this.Cost = cost;
                this.Heuristic = heuristic;
                this.From = from;
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
                return T.GetHashCode();
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

        public static IList<T> Solve<T>(T start, T goal, NeighbourDelegate<T> neighboursFunction,
                                                         CostDelegate<T> costFunction,
                                                         HeuristicDelegate<T> heuristicFunction) {

            Dictionary<T, Node<T>> nodeMap = new Dictionary<T, Node<T>>();
            HashSet<Node<T>> closedSet = new HashSet<Node<T>>();

            // We don't have SortedSet<T> in Unity's Mono so we use a custom PriorityQueue and HashSet.
            PriorityQueue<Node<T>> openQueue = new PriorityQueue<Node<T>>();
            HashSet<Node<T>> openSet = new HashSet<Node<T>>();

            Node<T> nStart = new Node<T>(start, 0, heuristicFunction(start, goal), null);
            Node<T> nGoal = new Node<T>(goal, 0, 0);
            openSet.Add(nStart);
            openQueue.Enqueue(nStart);
            while (openSet.Count > 0) {
                Node<T> current = openQueue.Dequeue();
                openSet.Remove(current);
                if (current.Equals(nGoal)) {
                    nGoal.From = current;
                    return Reconstruct(nGoal);
                }
                closedSet.Add(current);
                foreach (T item in neighboursFunction(current.Item)) {
                    float cost = current.Cost + costFunction(current.Item, item);
                    // If the item has a node, it will either be in the closedSet, or the openSet
                    if (nodeMap.ContainsKey(item)) {
                        Node<T> n = nodeMap[item];
                        if (cost < n.Cost) {
                            // Cost via current is better than the old one
                            n.From = current;
                            n.Cost = cost;
                        }
                    } else {
                        // It is not in the openSet, create a node and add it
                        Node<T> n = new Node<T>(item, cost, heuristicFunction(item, goal), current);
                        openSet.Add(n);
                        openQueue.Enqueue(n);
                        nodeMap[item] = n;
                    }
                }
            }
        }

        static IList<T> Reconstruct<T>(Node<T> goal) {
            List<T> reversePath = new List<T>();
            for (Node<T> node = goal; node.Item != null; node = node.From) {
                reversePath.Add(node.Item);
            }
            return reversePath.Reverse();
        }
    }
}

