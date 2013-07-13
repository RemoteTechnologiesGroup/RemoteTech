using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech {
    public class AntennaConfigFragment : IFragment, IDisposable {
        private class Entry {
            public String Text { get; set; }
            public Guid Guid { get; set; }
            public Color Color { get; set; }
            public List<Entry> SubEntries { get; private set; }
            public bool Expanded { get; set; }
            public int Depth { get; set; }

            public Entry() {
                SubEntries = new List<Entry>();
                Expanded = true;
            }
        }

        public void Draw() {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
                {
                    Color pushColor = GUI.color;
                    TextAnchor pushAlign = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    // Depth-first tree traversal.
                    Stack<Entry> dfs = new Stack<Entry>();
                    foreach (Entry child in mRootEntry.SubEntries) {
                        dfs.Push(child);
                    }
                    while (dfs.Count > 0) {
                        Entry current = dfs.Pop();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(20 * current.Depth);
                            if (current.SubEntries.Count > 0) {
                                RTUtil.Button(current.Expanded ? "<" : ">", 
                                    () => {
                                        current.Expanded = !current.Expanded;
                                }, GUILayout.Width(18));   
                            }
                            RTUtil.StateButton(current.Text, mSelection == current ? 1 : 0, 1, 
                                (s) => {
                                    mSelection = current;
                                    mFocus.DishTarget = mSelection.Guid;
                                });  

                        }
                        GUILayout.EndHorizontal();

                        if(current.Expanded) {
                            foreach (Entry child in current.SubEntries) {
                                dfs.Push(child);
                            } 
                        }
                    }

                    GUI.skin.button.alignment = pushAlign;
                    GUI.color = pushColor;
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        private Entry mSelection;
        private Vector2 mScrollPosition = Vector2.zero;
        private Entry mRootEntry = new Entry();

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
            Dictionary<CelestialBody, Entry> mEntries = new Dictionary<CelestialBody, Entry>();

            mRootEntry = new Entry();
            mSelection = new Entry() {
                Text = "No Target",
                Guid = Guid.Empty,
                Color = Color.white,
            };
            mRootEntry.SubEntries.Add(mSelection);

            foreach (KeyValuePair<Guid, CelestialBody> cb in RTCore.Instance.Network.Planets) {
                if (!mEntries.ContainsKey(cb.Value)) {
                    mEntries[cb.Value] = new Entry();
                }

                Entry current = mEntries[cb.Value];
                current.Text = cb.Value.bodyName;
                current.Guid = cb.Key;
                current.Color = Color.white;

                if (cb.Value.referenceBody != cb.Value) {
                    CelestialBody parent = cb.Value.referenceBody;
                    if (!mEntries.ContainsKey(parent)) {
                        mEntries[parent] = new Entry();
                    }
                    mEntries[parent].SubEntries.Add(current);
                } else {
                    mRootEntry.SubEntries.Add(current);
                }

                if (cb.Key == mFocus.DishTarget) {
                    mSelection = current;
                }
            }

            // Sort the lists based on semi-major axis. In reverse because of how we render it.
            foreach (var entryPair in mEntries) {
                entryPair.Value.SubEntries.Sort((b, a) => {
                    return RTCore.Instance.Network.Planets[a.Guid].orbit.semiMajorAxis.CompareTo(
                           RTCore.Instance.Network.Planets[b.Guid].orbit.semiMajorAxis);
                });
            }

            // Add the satellites.
            foreach (ISatellite s in RTCore.Instance.Network) {
                Entry current = new Entry() {
                    Text = s.Name,
                    Guid = s.Guid,
                    Color = Color.white,
                };
                mEntries[s.Body].SubEntries.Add(current);

                if (s.Guid == mFocus.DishTarget) {
                    mSelection = current;
                }
            }

            // Set a local depth variable so we can refer to it when rendering.
            Stack<Entry> dfs = new Stack<Entry>();
            foreach (Entry child in mRootEntry.SubEntries) {
                child.Depth = 0;
                dfs.Push(child);
            }
            while (dfs.Count > 0) {
                Entry current = dfs.Pop();
                foreach (Entry child in current.SubEntries) {
                    child.Depth = current.Depth + 1;
                    dfs.Push(child);
                }
            }
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