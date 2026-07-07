using System;
using System.Collections;
using System.Collections.Generic;

namespace RemoteTech.InGameTests;

/// <summary>
/// MSTest-compatible assertion shim that throws on failure. KSP's test
/// framework (<see cref="KSP.Testing.TestManager"/>) treats a thrown exception
/// as a failed test. Adapted from the sibling BackgroundResourceProcessing
/// test harness.
/// </summary>
public static class Assert
{
    public static void AreEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Assert.AreEqual failed. Expected:<{expected}>. Actual:<{actual}>.");
    }

    public static void AreEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Assert.AreEqual failed. Expected:<{expected}>. Actual:<{actual}>. {message}");
    }

    public static void AreEqual(double expected, double actual, double delta)
    {
        if (double.IsNaN(expected) || double.IsNaN(actual) || Math.Abs(expected - actual) > delta)
            throw new Exception($"Assert.AreEqual failed. Expected:<{expected}>. Actual:<{actual}>. Delta:<{delta}>.");
    }

    public static void AreEqual(double expected, double actual, double delta, string message)
    {
        if (double.IsNaN(expected) || double.IsNaN(actual) || Math.Abs(expected - actual) > delta)
            throw new Exception($"Assert.AreEqual failed. Expected:<{expected}>. Actual:<{actual}>. Delta:<{delta}>. {message}");
    }

    public static void AreNotEqual<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Assert.AreNotEqual failed. Expected any value except:<{expected}>.");
    }

    public static void IsTrue(bool condition)
    {
        if (!condition) throw new Exception("Assert.IsTrue failed.");
    }

    public static void IsTrue(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assert.IsTrue failed. {message}");
    }

    public static void IsFalse(bool condition)
    {
        if (condition) throw new Exception("Assert.IsFalse failed.");
    }

    public static void IsFalse(bool condition, string message)
    {
        if (condition) throw new Exception($"Assert.IsFalse failed. {message}");
    }

    public static T ThrowsException<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new Exception($"Assert.ThrowsException failed. Expected:<{typeof(T).Name}>. Actual:<{ex.GetType().Name}>.");
        }
        throw new Exception($"Assert.ThrowsException failed. Expected:<{typeof(T).Name}>. No exception was thrown.");
    }

    public static void Fail(string message)
    {
        throw new Exception($"Assert.Fail. {message}");
    }

    public static void IsNull(object value)
    {
        if (value != null) throw new Exception($"Assert.IsNull failed. Actual:<{value}>.");
    }

    public static void IsNotNull(object value)
    {
        if (value == null) throw new Exception("Assert.IsNotNull failed.");
    }

    public static void IsNotNull(object value, string message)
    {
        if (value == null) throw new Exception($"Assert.IsNotNull failed. {message}");
    }

    public static void AreSame(object expected, object actual)
    {
        if (!ReferenceEquals(expected, actual))
            throw new Exception($"Assert.AreSame failed. Expected:<{expected}>. Actual:<{actual}>.");
    }
}

public static class CollectionAssert
{
    public static void AreEqual(ICollection expected, ICollection actual)
    {
        if (expected == null && actual == null) return;
        if (expected == null || actual == null)
            throw new Exception("CollectionAssert.AreEqual failed. One collection is null.");
        if (expected.Count != actual.Count)
            throw new Exception($"CollectionAssert.AreEqual failed. Expected count:<{expected.Count}>. Actual count:<{actual.Count}>.");

        var e = expected.GetEnumerator();
        var a = actual.GetEnumerator();
        int index = 0;
        while (e.MoveNext() && a.MoveNext())
        {
            if (!Equals(e.Current, a.Current))
                throw new Exception($"CollectionAssert.AreEqual failed at index {index}. Expected:<{e.Current}>. Actual:<{a.Current}>.");
            index++;
        }
    }
}
