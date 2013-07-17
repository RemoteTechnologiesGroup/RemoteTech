using System;
using UnityEngine;

namespace RemoteTech {

    public class ProgcomFragment : IFragment {
        private String mCommand = "";

        private String Output1 {
            get { return mFlightComputer.Progcom.IO.Output1.ToString(); }
        }

        private String Output2 {
            get { return mFlightComputer.Progcom.IO.Output2.ToString(); }
        }

        private String Output3 {
            get { return mFlightComputer.Progcom.IO.Output3.ToString(); }
        }

        private String Output4 {
            get { return mFlightComputer.Progcom.IO.Output4.ToString(); }
        }

        private String OutputMsg {
            get { return mFlightComputer.Progcom.IO.OutputMsg.ToString(); }
        }

        private Vector2 mScrollPosition = Vector2.zero;
        private GUIStyle mTextConsole;
        private bool mEnableMonitor;
        private float mMonitorDimension;

        private readonly FlightComputer mFlightComputer;

        public ProgcomFragment(FlightComputer fc) {
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
            ProgcomIO io = mFlightComputer.Progcom.IO;

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
                        GUILayout.Label(io.Numpad.ToString(), GUI.skin.textField);

                        GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("7", () => io.Numpad = 7, GUILayout.Width(width4));
                            RTUtil.Button("8", () => io.Numpad = 8, GUILayout.Width(width4));
                            RTUtil.Button("9", () => io.Numpad = 9, GUILayout.Width(width4));
                            RTUtil.Button("R", () => io.Numpad = ProgcomIO.Reset,
                                GUILayout.Width(width4));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("4", () => io.Numpad = 4, GUILayout.Width(width4));
                            RTUtil.Button("5", () => io.Numpad = 5, GUILayout.Width(width4));
                            RTUtil.Button("6", () => io.Numpad = 6, GUILayout.Width(width4));
                            RTUtil.Button("C", () => io.Numpad = ProgcomIO.Clear,
                                GUILayout.Width(width4));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("1", () => io.Numpad = 1, GUILayout.Width(width4));
                            RTUtil.Button("2", () => io.Numpad = 2, GUILayout.Width(width4));
                            RTUtil.Button("3", () => io.Numpad = 3, GUILayout.Width(width4));
                            RTUtil.Button("-", () => io.Numpad = ProgcomIO.Minus,
                                GUILayout.Width(width4));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            RTUtil.Button("0", () => io.Numpad = 0, GUILayout.Width(width4));
                            RTUtil.Button("ENTER", () => io.Numpad = ProgcomIO.Enter,
                                GUILayout.ExpandWidth(true));
                            RTUtil.Button("+", () => io.Numpad = ProgcomIO.Plus,
                                GUILayout.Width(width4));
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUILayout.Width(150));
                    {
                        GUILayout.Label("M1: " + Output1, GUI.skin.textField);
                        GUILayout.Label("M2: " + Output2, GUI.skin.textField);
                        GUILayout.Label("M3: " + Output3, GUI.skin.textField);
                        GUILayout.Label("M4: " + Output4, GUI.skin.textField);
                        GUILayout.Label("MSG: " + OutputMsg, GUI.skin.textField);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Height(100));
                {
                    GUILayout.Box(io.Console, mTextConsole, GUILayout.ExpandHeight(true));
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
            if (Event.current.type == EventType.Repaint) {
                mMonitorDimension = GUILayoutUtility.GetLastRect().height -
                    GUI.skin.box.padding.top - GUI.skin.box.padding.bottom;
            }
            GUILayout.BeginVertical();
            {
                if (mEnableMonitor) {
                    GUILayout.Box(io.Monitor,  
                        GUILayout.Width(mMonitorDimension), GUILayout.Height(mMonitorDimension));  
                }
            }
            GUILayout.EndVertical();
        }

        private void Parse(String s) {
            ProgcomIO io = mFlightComputer.Progcom.IO;
            ProgcomUnit progcom = mFlightComputer.Progcom;
            if (String.IsNullOrEmpty(s))
                return;
            io.Log("> " + s);
            String[] split = s.Split(' ');
            switch (split[0]) {
                default:
                    io.Log("Parsing error.");
                    break;
                case "upload":
                    if (split.Length < 2) {
                        io.Log("Missing argument.");
                    } else {
                        io.Upload(split[1]); 
                    }
                    break;
                case "run":
                    progcom.Run();
                    io.Log("Running...");
                    break;
                case "pause":
                    progcom.Pause();
                    io.Log("Stopping...");
                    break;
                case "resume":
                    io.Log("Resuming...");
                    progcom.Resume();
                    break;
                case "reset":
                    io.Log("Resetting...");
                    progcom.Reset();
                    break;
            }
        }
    }

}