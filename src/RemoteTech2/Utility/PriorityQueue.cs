using System;
using System.Collections;
using System.Collections.Generic;

namespace RemoteTech
{
    // PriorityQueue based on a minimum-BinaryHeap.
    public class PriorityQueue<T> : IEnumerable<T>
    {
        public int Count { get { return heap.Count; } }

        private readonly BinaryHeap heap = new BinaryHeap();

        public void Clear()
        {
            heap.Clear();
        }

        public void Enqueue(T item)
        {
            heap.Add(item);
        }

        public T Peek()
        {
            return heap.Peek();
        }

        public T Dequeue()
        {
            return heap.Remove();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            heap.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return heap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class BinaryHeap : IEnumerable<T>
        {
            public int Count { get { return data.Count; } }

            public int Capacity { get { return data.Capacity; } }

            private readonly List<T> data;
            private readonly Comparer<T> comparer;

            public BinaryHeap() : this(0) { }

            public BinaryHeap(int size)
            {
                data = new List<T>(size);
                comparer = Comparer<T>.Default;
            }

            public void Clear()
            {
                data.Clear();
            }

            public T Peek()
            {
                return data[0];
            }

            public T Remove()
            {
                T top = data[0];
                data[0] = data[data.Count - 1];
                data.RemoveAt(data.Count - 1);
                if (data.Count > 0)
                {
                    Decrease(0);
                }
                return top;
            }

            public void Add(T item)
            {
                data.Add(item);
                Increase(data.Count - 1);
            }

            public void Increase(int id)
            {
                T item = data[id];
                int parent_id = (id - 1) >> 1;
                // While the item precedes its parent and hasn't reached root.
                while ((parent_id >= 0) && (comparer.Compare(item, data[parent_id]) < 0))
                {
                    // Propagate the parent downwards.
                    data[id] = data[parent_id];
                    id = parent_id;
                    parent_id = (id - 1) >> 1;
                }
                // Place the item where it belongs.
                data[id] = item;
            }

            public void Decrease(int id)
            {
                T item = data[id];
                while (true)
                {
                    int new_id;
                    int child1 = (id << 1) + 1;
                    if (child1 > data.Count - 1)
                    {
                        break;
                    }
                    int child2 = (id << 1) + 2;
                    // Check whether to use the left or right node.
                    if (child2 > data.Count - 1)
                    {
                        new_id = child1;
                    }
                    else
                    {
                        new_id = comparer.Compare(data[child1], data[child2]) < 0 ? child1 : child2;
                    }
                    // Propagate the child upwards if needed
                    if (comparer.Compare(item, data[new_id]) > 0)
                    {
                        data[id] = data[new_id];
                        id = new_id;
                    }
                    else
                    {
                        break;
                    }
                }
                // Place the item where it belongs.
                data[id] = item;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                data.CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return data.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}