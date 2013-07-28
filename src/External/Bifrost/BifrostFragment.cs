using System;
using UnityEngine;

namespace RemoteTech {

    public class BifrostFragment : IFragment {
        private String mCommand = "";

        private Vector2 mScrollPosition = Vector2.zero;
        private GUIStyle mTextConsole;
        private bool mEnableMonitor;

        private readonly FlightComputer mFlightComputer;

        public BifrostFragment(FlightComputer fc) {
            mFlightComputer = fc;
        }

        public void Draw() {
            if (mTextConsole == null) {
                mTextConsole = new GUIStyle(GUI.skin.box) {
                    wordWrap = true,
                    alignment = TextAnchor.LowerLeft
                };
            }

            float width3 = 125 / 3 - GUI.skin.button.margin.right * 2.0f / 3.0f;
            float width4 = 125 / 4 - GUI.skin.button.margin.right * 3.0f / 4.0f;
            GenericKeyboard k = mFlightComputer.Bifrost.Keyboard;
            LEM1802 m = mFlightComputer.Bifrost.Monitor;

            if (Event.current.type == EventType.KeyDown &&
                        Event.current.keyCode == KeyCode.Return) {
                Parse(mCommand);
                mCommand = "";
                Event.current.Use();
            }

            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.Width(125));
                    {
                        GUI.SetNextControlName("kb");
                        if (GUI.GetNameOfFocusedControl() == "kb") {
                            k.HandleKeyEvent();
                        }
                        GUILayout.TextField("KB: ", GUI.skin.textField);

                        /*GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("7", () => k.Numpad = 7, GUILayout.Width(width4));
                            RTUtil.Button("8", () => k.Numpad = 8, GUILayout.Width(width4));
                            RTUtil.Button("9", () => k.Numpad = 9, GUILayout.Width(width4));
                            GUILayout.Label("");
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("4", () => k.Numpad = 4, GUILayout.Width(width4));
                            RTUtil.Button("5", () => k.Numpad = 5, GUILayout.Width(width4));
                            RTUtil.Button("6", () => k.Numpad = 6, GUILayout.Width(width4));
                            GUILayout.Label("");
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("1", () => k.Numpad = 1, GUILayout.Width(width4));
                            RTUtil.Button("2", () => k.Numpad = 2, GUILayout.Width(width4));
                            RTUtil.Button("3", () => k.Numpad = 3, GUILayout.Width(width4));
                            RTUtil.Button("-", () => k.Numpad = ProgcomIO.Minus,
                                GUILayout.Width(width4));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("0", () =>k.Numpad = 0, GUILayout.Width(width4));
                            RTUtil.Button("ENTER", () => k.Numpad = ProgcomIO.Enter,
                                GUILayout.ExpandWidth(true));
                            RTUtil.Button("+", () => k.Numpad = ProgcomIO.Plus,
                                GUILayout.Width(width4));
                        }
                        GUILayout.EndHorizontal();*/
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUILayout.Width(150));
                    {
                        GUILayout.Label("PC: " + mFlightComputer.Bifrost.mCPU.CPU.PC);
                        GUILayout.Label("A: " + mFlightComputer.Bifrost.mCPU.CPU.A);
                        GUILayout.Label("B: " + mFlightComputer.Bifrost.mCPU.CPU.B);
                        GUILayout.Label("C: " + mFlightComputer.Bifrost.mCPU.CPU.C);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
                {
                    GUILayout.Box(mFlightComputer.Bifrost.Console, mTextConsole, GUILayout.ExpandHeight(true));
                }
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                {
                    RTUtil.TextField(ref mCommand);
                    RTUtil.StateButton("MON", mEnableMonitor ? 1 : 0, 1,
                        (s) => mEnableMonitor = (s > 0), GUILayout.Width(40));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Height(3 * m.Texture.height));
            {
                if (mEnableMonitor) {
                    GUILayout.Label("", GUILayout.Width(3 * m.Texture.width), GUILayout.Height(3 * m.Texture.height));
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), m.Texture, ScaleMode.StretchToFill);
                } else {
                    GUILayout.Label("");
                }
            }
            GUILayout.EndVertical();
        }

        private void Parse(String s) {
            BifrostUnit bu = mFlightComputer.Bifrost;
            if (String.IsNullOrEmpty(s))
                return;
            bu.Log("> " + s);
            String[] split = s.Split(' ');
            switch (split[0]) {
                default:
                    bu.Log("Parsing error.");
                    break;
                case "upload":
                    if (split.Length < 2) {
                        bu.Log("Missing argument.");
                    } else {
                        bu.Upload(split[1]);
                    }
                    break;
                case "run":
                    bu.Run();
                    bu.Log("Running...");
                    break;
                case "pause":
                    bu.Pause();
                    bu.Log("Stopping...");
                    break;
                case "resume":
                    bu.Log("Resuming...");
                    bu.Resume();
                    break;
                case "reset":
                    bu.Log("Resetting...");
                    bu.Reset();
                    break;
            }
        }
    }

}