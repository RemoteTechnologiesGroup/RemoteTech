using System;
using KSP.Testing;
using RemoteTech.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace RemoteTech.InGameTests.Collections;

public unsafe class SpookyHashTests : RTTestBase
{
    [TestInfo("SpookyHashTests_MatchesUnity_ForInt")]
    public void MatchesUnity_ForInt()
    {
        foreach (int value in new[] { 0, 1, -1, 12345, int.MinValue, int.MaxValue })
        {
            int v = value;
            Hash128 expected = default;
            HashUtilities.ComputeHash128(ref v, ref expected);

            Hash128 actual = default;
            SpookyHash.Hash128(&v, sizeof(int), ref actual);

            Assert.AreEqual(expected, actual, $"int {value}");
        }
    }

    [TestInfo("SpookyHashTests_MatchesUnity_ForGuidArrays")]
    public void MatchesUnity_ForGuidArrays()
    {
        foreach (int count in new[] { 1, 2, 3, 4, 5, 8, 13 })
        {
            var arena = new NativeArray<Guid>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
                arena[i] = Guid.NewGuid();

            int size = count * UnsafeUtility.SizeOf<Guid>();

            Hash128 expected = default;
            HashUnsafeUtilities.ComputeHash128(NativeArrayUnsafeUtility.GetUnsafePtr(arena), (ulong)size, &expected);

            Hash128 actual = default;
            SpookyHash.Hash128(NativeArrayUnsafeUtility.GetUnsafePtr(arena), size, ref actual);

            Assert.AreEqual(expected, actual, $"{count} guids");
        }
    }

    [TestInfo("SpookyHashTests_MatchesUnity_ForLongBuffer")]
    public void MatchesUnity_ForLongBuffer()
    {
        // Exercises the >=192-byte block (Mix/End) path, not just Short.
        //
        // We'd normally use a length of 191, but unity's implementation has a bug there that
        // makes it incorrect so we use 188.
        foreach (int length in new[] { 188, 192, 193, 300, 500 })
        {
            var buf = new NativeArray<byte>(length, Allocator.Temp);
            var rng = new System.Random(length);
            for (int i = 0; i < length; i++)
                buf[i] = (byte)rng.Next(256);

            Hash128 expected = default;
            HashUnsafeUtilities.ComputeHash128(NativeArrayUnsafeUtility.GetUnsafePtr(buf), (ulong)length, &expected);

            Hash128 actual = default;
            SpookyHash.Hash128(NativeArrayUnsafeUtility.GetUnsafePtr(buf), length, ref actual);

            Assert.AreEqual(expected, actual, $"length {length}");
        }
    }

    [TestInfo("SpookyHashTests_MatchesUnity_ForChainedSeed")]
    public void MatchesUnity_ForChainedSeed()
    {
        // Mirrors NetworkUpdate.HashPath: hash a small int, then
        // feed a guid array using the running hash as the seed for the next call.
        int state = 2;
        var guids = new NativeArray<Guid>(3, Allocator.Temp);
        for (int i = 0; i < guids.Length; i++)
            guids[i] = Guid.NewGuid();
        int size = guids.Length * UnsafeUtility.SizeOf<Guid>();

        Hash128 expected = default;
        HashUtilities.ComputeHash128(ref state, ref expected);
        HashUnsafeUtilities.ComputeHash128(NativeArrayUnsafeUtility.GetUnsafePtr(guids), (ulong)size, &expected);

        Hash128 actual = default;
        SpookyHash.Hash128(&state, sizeof(int), ref actual);
        SpookyHash.Hash128(NativeArrayUnsafeUtility.GetUnsafePtr(guids), size, ref actual);

        Assert.AreEqual(expected, actual);
    }
}
