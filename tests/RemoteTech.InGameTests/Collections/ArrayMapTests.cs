using System;
using System.Collections.Generic;
using KSP.Testing;
using RemoteTech.Collections;

namespace RemoteTech.InGameTests.Collections;

public class ArrayMapTests : RTTestBase
{
    [TestInfo("ArrayMapTests_ListAsSpan_ReturnsOccupiedElements_NotUnusedCapacity")]
    public void ListAsSpan_ReturnsOccupiedElements_NotUnusedCapacity()
    {
        var list = new List<int>(16) { 1, 2, 3 };
        var span = list.AsSpan();

        Assert.AreEqual(3, span.Length);
        Assert.AreEqual(1, span[0]);
        Assert.AreEqual(2, span[1]);
        Assert.AreEqual(3, span[2]);
    }

    [TestInfo("ArrayMapTests_Values_AfterRemove_ContainsOnlyLiveEntries")]
    public void Values_AfterRemove_ContainsOnlyLiveEntries()
    {
        var map = new ArrayMap<Guid, string>();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        map[a] = "a";
        map[b] = "b";
        map[c] = "c";
        map.Remove(b);

        var values = map.Values;
        Assert.AreEqual(2, values.Length);
        foreach (var value in values)
            Assert.IsNotNull(value);
    }
}
