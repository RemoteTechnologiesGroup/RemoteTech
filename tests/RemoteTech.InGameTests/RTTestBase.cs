using KSP.Testing;

namespace RemoteTech.InGameTests;

/// <summary>
/// Base class for RemoteTech's in-game unit tests. Subclasses live in this
/// assembly and tag test methods with <c>[TestInfo("...")]</c>; KSP's stock
/// <see cref="TestManager"/> discovers every <see cref="UnitTest"/>, and
/// <see cref="TestRunnerUI"/> runs just the RemoteTech ones from a main-menu
/// button. Unlike the headless RemoteTech.Tests project, these run inside the
/// KSP process, so the real native allocators (Temp/TempJob/Persistent),
/// NativeList, and scheduled Burst jobs are all available.
/// </summary>
public abstract class RTTestBase : UnitTest { }
