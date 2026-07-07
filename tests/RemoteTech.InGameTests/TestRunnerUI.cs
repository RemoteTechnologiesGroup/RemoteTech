using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using KSP.Testing;
using KSP.UI.Screens;
using UnityEngine;

namespace RemoteTech.InGameTests;

/// <summary>
/// Main-menu app-launcher button that runs RemoteTech's in-game unit tests and
/// shows a pass/fail window. Adapted from the sibling BackgroundResourceProcessing
/// harness.
/// </summary>
[KSPAddon(KSPAddon.Startup.MainMenu, false)]
public class TestRunnerUI : MonoBehaviour
{
    ApplicationLauncherButton button;
    TestResults results;
    bool showWindow;
    Vector2 scroll;
    Rect windowRect = new Rect(100, 100, 600, 700);
    int passCount;
    int failCount;

    static Texture2D buttonTexture;

    void Start()
    {
        if (buttonTexture == null)
            buttonTexture = RTUtil.LoadImage("gitpagessat");

        GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
        if (ApplicationLauncher.Ready)
            OnAppLauncherReady();
    }

    void OnDestroy()
    {
        GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
        if (button != null)
            ApplicationLauncher.Instance.RemoveModApplication(button);
    }

    void OnAppLauncherReady()
    {
        if (button != null) return;
        button = ApplicationLauncher.Instance.AddModApplication(
            OnButtonTrue, OnButtonFalse, null, null, null, null,
            ApplicationLauncher.AppScenes.MAINMENU, buttonTexture);
    }

    void OnButtonTrue()
    {
        RunTests();
        showWindow = true;
    }

    void OnButtonFalse() => showWindow = false;

    // KSP's TestManager registers an instance of every UnitTest subclass across
    // all loaded mods. Reach into that list and run only the RemoteTech ones
    // (deriving from RTTestBase) rather than every mod's tests.
    static readonly FieldInfo TestsField = typeof(TestManager).GetField(
        "tests", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
    static readonly MethodInfo PerformTestMethod = typeof(UnitTest).GetMethod(
        "PerformTest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    static TestResults RunRTTests()
    {
        var results = new TestResults();
        var tests = (IEnumerable)TestsField.GetValue(null);
        foreach (UnitTest test in tests)
        {
            if (test == null || test is not RTTestBase)
                continue;
            var states = (IEnumerable)PerformTestMethod.Invoke(test, null);
            foreach (TestState state in states)
            {
                if (state.Succeeded) results.success++;
                else results.failed++;
                results.states.Add(state);
            }
        }
        return results;
    }

    void RunTests()
    {
        results = RunRTTests();
        passCount = 0;
        failCount = 0;
        foreach (var state in results.states)
        {
            if (state.Succeeded) passCount++;
            else failCount++;
        }

        Debug.Log($"[RT.Test] {passCount} passed, {failCount} failed ({results.states.Count} total)");
        foreach (var state in results.states)
        {
            if (state.Succeeded) continue;
            var name = state.Info?.Name ?? "(unnamed)";
            Debug.LogError($"[RT.Test] FAIL: {name}\n  Reason: {state.Reason}\n  Details: {state.Details}");
        }
        WriteLogFile();
    }

    void WriteLogFile()
    {
        var logPath = Path.Combine(KSPUtil.ApplicationRootPath, "Logs", "RemoteTech.InGameTests.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));

        var sb = new StringBuilder();
        sb.AppendLine($"RemoteTech in-game test results - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"{passCount} passed, {failCount} failed ({results.states.Count} total)");
        sb.AppendLine();
        foreach (var state in results.states)
        {
            var name = state.Info?.Name ?? "(unnamed)";
            sb.AppendLine($"[{(state.Succeeded ? "PASS" : "FAIL")}] {name}");
            if (!state.Succeeded)
            {
                if (!string.IsNullOrEmpty(state.Reason)) sb.AppendLine($"  Reason: {state.Reason}");
                if (!string.IsNullOrEmpty(state.Details)) sb.AppendLine($"  Details: {state.Details}");
            }
        }
        File.WriteAllText(logPath, sb.ToString());
        Debug.Log($"[RT.Test] Results written to {logPath}");
    }

    void OnGUI()
    {
        if (!showWindow || results == null) return;
        GUI.skin = HighLogic.Skin;
        Styles.Init();
        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "RemoteTech Test Results");
    }

    void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{passCount} passed, {failCount} failed",
            failCount > 0 ? Styles.failLabel : Styles.passLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Re-run")) RunTests();
        if (GUILayout.Button("Close")) { showWindow = false; button?.SetFalse(false); }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var state in results.states)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(state.Succeeded ? "PASS" : "FAIL",
                state.Succeeded ? Styles.passLabel : Styles.failLabel, GUILayout.Width(40));
            GUILayout.Label(state.Info?.Name ?? "(unnamed)");
            GUILayout.EndHorizontal();
            if (!state.Succeeded)
            {
                if (!string.IsNullOrEmpty(state.Reason))
                    GUILayout.Label("  Reason: " + state.Reason, Styles.detailLabel);
                if (!string.IsNullOrEmpty(state.Details))
                    GUILayout.Label("  Details: " + state.Details, Styles.detailLabel);
            }
        }
        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    static class Styles
    {
        static bool initialized;
        internal static GUIStyle passLabel;
        internal static GUIStyle failLabel;
        internal static GUIStyle detailLabel;

        internal static void Init()
        {
            if (initialized) return;
            initialized = true;
            passLabel = new GUIStyle(HighLogic.Skin.label) { normal = { textColor = Color.green } };
            failLabel = new GUIStyle(HighLogic.Skin.label) { normal = { textColor = Color.red } };
            detailLabel = new GUIStyle(HighLogic.Skin.label)
            {
                normal = { textColor = new Color(1f, 0.8f, 0.4f) },
                wordWrap = true,
                fontSize = 11,
            };
        }
    }
}
