using System.Collections;
using System.Collections.Generic;

namespace RemoteTech.Common.Collections
{
    // Sadly there is no way to enforce immutability C#.
    // Do not modify sorting order externally! Increase/Decrease().
    public class BinaryHeap<T> : IEnumerable<T>
    {
        public int Count => _data.Count;

        public int Capacity => _data.Capacity;

        public bool IsReadOnly => false;

        private readonly List<T> _data;
        private readonly Comparer<T> _comparer;

        public BinaryHeap() : this(0) { }

        public BinaryHeap(int size)
        {
            _data = new List<T>(size);
            _comparer = Comparer<T>.Default;
        }

        public void Clear()
        {
            _data.Clear();
        }

        public T Peek()
        {
            return _data[0];
        }

        public T Remove()
        {
            var top = _data[0];
            _data[0] = _data[_data.Count - 1];
            _data.RemoveAt(_data.Count - 1);
            if (_data.Count > 0)
            {
                Decrease(0);
            }
            return top;
        }

        public void Add(T item)
        {
            _data.Add(item);
            Increase(_data.Count - 1);
        }

        public void Increase(int id)
        {
            var item = _data[id];
            var parentId = (id - 1) >> 1;
            // While the item precedes its parent and hasn't reached root.
            while ((parentId >= 0) && (_comparer.Compare(item, _data[parentId]) < 0))
            {
                // Propagate the parent downwards.
                _data[id] = _data[parentId];
                id = parentId;
                parentId = (id - 1) >> 1;
            }
            // Place the item where it belongs.
            _data[id] = item;
        }


        public void Decrease(int id)
        {
            var item = _data[id];
            while (true)
            {
                int newId;
                var child1 = (id << 1) + 1;
                if (child1 > _data.Count - 1)
                {
                    break;
                }
                var child2 = (id << 1) + 2;
                // Check whether to use the left or right node.
                if (child2 > _data.Count - 1)
                {
                    newId = child1;
                }
                else
                {
                    newId = _comparer.Compare(_data[child1], _data[child2]) < 0 ? child1 : child2;
                }
                // Propagate the child upwards if needed
                if (_comparer.Compare(item, _data[newId]) > 0)
                {
                    _data[id] = _data[newId];
                    id = newId;
                }
                else
                {
                    break;
                }
            }
            // Place the item where it belongs.
            _data[id] = item;
        }

        public int IndexOf(T item)
        {
            return _data.IndexOf(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}