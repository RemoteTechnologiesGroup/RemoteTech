using System;
using System.Collections.Generic;

namespace RemoteTech {
    // PriorityQueue based on a minimum-BinaryHeap.
    public class PriorityQueue<T> where T : class, IComparable<T> {

        BinaryHeap<T> mHeap;

        public PriorityQueue() {
            mHeap = new BinaryHeap<T>();
        }

        public int Count { get { return mHeap.Count; } }

        public void Enqueue(T item) {
            mHeap.Add(item);
        }

        public T Peek() {
            return mHeap.Peek();
        }

        public T Dequeue() {
            return mHeap.Remove();
        }

        public void Increase(T item) {
            mHeap.Increase(mHeap.IndexOf(item));
        }

        public void Decrease(T item) {
            mHeap.Decrease(mHeap.IndexOf(item));
        }
    }
}

