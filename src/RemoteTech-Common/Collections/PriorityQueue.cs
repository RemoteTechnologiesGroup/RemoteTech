using System.Collections;
using System.Collections.Generic;

namespace RemoteTech.Common.Collections
{
    // PriorityQueue based on a minimum-BinaryHeap.
    public class PriorityQueue<T> : IEnumerable<T>
    {
        public int Count => _binaryHeap.Count;

        private readonly BinaryHeap<T> _binaryHeap;

        public PriorityQueue()
        {
            _binaryHeap = new BinaryHeap<T>();
        }

        public void Clear()
        {
            _binaryHeap.Clear();
        }

        public void Enqueue(T item)
        {
            _binaryHeap.Add(item);
        }

        public T Peek()
        {
            return _binaryHeap.Peek();
        }

        public T Dequeue()
        {
            return _binaryHeap.Remove();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _binaryHeap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}