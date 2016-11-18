using System.Collections;
using System.Collections.Generic;

namespace RemoteTech.Common.Collections
{
    // PriorityQueue based on a minimum-BinaryHeap.
    public class PriorityQueue<T> : IEnumerable<T>
    {
        public int Count { get { return mHeap.Count; } }

        private readonly BinaryHeap<T> mHeap;

        public PriorityQueue()
        {
            mHeap = new BinaryHeap<T>();
        }

        public void Clear()
        {
            mHeap.Clear();
        }

        public void Enqueue(T item)
        {
            mHeap.Add(item);
        }

        public T Peek()
        {
            return mHeap.Peek();
        }

        public T Dequeue()
        {
            return mHeap.Remove();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return mHeap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}