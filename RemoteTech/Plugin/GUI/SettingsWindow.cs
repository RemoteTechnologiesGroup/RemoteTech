using System;
using UnityEngine;

namespace RemoteTech {
    public class SettingsWindow : AbstractWindow {
        private SettingsFragment mFragment;

        public SettingsWindow() : base("RemoteTech Configuration", new Rect(100, 100, 0, 0)) {}

        public override void Window(int id) {
            base.Window(id);
            GUILayout.BeginVertical();
            {
                mFragment.Draw();
                GUILayout.BeginHorizontal();
                {
                    RTUtil.Button("Apply", () => {
                        mFragment.SaveSettings();
                        mFragment.LoadSettings();
                    });
                    RTUtil.Button("Confirm", () => {
                        mFragment.SaveSettings();
                        Hide();
                    });
                    RTUtil.Button("Close", () => Hide());
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        public override void Show() {
            mFragment = new SettingsFragment();
            base.Show();
        }

        public override void Hide() {
            base.Hide();
            mFragment = null;
        }
    }

    public class SettingsFragment : IFragment {
        public void Draw() {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition,
                                                            GUILayout.MinHeight(300),
                                                            GUILayout.MinWidth(300));
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Signal speed: ");
                        RTUtil.TextField(ref mSignalSpeed, GUILayout.Width(150));
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        private Vector2 mScrollPosition;
        private String mSignalSpeed;

        public SettingsFragment() {
            mScrollPosition = Vector2.zero;
            LoadSettings();
        }

        public void LoadSettings() {
            Settings settings = RTCore.Instance.Settings;
            mSignalSpeed = settings.SIGNAL_SPEED.ToString();
        }

        public void SaveSettings() {
            Settings settings = RTCore.Instance.Settings;
        }
    }
}
