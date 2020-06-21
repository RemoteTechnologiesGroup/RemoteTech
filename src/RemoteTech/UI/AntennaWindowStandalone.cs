using KSP.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;
using static RemoteTech.UI.AntennaFragment;

namespace RemoteTech.UI
{
    /// <summary>
    /// Lightweight version of AntennaWindow class (a selectable list of planets and vessels for antenna targeting)
    /// for scenarios that full-functionality RTCore will not work in, such as Editor
    /// </summary>
    public class AntennaWindowStandalone : AbstractWindow
    {
        private const float winWidth = 300, winHeight = 500, startX = 300, startY = 100;
        public static Guid Guid = new Guid("f3959a08-073a-4790-a74a-b78cd891ea64");
        private IAntenna mAntenna;
        public IAntenna Antenna
        {
            get { return mAntenna; }
            set { if (mAntenna != value) { mAntenna = value; } }
        }

        private Vector2 scrollPosition = Vector2.zero;
        private Dictionary<CelestialBody, Entry> celestialBodyEntryDict;
        private Entry rootEntry = new Entry();
        private Entry selectedEntry;

        public AntennaWindowStandalone(IAntenna antenna)
            : base(Guid, "Antenna Configuration", new Rect(startX, startY, winWidth, winHeight), WindowAlign.Floating)
        {
            mSavePosition = true;
            mAntenna = antenna;

            AddDefaultTargets();
            AddCelestialBodies();
            AddMissionControls();
            ComputeEntryDepths();
        }

        public override void Window(int uid)
        {
            if (Antenna == null)
            {
                Hide();
                return;
            }

            GUI.skin = HighLogic.Skin;
            GUILayout.BeginVertical(GUILayout.Width(winWidth), GUILayout.Height(winHeight));
            {
                DrawInterface();
            }
            GUILayout.EndVertical();
            base.Window(uid);
        }

        public void DrawInterface()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            Color pushColor = GUI.backgroundColor;
            TextAnchor pushAlign = GUI.skin.button.alignment;

            try
            {
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                // Depth-first tree traversal.
                Stack<Entry> dfs = new Stack<Entry>();
                for(int i=0;i< rootEntry.SubEntries.Count;i++)
                {
                    var child = rootEntry.SubEntries[i];
                    dfs.Push(child);
                }

                while (dfs.Count > 0)
                {
                    var current = dfs.Pop();
                    GUI.backgroundColor = current.Color;

                    // draw child
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(current.Depth * (GUI.skin.button.margin.left + 24));
                        if (current.SubEntries.Count > 0)
                        {
                            RTUtil.Button(current.Expanded ? " <" : " >",
                                () =>
                                {
                                    current.Expanded = !current.Expanded;
                                }, 
                                GUILayout.Width(24));
                        }

                        RTUtil.StateButton(current.Text, selectedEntry == current ? 1 : 0, 1,
                            (s) =>
                            {
                                selectedEntry = current;
                                Antenna.Target = selectedEntry.Guid;
                            });
                    }
                    GUILayout.EndHorizontal();

                    // draw child's grandchilden if expanded
                    if (current.Expanded)
                    {
                        for (int j = 0; j < current.SubEntries.Count; j++)
                        {
                            var child2 = current.SubEntries[j];
                            dfs.Push(child2);
                        }
                    }
                }

            }
            finally
            {
                GUILayout.EndScrollView();
                GUI.skin.button.alignment = pushAlign;
                GUI.backgroundColor = pushColor;
            }
        }

        private void AddDefaultTargets()
        {
            // Add "No Target" entry
            selectedEntry = new Entry() // selected entry by default
            {
                Text = Localizer.Format("#RT_ModuleUI_NoTarget"),//"No Target"
                Guid = new Guid(RTSettings.Instance.NoTargetGuid),
                Color = Color.white,
                Depth = 0,
            };
            rootEntry.SubEntries.Add(selectedEntry);

            if (Antenna == null) { return; }

            // Add "Active Vessel" entry
            var activeVesselEntry = new Entry()
            {
                Text = Localizer.Format("#RT_ModuleUI_ActiveVessel"),//"Active Vessel"
                Guid = NetworkManager.ActiveVesselGuid,
                Color = Color.white,
                Depth = 0,
            };
            rootEntry.SubEntries.Add(activeVesselEntry);

            // is it antenna's selected entry?
            if (Antenna.Target == activeVesselEntry.Guid)
            {
                selectedEntry = activeVesselEntry;
            }
        }

        private void AddCelestialBodies()
        {
            celestialBodyEntryDict = new Dictionary<CelestialBody, Entry>();

            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                var cb = FlightGlobals.Bodies[i];
                if (!celestialBodyEntryDict.ContainsKey(cb))
                {
                    celestialBodyEntryDict[cb] = new Entry();
                }

                var current = celestialBodyEntryDict[cb];
                current.Text = cb.bodyName;
                current.Guid = cb.Guid();
                current.Color = cb.GetOrbitDriver() != null ? cb.GetOrbitDriver().orbitColor : Color.yellow;
                current.Color.a = 1.0f;

                // have moons?
                if (cb.referenceBody != cb)
                {
                    CelestialBody parent = cb.referenceBody;
                    if (!celestialBodyEntryDict.ContainsKey(parent))
                    {
                        celestialBodyEntryDict[parent] = new Entry();
                    }
                    celestialBodyEntryDict[parent].SubEntries.Add(current);
                }
                else
                {
                    rootEntry.SubEntries.Add(current);
                }

                // is it antenna's selected entry?
                if (Antenna.Target == cb.Guid())
                {
                    selectedEntry = current;
                }
            }

            // Sort the lists based on semi-major axis. In reverse because of how we render it.
            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                var cb = FlightGlobals.Bodies[i];
                celestialBodyEntryDict[cb].SubEntries.Sort((b, a) =>
                {
                    return FlightGlobals.Bodies.Find(x => x.Guid() == a.Guid).orbit.semiMajorAxis.CompareTo(
                        FlightGlobals.Bodies.Find(x => x.Guid() == b.Guid).orbit.semiMajorAxis);
                });
            }
            rootEntry.SubEntries.Reverse();
        }

        private void AddMissionControls()
        {
            if (RTSettings.Instance != null)
            {
                for (int i = 0; i < RTSettings.Instance.GroundStations.Count; i++)
                {
                    var current = new Entry()
                    {
                        Text = RTSettings.Instance.GroundStations[i].GetName(),
                        Guid = RTSettings.Instance.GroundStations[i].mGuid,
                        Color = Color.white,
                    };
                    celestialBodyEntryDict[RTSettings.Instance.GroundStations[i].GetBody()].SubEntries.Add(current);

                    // is it antenna's selected entry?
                    if (Antenna.Target == RTSettings.Instance.GroundStations[i].mGuid)
                    {
                        selectedEntry = current;
                    }
                }
            }
        }

        private void ComputeEntryDepths()
        {
            Stack<Entry> dfs = new Stack<Entry>();
            for (int i = 0; i < rootEntry.SubEntries.Count; i++)
            {
                var child = rootEntry.SubEntries[i];
                child.Depth = 0;
                dfs.Push(child);
            }

            while (dfs.Count > 0)
            {
                var current = dfs.Pop();
                for (int j = 0; j < current.SubEntries.Count; j++)
                {
                    var child2 = current.SubEntries[j];
                    child2.Depth = current.Depth + 1;
                    dfs.Push(child2);
                }
            }
        }
    }
}
