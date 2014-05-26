using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class AntennaFragment : IFragment, IDisposable
    {
        private enum Mode
        {
            PlanetSatellite,
            Planet,
            Satellite,
            Group
        }
        private class Entry
        {
            public String Text { get; set; }
            public Target Target { get; set; }
            public Color Color;
            public List<Entry> SubEntries { get; private set; }
            public bool Expanded { get; set; }
            public int Depth { get; set; }
            public double Priority { get; set; }

            public Entry()
            {
                SubEntries = new List<Entry>();
                Expanded = true;
                Priority = 0;
            }
        }

        public IAntenna Antenna { 
            get { return antenna; }
            set { if (antenna != value) { antenna = value; Refresh(); } }
        }

        private IAntenna antenna;
        private Vector2 scrollPosition1 = Vector2.zero;
        private Vector2 scrollPosition2 = Vector2.zero;
        private Entry rootEntry = new Entry();
        private Entry selection;

        private int targetIndex = 0;
        private Mode currentMode = Mode.PlanetSatellite;

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
            RTGui.HorizontalBlock(() =>
            {
                RTGui.StateButton("P/S", currentMode, Mode.PlanetSatellite, (s) => OnClickMode(Mode.PlanetSatellite, s));
                RTGui.StateButton("P", currentMode, Mode.Planet, (s) => OnClickMode(Mode.Planet, s));
                RTGui.StateButton("S", currentMode, Mode.Satellite, (s) => OnClickMode(Mode.Satellite, s));
                RTGui.StateButton("G", currentMode, Mode.Group, (s) => OnClickMode(Mode.Group, s));
                GUILayout.FlexibleSpace();
            });
            switch (currentMode)
            {
                case Mode.PlanetSatellite:
                    DrawPlanetSatelliteTree();
                    break;
                case Mode.Planet:
                    break;
                case Mode.Satellite:
                    break;
                case Mode.Group:
                    break;
            }
        }

        private void OnClickMode(Mode m, int s)
        {
            currentMode = (s >= 0) ? m : Mode.PlanetSatellite;
        }
        private void DrawPlanetList()
        {

        }

        private void DrawPlanetSatelliteTree()
        {
            RTGui.ScrollViewBlock(ref scrollPosition1, () =>
            {
                Color pushColor = GUI.backgroundColor;
                TextAnchor pushAlign = GUI.skin.button.alignment;
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                // Depth-first tree traversal.
                Stack<Entry> dfs = new Stack<Entry>();
                foreach (Entry child in rootEntry.SubEntries)
                {
                    dfs.Push(child);
                }
                while (dfs.Count > 0)
                {
                    Entry current = dfs.Pop();
                    GUI.backgroundColor = current.Color;

                    RTGui.HorizontalBlock(() =>
                    {
                        GUILayout.Space(current.Depth * (GUI.skin.button.margin.left + 24));
                        if (current.SubEntries.Count > 0)
                        {
                            RTGui.Button(current.Expanded ? " <" : " >", () =>
                            {
                                current.Expanded = !current.Expanded;
                            }, GUILayout.Width(24));
                        }
                        RTGui.StateButton(current.Text, selection, current, (s) =>
                        {
                            selection = current;
                            Antenna.Targets[targetIndex] = current.Target;
                        });
                    });

                    if (current.Expanded)
                    {
                        foreach (Entry child in current.SubEntries)
                        {
                            dfs.Push(child);
                        }
                    }
                }

                GUI.skin.button.alignment = pushAlign;
                GUI.backgroundColor = pushColor;
            });
        }

        public void Refresh(IAntenna sat) { if (sat == Antenna) { Antenna = null; } }
        public void Refresh(ISatellite sat) { Refresh(); }
        public void Refresh()
        {
            Dictionary<ICelestialBody, Entry> entries = new Dictionary<ICelestialBody, Entry>();

            rootEntry = new Entry();
            selection = new Entry()
            {
                Text = "No Target",
                Target = Target.Empty,
                Color = Color.white,
                Depth = 0,
            };
            rootEntry.SubEntries.Add(selection);

            if (Antenna == null) return;

            // Add the planets
            foreach (var cb in RTCore.Instance.Bodies)
            {
                if (!entries.ContainsKey(cb))
                {
                    entries[cb] = new Entry();
                }

                var current = entries[cb];
                current.Text = cb.Name;
                current.Target = Target.Planet(cb);
                current.Color = cb.Color;
                current.Color.a = 1.0f;
                current.Priority = cb.SemiMajorAxis;

                // Does it have a parent? Add it to the hierarchy, else, add it to root (Sun).
                if (cb.Parent != cb)
                {
                    var parent = cb.Parent;
                    if (!entries.ContainsKey(parent))
                    {
                        entries[parent] = new Entry();
                    }
                    entries[parent].SubEntries.Add(current);
                }
                else
                {
                    rootEntry.SubEntries.Add(current);
                }

                // Set the selection if the current target is this body.
                if (Antenna.Targets.Count < targetIndex && current.Target == Antenna.Targets[targetIndex])
                {
                    selection = current;
                }
            }

            // Sort the lists based on semi-major axis. In reverse because of how we render it.
            foreach (var entryPair in entries)
            {
                entryPair.Value.SubEntries.Sort((b, a) =>
                {
                    return a.Priority.CompareTo(b.Priority);
                });
            }

            // Add the satellites.
            foreach (ISatellite s in RTCore.Instance.Network)
            {
                // Do not show if the satellite belongs to the current antenna (they share a Guid).
                if (s.Guid == Antenna.Guid) continue;

                Entry current = new Entry()
                {
                    Text = s.Name,
                    Target = Target.Single(s),
                    Color = Color.white,
                };

                entries[s.Body].SubEntries.Add(current);

                // Set current selection to the satellite if it's the antenna's target.
                if (Antenna.Targets.Count < targetIndex && current.Target.Equals(Antenna.Targets[targetIndex]))
                {
                    selection = current;
                }
            }

            // Set a local depth variable so we can refer to it when rendering.
            rootEntry.SubEntries.Reverse();
            Stack<Entry> dfs = new Stack<Entry>();
            foreach (Entry child in rootEntry.SubEntries)
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
