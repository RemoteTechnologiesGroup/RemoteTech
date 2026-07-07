using System;
using System.Collections;
using System.Collections.Generic;

namespace RemoteTech.Collections;

public class ArrayMap<K, V> : IEnumerable<KeyValuePair<K, V>>
{
    private readonly Dictionary<K, int> indices;
    private readonly List<K> keys;
    private readonly List<V> values;

    public int Count => values.Count;
    public V this[K key]
    {
        get => values[indices[key]];
        set
        {
            if (indices.TryGetValue(key, out var index))
            {
                keys[index] = key;
                values[index] = value;
            }
            else
            {
                index = values.Count;
                indices.Add(key, index);
                keys.Add(key);
                values.Add(value);
            }
        }
    }

    public ReadOnlySpan<K> Keys => keys.AsReadOnlySpan();
    public Span<V> Values => values.AsSpan();

    public ArrayMap()
    {
        indices = [];
        keys = [];
        values = [];
    }

    public ArrayMap(int capacity)
    {
        indices = new(capacity);
        keys = new(capacity);
        values = new(capacity);
    }

    public bool TryGetValue(K key, out V value)
    {
        value = default;
        if (!indices.TryGetValue(key, out var index))
            return false;

        value = values[index];
        return true;
    }

    public bool ContainsKey(K key) => indices.ContainsKey(key);

    public int IndexOf(K key) => indices.TryGetValue(key, out var index) ? index : -1;

    public void Add(K key, V value)
    {
        var count = Count;
        indices.Add(key, count);
        keys.Add(key);
        values.Add(value);
    }

    public bool TryAdd(K key, V value)
    {
        if (indices.ContainsKey(key))
            return false;

        var index = Count;
        indices.Add(key, index);
        keys.Add(key);
        values.Add(value);
        return true;
    }

    public bool Remove(K key)
    {
        if (!indices.TryGetValue(key, out var index))
            return false;

        if (index == Count - 1)
        {
            indices.Remove(key);
            keys.RemoveAt(index);
            values.RemoveAt(index);
        }
        else
        {
            var count = Count;
            var other = keys[index] = keys[count - 1];
            values[index] = values[count - 1];

            indices[other] = index;
            indices.Remove(key);
            keys.RemoveAt(count - 1);
            values.RemoveAt(count - 1);
        }

        return true;
    }

    public void Clear()
    {
        indices.Clear();
        keys.Clear();
        values.Clear();
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(ArrayMap<K, V> map) : IEnumerator<KeyValuePair<K, V>>
    {
        List<K>.Enumerator keys = map.keys.GetEnumerator();
        List<V>.Enumerator vals = map.values.GetEnumerator();

        public KeyValuePair<K, V> Current => new(keys.Current, vals.Current);
        object IEnumerator.Current => Current;

        public bool MoveNext() => keys.MoveNext() & vals.MoveNext();

        void IEnumerator.Reset()
        {
            DoReset(ref keys);
            DoReset(ref vals);
        }

        public void Dispose() { }

        private static void DoReset<T>(ref T enumerator)
            where T : IEnumerator
        {
            enumerator.Reset();
        }
    }
}