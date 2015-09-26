using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech.UI
{
    public class AntennaFragment : IFragment, IDisposable
    {
        public class Entry
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
            set { if (mAntenna != value) { mAntenna = value; RefreshPlanets(); Refresh(); } }
        }
        private IAntenna mAntenna;
        private Vector2 mScrollPosition = Vector2.zero;
        /// <summary>The tree of (real or virtual) targets displayed in this fragment.</summary>
        /// <invariant>No Entry object appears in the tree pointed to by mRootEntry more than once.</invariant>
        private Entry mRootEntry = new Entry();
        /// <summary>The Entry corresponding to the currently selected target, if any.</summary>
        private Entry mSelection;
        /// <summary>The Entry corresponding to the currently selected target, if any.</summary>
        private Entry mCurrentMouseOverEntry = null;
        /// <summary>Callback trigger for mouse over a list entry</summary>
        public Action onMouseOverListEntry = delegate { };
        /// <summary>Callback trigger for mouse out of a list entry</summary>
        public Action onMouseOutListEntry = delegate { };
        /// <summary>Current entry of the mouse</summary>
        public Entry mouseOverEntry { get { return mCurrentMouseOverEntry; } private set { mCurrentMouseOverEntry = value; } }
        /// <summary>Flag to trigger the onMouseover event</summary>
        public bool triggerMouseOverListEntry = false;

        /// <summary>The Entries corresponding to loaded celestial bodies.</summary>
        private Dictionary<CelestialBody, Entry> mEntries;      // Current planet list
        private int refreshCounter = 0;

        public AntennaFragment(IAntenna antenna)
        {
            Antenna = antenna;
            RTCore.Instance.Satellites.OnRegister += Refresh;
            RTCore.Instance.Satellites.OnUnregister += Refresh;
            RTCore.Instance.Antennas.OnUnregister += Refresh;
            RefreshPlanets();
            Refresh();
        }

        public void Dispose()
        {
            triggerMouseOverListEntry = false;

            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.OnRegister -= Refresh;
                RTCore.Instance.Satellites.OnUnregister -= Refresh;
                RTCore.Instance.Antennas.OnUnregister -= Refresh;
            }
        }

        public void Draw()
        {
            // Allow update for non-triggering changes (e.g., changing map view filters or changing a vessel's type)
            // This is the best way I could find to do periodic refreshes; 
            //  RTCore.Instance.InvokeRepeating() would require a search for instances 
            //  of AntennaFragment, and would keep running after all target windows 
            //  closed. Replace with something less clunky later! -- Starstrider42
            if (++refreshCounter >= 100) {
                Refresh();
                refreshCounter = 0;
            }

            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
            Color pushColor = GUI.backgroundColor;
            // starstriders changes
            //Color pushCtColor = GUI.contentColor;
            //Color pushBgColor = GUI.backgroundColor;
            TextAnchor pushAlign = GUI.skin.button.alignment;
            try {
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                // Depth-first tree traversal.
                Stack<Entry> dfs = new Stack<Entry>();
                foreach (Entry child in mRootEntry.SubEntries)
                {
                    dfs.Push(child);
                }

                // Set the inital mouseover to the selected entry
                mouseOverEntry = mSelection;
                
                while (dfs.Count > 0)
                {
                    Entry current = dfs.Pop();
                    GUI.backgroundColor = current.Color;
                    
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

                        RTUtil.StateButton(current.Text, mSelection == current ? 1 : 0, 1,
                            (s) =>
                            {
                                mSelection = current;
                                Antenna.Target = mSelection.Guid;
                            });

                        // Mouse is over the button
                        if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && triggerMouseOverListEntry)
                        {
                            // reset current entry
                            mouseOverEntry = null;
                            if (current.Text.ToLower() != "active vessel" && current.Text.ToLower() != "no target")
                            {
                                mouseOverEntry = current;
                            }
                            onMouseOverListEntry.Invoke();
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
                GUI.backgroundColor = pushColor;
            }
        }

        public void Refresh(IAntenna sat) { if (sat == Antenna) { Antenna = null; } }
        /// <summary>Rebuilds list of target vessels</summary>
        /// <description>Rebuilds the list of target vessels, preserving the rest of the target list state. Does 
        ///     not alter planets or special targets, call RefreshPlanets() for that.</description>
        /// <param name="sat">The satellite whose status has just changed.</param>
        public void Refresh(ISatellite sat) { Refresh(); }
        /// <summary>Rebuilds list of target vessels</summary>
        /// <description>Rebuilds the list of target vessels, preserving the rest of the target list state. Does 
        ///     not alter planets or special targets, call RefreshPlanets() for that.</description>
        public void Refresh()
        {
            // Clear the satellites
            RemoveVessels(mRootEntry);

            if (Antenna == null) return;

            // Add the satellites
            foreach (ISatellite s in RTCore.Instance.Network)
            {
                if (s.Guid == Antenna.Guid) continue;

                if (s.parentVessel != null && !MapViewFiltering.CheckAgainstFilter(s.parentVessel)) {
                    continue;
                }

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

        /// <summary>Full refresh of target list</summary>
        /// <description>Rebuilds the list of target buttons from scratch, including special targets and 
        /// planets. Does not build vessel list, call Refresh() for that.</description>
        /// <remarks>Calling this function wipes all information about which submenus were open or closed.</remarks>
        private void RefreshPlanets() {
            mEntries = new Dictionary<CelestialBody, Entry>();

            mRootEntry = new Entry();
            mSelection = new Entry()
            {
                Text = "No Target",
                Guid = new Guid(RTSettings.Instance.NoTargetGuid),
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
            mRootEntry.SubEntries.Reverse();
        }

        /// <summary>Removes all buttons representing specific vessels, while preserving celestial bodies 
        ///     and special targets</summary>
        /// <param name="root">The top of the tree from which to remove vessels.</param>
        private void RemoveVessels(Entry root) {
            List<Entry> vesselList = new List<Entry>();

            foreach (Entry subentry in root.SubEntries) {
                // Is it a vessel?
                if (subentry.Guid != Guid.Empty && subentry.Guid != NetworkManager.ActiveVesselGuid 
                    && !mEntries.ContainsValue(subentry)) {
                    // List<T> iterator is invalidated by modifications, so do all deletions later
                    vesselList.Add(subentry);
                } else {
                    RemoveVessels(subentry);
                }
            }
            // Do deletions without relying on an iterator for root.SubEntries
            foreach (Entry vessel in vesselList) {
                root.SubEntries.Remove(vessel);
            }
        }
    }
}
