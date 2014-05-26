using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public class EventListWrapper<T> : IList<T>, IConfigNode
    {
        public event Action ListChanged = delegate { };
        public event Action AddingNew = delegate { };

        private List<T> source;
        protected EventListWrapper() { }
        public EventListWrapper(IList<T> list)
        {
            this.source = new List<T>(list);
        }
        public int IndexOf(T item)
        {
             return source.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            source.Insert(index, item);
            ListChanged.Invoke();
        }

        public void RemoveAt(int index)
        {
            source.RemoveAt(index);
            ListChanged.Invoke();
        }

        public T this[int index]
        {
            get
            {
                return source[index];
            }
            set
            {
                source[index] = value;
                ListChanged.Invoke();
            }
        }

        public void Add(T item)
        {
            source.Add(item);
            AddingNew.Invoke();
        }

        public void Clear()
        {
            source.Clear();
            ListChanged.Invoke();
        }

        public bool Contains(T item)
        {
            return source.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            source.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return source.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IList<T>)source).IsReadOnly; }
        }

        public bool Remove(T item)
        {
            bool ret = source.Remove(item);
            ListChanged.Invoke();
            return ret;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return source.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Load(ConfigNode node)
        {
            source = new List<T>();
            if (node.HasNode("Items")) {
                try {
                    var array = ConfigNode.CreateObjectFromConfig<T[]>(node.GetNode("Items"));
                    source.AddRange(array);
                } catch (Exception) { ; }
            }
        }

        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(new { Items = source.ToArray() }, 0, node);
        }
    }
}