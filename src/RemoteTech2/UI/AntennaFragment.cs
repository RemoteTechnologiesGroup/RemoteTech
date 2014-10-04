using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class AntennaFragment : IFragment, IDisposable
    {
        private class Entry
        {
            public String Text { get; set; }
            public Guid Guid { get; set; }
            public Color Color;
            public List<Entry> SubEntries { get; private set; }
            public bool Expanded { get; set; }
            public int Depth { get; set; }

            public Entry()
            {
                SubEntries = new List<Entry>();
                Expanded = true;
            }
        }

        public IAntenna Antenna { 
            get { return mAntenna; }
            set { if (mAntenna != value) { mAntenna = value; Refresh(); } }
        }
        private IAntenna mAntenna;
        private Vector2 mScrollPosition = Vector2.zero;
        private Entry mRootEntry = new Entry();
        private Entry mSelection;
        public TargetInfoPopup mTargetInfoPopup;

        public AntennaFragment(IAntenna antenna)
        {
            Antenna = antenna;
            RTCore.Instance.Satellites.OnRegister += Refresh;
            RTCore.Instance.Satellites.OnUnregister += Refresh;
            RTCore.Instance.Antennas.OnUnregister += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.OnRegister -= Refresh;
                RTCore.Instance.Satellites.OnUnregister -= Refresh;
                RTCore.Instance.Antennas.OnUnregister -= Refresh;
            }
        }

        public void Draw()
        {
            if (this.mTargetInfoPopup != null)
            {
                this.mTargetInfoPopup.Hide();
            }

            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
            Color pushCtColor = GUI.contentColor;
            Color pushBgColor = GUI.backgroundColor;
            TextAnchor pushAlign = GUI.skin.button.alignment;
            try {
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                // Depth-first tree traversal.
                Stack<Entry> dfs = new Stack<Entry>();
                foreach (Entry child in mRootEntry.SubEntries)
                {
                    dfs.Push(child);
                }
                while (dfs.Count > 0)
                {
                    Entry current = dfs.Pop();
                    GUI.backgroundColor = current.Color;

                    string diagnosis;
                    KeyValuePair<string, Color> temp = NetworkFeedback.tryConnection(Antenna, current.Guid);
                    diagnosis = temp.Key;
                    if (diagnosis.Length > 0) {
                        diagnosis = " (" + diagnosis + ")";
                    }
                    GUI.contentColor = temp.Value;

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(current.Depth * (GUI.skin.button.margin.left + 24));
                        if (current.SubEntries.Count > 0)
                        {
                            RTUtil.Button(current.Expanded ? " <" : " >",
                                () =>
                                {
                                    current.Expanded = !current.Expanded;
                                }, GUILayout.Width(24));
                        }
                        RTUtil.StateButton(current.Text + diagnosis, mSelection == current ? 1 : 0, 1,
                            (s) =>
                            {
                                mSelection = current;
                                Antenna.Target = mSelection.Guid;
                            });

                        // Mouse is over the last button
                        if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && this.mTargetInfoPopup != null)
                        {
                            this.mTargetInfoPopup.Show();
                            this.mTargetInfoPopup.setYPositionToMouse();
                            this.mTargetInfoPopup.setPositionToRight();
                            // change the popup x position to this element relativ to the parent position
                            //this.mTargetInfoPopup.changePosition(GUILayoutUtility.GetLastRect().x + GUILayoutUtility.GetLastRect().width + 50,0);
                            this.mTargetInfoPopup.setTarget(current.Text);
                        }

                    }
                    GUILayout.EndHorizontal();

                    if (current.Expanded)
                    {
                        foreach (Entry child in current.SubEntries)
                        {
                            dfs.Push(child);
                        }
                    }
                }

            } finally {
                GUILayout.EndScrollView();
                GUI.skin.button.alignment = pushAlign;
                GUI.backgroundColor = pushBgColor;
                GUI.contentColor = pushCtColor;
            }
        }

        public void Refresh(IAntenna sat) { if (sat == Antenna) { Antenna = null; } }
        public void Refresh(ISatellite sat) { Refresh(); }
        public void Refresh()
        {
            Dictionary<CelestialBody, Entry> mEntries = new Dictionary<CelestialBody, Entry>();

            mRootEntry = new Entry();
            mSelection = new Entry()
            {
                Text = "No Target",
                Guid = Guid.Empty,
                Color = Color.white,
                Depth = 0,
            };
            mRootEntry.SubEntries.Add(mSelection);

            if (Antenna == null) return;

            var activeVesselEntry = new Entry()
            {
                Text = "Active Vessel",
                Guid = NetworkManager.ActiveVesselGuid,
                Color = Color.white,
                Depth = 0,
            };
            mRootEntry.SubEntries.Add(activeVesselEntry);
            if (Antenna.Target == activeVesselEntry.Guid)
            {
                mSelection = activeVesselEntry;
            }


            // Add the planets
            foreach (var cb in RTCore.Instance.Network.Planets)
            {
                if (!mEntries.ContainsKey(cb.Value))
                {
                    mEntries[cb.Value] = new Entry();
                }

                Entry current = mEntries[cb.Value];
                current.Text = cb.Value.bodyName;
                current.Guid = cb.Key;
                current.Color = cb.Value.GetOrbitDriver() != null ? cb.Value.GetOrbitDriver().orbitColor : Color.yellow;
                current.Color.a = 1.0f;

                if (cb.Value.referenceBody != cb.Value)
                {
                    CelestialBody parent = cb.Value.referenceBody;
                    if (!mEntries.ContainsKey(parent))
                    {
                        mEntries[parent] = new Entry();
                    }
                    mEntries[parent].SubEntries.Add(current);
                }
                else
                {
                    mRootEntry.SubEntries.Add(current);
                }

                if (cb.Key == Antenna.Target)
                {
                    mSelection = current;
                }
            }

            // Sort the lists based on semi-major axis. In reverse because of how we render it.
            foreach (var entryPair in mEntries)
            {
                entryPair.Value.SubEntries.Sort((b, a) =>
                {
                    return RTCore.Instance.Network.Planets[a.Guid].orbit.semiMajorAxis.CompareTo(
                           RTCore.Instance.Network.Planets[b.Guid].orbit.semiMajorAxis);
                });
            }

            // Add the satellites.
            foreach (ISatellite s in RTCore.Instance.Network)
            {
                if (s.Guid == Antenna.Guid) continue;
                Entry current = new Entry()
                {
                    Text = s.Name,
                    Guid = s.Guid,
                    Color = Color.white,
                };
                mEntries[s.Body].SubEntries.Add(current);

                if (s.Guid == Antenna.Target)
                {
                    mSelection = current;
                }
            }

            // Set a local depth variable so we can refer to it when rendering.
            mRootEntry.SubEntries.Reverse();
            Stack<Entry> dfs = new Stack<Entry>();
            foreach (Entry child in mRootEntry.SubEntries)
            {
                child.Depth = 0;
                dfs.Push(child);
            }
            while (dfs.Count > 0)
            {
                Entry current = dfs.Pop();
                foreach (Entry child in current.SubEntries)
                {
                    child.Depth = current.Depth + 1;
                    dfs.Push(child);
                }
            }
        }
    }
}
