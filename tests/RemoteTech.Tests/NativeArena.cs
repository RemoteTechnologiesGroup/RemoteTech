using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace RemoteTech.Tests;

/// <summary>
/// Hands out <see cref="NativeArray{T}"/> instances backed by
/// <see cref="Marshal.AllocHGlobal"/> so the real Burst-path code can run in a
/// plain test process.
///
/// Outside the Unity player, <c>UnsafeUtility.Malloc</c> is a native ECall that
/// throws (<c>SecurityException: ECall methods must be packaged into a system
/// module</c>), so <c>new NativeArray&lt;T&gt;(len, Allocator.Temp/Persistent)</c>
/// and <c>NativeList&lt;T&gt;</c> cannot be constructed here. Wrapping our own
/// HGlobal block with <see cref="NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray"/>
/// (allocator <see cref="Allocator.None"/>) sidesteps that — element access and
/// <c>Length</c> work, and we own the lifetime.
///
/// Use one arena per test and dispose it (the helpers/fixtures below do) to free
/// every block at once.
/// </summary>
internal sealed class NativeArena : IDisposable
{
    private readonly List<IntPtr> _blocks = new List<IntPtr>();

    /// <summary>
    /// Zero-initialized native array of <paramref name="length"/> elements.
    /// </summary>
    public unsafe NativeArray<T> Alloc<T>(int length) where T : unmanaged
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        // Always allocate at least one byte so the pointer is valid; a zero-length
        // NativeArray over that pointer simply never dereferences it.
        int bytes = Math.Max(1, length * sizeof(T));
        IntPtr ptr = Marshal.AllocHGlobal(bytes);
        Unsafe_ZeroMemory((byte*)ptr, bytes);
        _blocks.Add(ptr);
        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)ptr, length, Allocator.None);
    }

    /// <summary>
    /// Native array initialized from <paramref name="values"/>.
    /// </summary>
    public NativeArray<T> Of<T>(params T[] values) where T : unmanaged
    {
        var array = Alloc<T>(values.Length);
        for (int i = 0; i < values.Length; i++) array[i] = values[i];
        return array;
    }

    private static unsafe void Unsafe_ZeroMemory(byte* p, int bytes)
    {
        for (int i = 0; i < bytes; i++) p[i] = 0;
    }

    public void Dispose()
    {
        foreach (var ptr in _blocks) Marshal.FreeHGlobal(ptr);
        _blocks.Clear();
    }
}
