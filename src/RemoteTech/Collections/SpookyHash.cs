using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace RemoteTech.Collections;

/// <summary>
/// Burst-compatible port of SpookyHash V2.
/// </summary>
/// 
/// <remarks>
/// Unity actually has this available internally, but it is not burst-compatible
/// due to a call made in its static constructor.
/// </remarks>
internal static unsafe class SpookyHash
{
    const ulong SeedConst = 0xdeadbeefdeadbeefUL;

    static readonly byte[] MixRot = [11, 32, 43, 31, 17, 28, 39, 57, 55, 54, 22, 46];
    static readonly byte[] EndRot = [44, 15, 34, 21, 38, 33, 10, 13, 38, 53, 42, 54];
    static readonly byte[] ShortMixRot = [50, 52, 30, 41, 54, 48, 38, 37, 62, 34, 5, 36];
    static readonly byte[] ShortEndRot = [15, 52, 26, 51, 28, 9, 47, 54, 32, 25, 63];

    static ulong Rot64(ulong x, int k) => (x << k) | (x >> (64 - k));

    /// <summary>
    /// Updates the 128-bit state in place, seeded from <paramref name="hash"/>'s current value.
    /// </summary>
    public static void Hash128(void* data, int length, ref Hash128 hash)
    {
        fixed (Hash128* hp = &hash)
        {
            ulong* seed = (ulong*)hp;
            ulong hash1 = seed[0];
            ulong hash2 = seed[1];

            if (length < 192)
            {
                Short(data, length, ref hash1, ref hash2);
            }
            else
            {
                var s = stackalloc ulong[12];
                for (int i = 0; i < 12; i++)
                    s[i] = (i % 3) switch { 0 => hash1, 1 => hash2, _ => SeedConst };

                byte* p = (byte*)data;
                byte* end = p + length / 96 * 96;
                while (p < end)
                {
                    Mix((ulong*)p, s);
                    p += 96;
                }

                var buf = stackalloc ulong[12];
                int remainder = length - (int)(end - (byte*)data);
                UnsafeUtility.MemCpy(buf, p, remainder);
                UnsafeUtility.MemClear((byte*)buf + remainder, 96 - remainder);
                ((byte*)buf)[95] = (byte)remainder;

                End(buf, s);

                hash1 = s[0];
                hash2 = s[1];
            }

            seed[0] = hash1;
            seed[1] = hash2;
        }
    }

    static void Mix(ulong* data, ulong* s)
    {
        for (int i = 0; i < 12; i++)
        {
            int a = i;
            int b = (i + 2) % 12;
            int c = (i + 10) % 12;
            int d = (i + 11) % 12;
            int e = (i + 1) % 12;
            s[a] += data[a];
            s[b] ^= s[c];
            s[d] ^= s[a];
            s[a] = Rot64(s[a], MixRot[a]);
            s[d] += s[e];
        }
    }

    static void End(ulong* data, ulong* s)
    {
        for (int i = 0; i < 12; i++) s[i] += data[i];

        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < 12; i++)
            {
                int a = (i + 11) % 12;
                int b = (i + 1) % 12;
                int c = (i + 2) % 12;
                s[a] += s[b];
                s[c] ^= s[a];
                s[b] = Rot64(s[b], EndRot[i]);
            }
        }
    }

    static void ShortMix(ulong* h)
    {
        for (int k = 0; k < 12; k++)
        {
            int cur = (2 + k) % 4;
            int nxt = (cur + 1) % 4;
            int prev2 = (cur + 2) % 4;
            h[cur] = Rot64(h[cur], ShortMixRot[k]);
            h[cur] += h[nxt];
            h[prev2] ^= h[cur];
        }
    }

    static readonly int[] ShortEndX = { 3, 0, 1, 2 };
    static readonly int[] ShortEndY = { 2, 3, 0, 1 };

    static void ShortEnd(ulong* h)
    {
        for (int k = 0; k < 11; k++)
        {
            int x = ShortEndX[k % 4];
            int y = ShortEndY[k % 4];
            h[x] ^= h[y];
            h[y] = Rot64(h[y], ShortEndRot[k]);
            h[x] += h[y];
        }
    }

    static void Short(void* message, int length, ref ulong hash1, ref ulong hash2)
    {
        var h = stackalloc ulong[4];
        h[0] = hash1;
        h[1] = hash2;
        h[2] = SeedConst;
        h[3] = SeedConst;

        byte* p = (byte*)message;
        int remaining = length;

        while (remaining >= 32)
        {
            h[2] += *(ulong*)p;
            h[3] += *(ulong*)(p + 8);
            ShortMix(h);
            h[0] += *(ulong*)(p + 16);
            h[1] += *(ulong*)(p + 24);
            p += 32;
            remaining -= 32;
        }

        if (remaining >= 16)
        {
            h[2] += *(ulong*)p;
            h[3] += *(ulong*)(p + 8);
            ShortMix(h);
            p += 16;
            remaining -= 16;
        }

        h[3] += (ulong)(uint)length << 56;

        if (remaining > 0)
        {
            // Zero-padded scratch instead of Unity's in-place tail reads, which overrun the buffer for a 7-byte remainder.
            var tail = stackalloc byte[16];
            UnsafeUtility.MemClear(tail, 16);
            UnsafeUtility.MemCpy(tail, p, remaining);
            h[2] += *(ulong*)tail;
            h[3] += *(ulong*)(tail + 8);
        }
        else
        {
            h[2] += SeedConst;
            h[3] += SeedConst;
        }

        ShortEnd(h);

        hash1 = h[0];
        hash2 = h[1];
    }
}
