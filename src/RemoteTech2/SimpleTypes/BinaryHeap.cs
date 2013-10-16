using System;
using System.Collections.Generic;

namespace RemoteTech
{
    // Sadly there is no way to enforce immutability C#.
    // Do not modify sorting order externally! Increase/Decrease().
    public class BinaryHeap<T> where T : class
    {
        public int Count { get { return mData.Count; } }

        public int Capacity { get { return mData.Capacity; } }

        public bool IsReadOnly { get { return false; } }

        private readonly List<T> mData;
        private readonly Comparer<T> mComparer;

        public BinaryHeap() : this(0) { }

        public BinaryHeap(int size)
        {
            mData = new List<T>(size);
            mComparer = Comparer<T>.Default;
        }

        public void Clear()
        {
            mData.Clear();
        }

        public T Peek()
        {
            return mData[0];
        }

        public T Remove()
        {
            T top = mData[0];
            mData[0] = mData[mData.Count - 1];
            mData.RemoveAt(mData.Count - 1);
            if (mData.Count > 0)
            {
                Decrease(0);
            }
            return top;
        }

        public void Add(T item)
        {
            mData.Add(item);
            Increase(mData.Count - 1);
        }

        public void Increase(int id)
        {
            T item = mData[id];
            int parent_id = (id - 1) >> 1;
            // While the item precedes its parent and hasn't reached root.
            while ((parent_id >= 0) && (mComparer.Compare(item, mData[parent_id]) < 0))
            {
                // Propagate the parent downwards.
                mData[id] = mData[parent_id];
                id = parent_id;
                parent_id = (id - 1) >> 1;
            }
            // Place the item where it belongs.
            mData[id] = item;
        }


        public void Decrease(int id)
        {
            T item = mData[id];
            while (true)
            {
                int new_id;
                int child1 = (id << 1) + 1;
                if (child1 > mData.Count - 1)
                {
                    break;
                }
                int child2 = (id << 1) + 2;
                // Check whether to use the left or right node.
                if (child2 > mData.Count - 1)
                {
                    new_id = child1;
                }
                else
                {
                    new_id = mComparer.Compare(mData[child1], mData[child2]) < 0 ? child1 : child2;
                }
                // Propagate the child upwards if needed
                if (mComparer.Compare(item, mData[new_id]) > 0)
                {
                    mData[id] = mData[new_id];
                    id = new_id;
                }
                else
                {
                    break;
                }
            }
            // Place the item where it belongs.
            mData[id] = item;
        }

        public int IndexOf(T item)
        {
            return mData.IndexOf(item);
        }
    }
}