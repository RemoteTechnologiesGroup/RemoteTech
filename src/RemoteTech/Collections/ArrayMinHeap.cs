using System;
using Unity.Collections;

namespace RemoteTech.Collections;

/// <summary>
/// Binary min-heap over an internally-allocated <see cref="NativeList{T}"/>.
/// Grows on demand, so callers don't need to size it to a total-pushes upper
/// bound themselves.
/// </summary>
internal struct ArrayMinHeap<T>(int capacity, Allocator allocator)
    where T : unmanaged, IComparable<T>
{
    private NativeList<T> _items = new(capacity, allocator);

    public int Count => _items.Length;

    public void Push(T item)
    {
        int index = Count;
        _items.Add(item);
        MoveUp(index);
    }

    /// <summary>
    /// Adds a batch of items in O(n) via bottom-up heapify, rather than O(n log n)
    /// from pushing them one at a time.
    /// </summary>
    public void AddRange(NativeArray<T> items)
    {
        _items.AddRange(items);

        for (int i = (Count >> 1) - 1; i >= 0; i--)
            MoveDown(i);
    }

    public bool TryPop(out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = _items[0];
        _items.RemoveAtSwapBack(0);

        if (Count > 0)
            MoveDown(0);

        return true;
    }

    /// <summary>
    /// Sifts the item at <paramref name="index"/> up toward the root until its
    /// parent is no greater.
    /// </summary>
    private void MoveUp(int index)
    {
        T item = _items[index];
        while (index > 0)
        {
            int parent = (index - 1) >> 1;
            T parentItem = _items[parent];
            if (item.CompareTo(parentItem) >= 0)
                break;

            _items[index] = parentItem;
            index = parent;
        }
        _items[index] = item;
    }

    /// <summary>
    /// Sifts the item at <paramref name="index"/> down toward the leaves until
    /// both children are no smaller.
    /// </summary>
    private void MoveDown(int index)
    {
        T item = _items[index];
        int count = Count;
        while (true)
        {
            int left = (index << 1) + 1;
            int right = left + 1;
            if (left >= count) break;

            int min = left;
            T minItem = _items[left];
            if (right < count)
            {
                T r = _items[right];
                if (r.CompareTo(minItem) < 0)
                {
                    min = right;
                    minItem = r;
                }
            }

            if (item.CompareTo(minItem) <= 0) break;
            _items[index] = minItem;
            index = min;
        }
        _items[index] = item;
    }
}
