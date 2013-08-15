using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class SatelliteConfigFragment : IFragment, IDisposable {
        public void Draw() {
            GUILayout.BeginVertical(GUILayout.Width(200), GUILayout.ExpandHeight(true));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Box(mFocus.Name);
                    RTUtil.Button("Name", () => mFocus.Vessel.RenameVessel(),
                                  GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
                    {
                        Color pushColor = GUI.contentColor;
                        TextAnchor pushAlign = GUI.skin.button.alignment;
                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                        for (int i = 0; i < mFocusAntennas.Count; i++) {
                            GUI.contentColor = 
                                (mFocusAntennas[i].DishRange > 0) ? Color.green : Color.red;
                            String text = mFocusAntennas[i].Name + '\n' +
                                "Target: " + RTUtil.TargetName(mFocusAntennas[i].DishTarget);
                            RTUtil.StateButton(text, mSelection, i, s => {
                                mSelection = (s > 0) ? s : 0;
                                mOnClick.Invoke(mFocusAntennas[mSelection]);
                            });
                        }
                        GUI.skin.button.alignment = pushAlign;
                        GUI.contentColor = pushColor;
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private VesselSatellite mFocus;
        private List<IAntenna> mFocusAntennas;
        private Vector2 mScrollPosition = Vector2.zero;
        private int mSelection = -1;

        private readonly OnAntenna mOnClick;
        private readonly OnClick mForceClose;

        public SatelliteConfigFragment(VesselSatellite vs, OnClick forceClose, OnAntenna onClick) {
            mForceClose = forceClose;
            mOnClick = onClick;

            mFocus = vs;
            RebuildAntennaList();

            RTCore.Instance.Antennas.Registered += OnAntenna;
            RTCore.Instance.Antennas.Unregistered += OnAntenna;
            RTCore.Instance.Satellites.Unregistered += OnSatellite;
        }

        public void RebuildAntennaList() {
            mFocusAntennas = RTCore.Instance.Antennas[mFocus].Where(a => a.CanTarget).ToList();
        }

        private void OnSatellite(ISatellite satellite) {
            if (satellite == mFocus) {
                mOnClick.Invoke(null);
                mForceClose.Invoke();
            }
        }

        private void OnAntenna(IAntenna antenna) {
            if (mFocusAntennas.Contains(antenna)) {
                RebuildAntennaList();
            }
        }

        public void Dispose() {
            if (RTCore.Instance != null) {
                RTCore.Instance.Antennas.Registered -= OnAntenna;
                RTCore.Instance.Antennas.Unregistered -= OnAntenna;
                RTCore.Instance.Satellites.Unregistered -= OnSatellite;
            }
        }
    }
}