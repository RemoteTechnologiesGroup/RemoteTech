using System;
using System.Collections.Generic;

namespace RemoteTechExtended
{
    public class PriorityQueue<T> where T : class, IComparable {

        List<T> mData;

        public int Count { get { return mData.Count; } }

        public PriorityQueue() {
            this.mData = new List<T>();
        }

        public void Enqueue(T item) {
            // Insert item as leaf
            mData.Add(item);

            // Propagate item upwards on the tree
            int parent_id;
            for (int id = mData.Count - 1; id > 0; id = parent_id) {
                parent_id = (id - 1) / 2;
                if (mData[id].CompareTo(mData[parent_id]) >= 0) {
                    break;
                }
                T tmp = mData[id];
                mData[id] = mData[parent_id];
                mData[parent_id] = tmp;
            }
        }

        public T Peek() {
            return mData[0];
        }

        public T Dequeue() {
            // Remove top, move leaf to top
            int least_id = mData.Count - 1;
            T front = mData[0];
            mData[0] = mData[least_id];
            mData.RemoveAt(least_id--);

            // Propagate leaf downwards on the tree
            int id = 0;
            for (;;) {
                int child_id = id * 2 + 1;
                if (child_id > least_id) {
                    break;
                }
                int right_id = child_id + 1;
                if (right_id <= least_id && mData[right_id].CompareTo(mData[child_id]) < 0) {
                    child_id = right_id;
                }
                if (mData[id].CompareTo(mData[child_id]) <= 0) {
                    break;
                }
                T tmp = mData[id];
                mData[id] = mData[child_id];
                mData[child_id] = tmp;
                id = child_id;
            }
            return front;
        }
    }
}

