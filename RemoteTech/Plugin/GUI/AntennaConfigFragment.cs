using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class AntennaConfigFragment : IFragment, IDisposable {
        internal struct Entry {
            public String Text { get; set; }
            public Guid Guid { get; set; }
            public Color Color { get; set; }
        }

        public void Draw() {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200), GUILayout.MinHeight(200));
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
                {
                    Color push = GUI.color;
                    for (int i = 0; i < mEntries.Count; i++) {
                        GUI.color = mEntries[i].Color;
                        RTUtil.StateButton(mEntries[i].Text, mSelection, i, s => {
                            mSelection = (s > 0) ? s : 0;
                            mFocus.DishTarget = mEntries[mSelection].Guid;
                            mOnUpdate.Invoke(mFocus);
                        });    
                    }
                    GUI.color = push;
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        private int mSelection;
        private Vector2 mScrollPosition = Vector2.zero;
        private List<Entry> mEntries = new List<Entry>();

        private readonly IAntenna mFocus;
        private readonly OnClick mForceClose;
        private readonly OnAntenna mOnUpdate;

        public AntennaConfigFragment(IAntenna antenna, OnClick forceClose, OnAntenna onUpdate) {
            mFocus = antenna;
            mForceClose = forceClose;
            mOnUpdate = onUpdate;

            RTCore.Instance.Antennas.Unregistered += OnAntenna;

            RebuildTargetList();
        }

        public void RebuildTargetList() {
            mEntries.Clear();
            mEntries.Capacity = RTCore.Instance.Network.Count +
                                RTCore.Instance.Network.Planets.Count + 1;
            mEntries.Add(new Entry() {Text = "No Target", Guid = Guid.Empty, Color = Color.white});
            var planetIterator = RTCore.Instance.Network.Planets.GetEnumerator();
            foreach(var planetEntry in RTCore.Instance.Network.Planets) {
                CelestialBody cb = planetEntry.Value;
                mEntries.Add(new Entry() {
                    Text = cb.name,
                    Guid = planetEntry.Key,
                    Color = Color.white,
                });
                foreach (ISatellite s in RTCore.Instance.Network.Where(s => s.Body == cb)) {
                    mEntries.Add(new Entry() {
                        Text = "> " + s.Name,
                        Guid = s.Guid,
                        Color = Color.white,
                    });
                }
            }
            mSelection = mEntries.FindIndex(x => x.Guid == mFocus.DishTarget);
        }

        private void OnAntenna(IAntenna antenna) {
            if (antenna == mFocus) {
                mForceClose.Invoke();
            }
        }

        public void Dispose() {
            if (RTCore.Instance != null) {
                RTCore.Instance.Antennas.Unregistered -= OnAntenna;
            }
        }
    }
}