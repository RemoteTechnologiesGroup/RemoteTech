using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace RemoteTech.Collections;

internal static class RTExtensions
{
    #region NativeArray<T>
    public static unsafe ref T GetElement<T>(this NativeArray<T> array, int index)
        where T : unmanaged
        => ref ((T*)NativeArrayUnsafeUtility.GetUnsafePtr(array))[index];

    public static void Fill<T>(this NativeArray<T> array, T value)
        where T : unmanaged
    {
        for (int i = 0; i < array.Length; ++i)
            array[i] = value;
    }

    public static unsafe void Clear<T>(this NativeArray<T> array)
        where T : unmanaged
    {
        UnsafeUtility.MemClear(
            NativeArrayUnsafeUtility.GetUnsafePtr(array),
            (long)array.Length * UnsafeUtility.SizeOf<T>());
    }
    #endregion

    #region NativeList<T>
    public static unsafe ref T GetElement<T>(this NativeList<T> list, int index)
        where T : unmanaged
    {
        return ref ((T*)NativeListUnsafeUtility.GetUnsafePtr(list))[index];
    }

    public static void Resize<T>(this NativeList<T> list, int length, T value)
        where T : unmanaged
    {
        var start = list.Length;
        list.ResizeUninitialized(length);

        for (int i = start; i < length; ++i)
            list[i] = value;
    }
    #endregion

    #region NativeSlice<T>
    public static unsafe NativeArray<T> AsNativeArray<T>(this NativeSlice<T> slice)
        where T : unmanaged
    {
        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
            NativeSliceUnsafeUtility.GetUnsafePtr(slice),
            slice.Length,
            Allocator.Invalid);
    }
    #endregion

    #region Vector3d
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double3 ToDouble3(this Vector3d v) => new(v.x, v.y, v.z);
    #endregion

    #region List<T>
    public static Span<T> AsSpan<T>(this List<T> list) =>
        ((Span<T>)list._items).Slice(0, list.Count);

    public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> span) => span;
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T> list) => list.AsSpan();
    #endregion

    #region Span<T>
    public static SpanEnumerator<T> GetEnumerator<T>(this Span<T> span) => new(span);

    public static ReadOnlySpanEnumerator<T> GetEnumerator<T>(this ReadOnlySpan<T> span) => new(span);

    public static T[] Concat<T>(this ReadOnlySpan<T> a, ReadOnlySpan<T> b)
    {
        T[] array = new T[a.Length + b.Length];
        a.CopyTo(array.AsSpan().Slice(0, a.Length));
        b.CopyTo(array.AsSpan().Slice(a.Length));
        return array;
    }
    #endregion

    #region NativeKeyValueArrays<K, V>
    public static NativeKeyValueArraysEnumerator<K, V> GetEnumerator<K, V>(this NativeKeyValueArrays<K, V> arrays)
        where K : unmanaged
        where V : unmanaged
    {
        return new(arrays);
    }
    #endregion
}

public ref struct SpanEnumerator<T>(Span<T> span) : IEnumerator<T>
{
    readonly Span<T> span = span;
    int index = -1;

    public readonly ref T Current => ref span[index];
    readonly T IEnumerator<T>.Current => Current;
    readonly object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        index += 1;
        return index < span.Length;
    }

    public void Dispose() { }

    public void Reset() => index = -1;
}

public ref struct ReadOnlySpanEnumerator<T>(ReadOnlySpan<T> span) : IEnumerator<T>
{
    readonly ReadOnlySpan<T> span = span;
    int index = -1;

    public readonly T Current => span[index];
    readonly object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        index += 1;
        return index < span.Length;
    }

    public void Dispose() { }

    public void Reset() => index = -1;
}

internal struct NativeKeyValueArraysEnumerator<K, V>(NativeKeyValueArrays<K, V> arrays) : IEnumerator<KeyValuePair<K, V>>
    where K : unmanaged
    where V : unmanaged
{
    int index = -1;
    readonly NativeKeyValueArrays<K, V> arrays = arrays;

    public KeyValuePair<K, V> Current => new (arrays.Keys[index], arrays.Values[index]);

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        index += 1;
        return index < arrays.Keys.Length;
    }

    public void Reset() => index = -1;

    public void Dispose() { }
}
