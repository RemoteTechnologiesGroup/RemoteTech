using System;
using System.Collections;
using System.Collections.Generic;

namespace RemoteTech.Collections;

internal readonly struct IntRange(int start, int length)
{
    public int Start { get; }= start;
    public int Length { get; } = length;
    public int End => Start + Length;

    public bool IsEmpty => Length == 0;

    public RangeEnumerator GetEnumerator() => new(this);
}

internal struct RangeEnumerator(IntRange range) : IEnumerator<int>
{
    int index = range.Start - 1;
    readonly int end = range.End;

    public readonly int Current => index;
    readonly object IEnumerator.Current => Current;

    public readonly void Dispose() { }

    public bool MoveNext()
    {
        index += 1;
        return index < end;
    }

    public void Reset() => throw new NotImplementedException();

    public readonly RangeEnumerator GetEnumerator() => this;
}