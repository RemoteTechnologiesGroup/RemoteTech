using System;
using System.Collections.Generic;

namespace RemoteTech {
    public class PriorityQueue<T> where T : class, IComparable {
        // TODO: Make Increase()/Decrease() O(logn) worst-case instead of O(n)

        private IList<T> mData;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class.
        /// Type T must be a reference-type and implement IComparable.
        /// It is not safe to modify priorities without notifying and updating the queue.
        /// </summary>
        public PriorityQueue() {
            this.mData = new List<T>();
        }

        // Public /////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of elements in this container.
        /// </summary>
        /// <value>
        /// Number of elements.
        /// </value>
        public int Count { get { return mData.Count; } }

        /// <summary>
        /// Enqueues the specified item. O(logn) worst-case
        /// </summary>
        /// <param name="item">The item.</param>
        public void Enqueue(T item) {
            mData.Add(item);
            _increase(mData.Count - 1);
        }

        /// <summary>
        /// Dequeues and returns the item in the container with the highest priority,
        /// the item preceding (lower than) every other item in the container. 
        /// As per IComparable implementation. O(logn) worst-case
        /// </summary>
        /// <returns>Highest priority item.</returns>
        public T Dequeue() {
            T front = mData[0];
            mData[0] = mData[mData.Count - 1];
            mData.RemoveAt(mData.Count - 1);
            _decrease(0);
            return front;
        }

        /// <summary>
        /// Updates the priority of the specified item and propagates it up the heap. The heap constraint can not be guaranteed if this is misused. Use Update() if you are unsure.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Increase(T item) {
            _increase(mData.IndexOf(item));
        }

        /// <summary>
        /// Updates the priority of the specified item and propagates it down the heap. The heap constraint can not be guaranteed if this is misused. Use Update() if you are unsure.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Decrease(T item) {
            _decrease(mData.IndexOf(item));
        }

        /// <summary>
        /// Updates the priority of the specified item and checks if it should propagate upwards or downwards.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Update(T item) {
            Increase(item);
            Decrease(item);
        }

        /// <summary>
        /// Returns the highest priority item without dequeueing.
        /// </summary>
        /// <returns>Highest priority item.</returns>
        public T Peek() {
            return mData[0];
        }

        // Private //////////////////////////////////////////////////////////

        private int _increase(int index) {
            // Move item upwards on tree
            int parent_id;
            int id = index;
            while(id > 0) {
                parent_id = (id - 1) / 2;
                if (mData[parent_id].CompareTo(mData[id]) < 0) {
                    break;
                }
                T tmp = mData[id];
                mData[id] = mData[parent_id];
                mData[parent_id] = tmp;
                id = parent_id;
            }
            return id;
        }

        private int _decrease(int index) {
            // Move item downwards on tree
            int lowest_id = mData.Count - 1;
            int child_id;
            int id = index;
            for(;;) {
                child_id = id * 2 + 1;
                if (child_id > lowest_id) {
                    break;
                }
                int right_id = child_id + 1;
                if (right_id <= lowest_id &&
                        mData[right_id].CompareTo(mData[child_id]) < 0) {
                    child_id = right_id;
                }
                if (mData[child_id].CompareTo(mData[id]) > 0) {
                    break;
                }
                T tmp = mData[id];
                mData[id] = mData[child_id];
                mData[child_id] = tmp;
                id = child_id;
            }
            return id;
        }
    }
}

