using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class SatelliteConfigFragment : IFragment, IDisposable {
        public void Draw() {
            GUILayout.BeginVertical(GUILayout.Width(200), GUILayout.ExpandHeight(true));
            {
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
                    {
                        for (int i = 0; i < mFocusAntennas.Count; i++) {
                            RTUtil.StateButton(mFocusAntennas[i].Name, mSelection == i, s => {
                                mSelection = s == 1 ? i : 0;
                                mOnClick.Invoke(mFocusAntennas[i]);
                            });
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Box(mFocus.Name);
                    RTUtil.Button("Name", () => mFocus.SignalProcessor.Vessel.RenameVessel(),
                                  GUILayout.ExpandWidth(false));
                }
                GUILayout.EndHorizontal();
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
            mFocusAntennas = RTCore.Instance.Antennas.For(mFocus).Where(a => a.CanTarget).ToList();
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