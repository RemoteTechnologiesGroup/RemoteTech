using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class SatelliteFragment : IFragment, IDisposable
    {
        public ISatellite Satellite
        {
            get { return satellite; }
            set { if (satellite != value) { satellite = value; Antenna = null; } }
        }

        public IAntenna Antenna { get; private set; }

        private ISatellite satellite;
        private Vector2 scrollPosition;
        private List<AntennaEntry> Entries = new List<AntennaEntry>();

        private class AntennaEntry
        {
            public readonly String Name;
            public readonly String DescLeft;
            public readonly String DescRight;
            public readonly IAntenna Antenna;
            public readonly Color Color;

            public AntennaEntry(String name, String left, String right, IAntenna antenna, Color color)
            {
                this.Name = name;
                this.DescLeft = left;
                this.DescRight = right;
                this.Antenna = antenna;
                this.Color = color;
            }
        }

        public SatelliteFragment(ISatellite sat)
        {
            Satellite = sat;
            RTCore.Instance.Satellites.OnUnregister += Refresh;
        }

        public void Dispose()
        {
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.OnUnregister -= Refresh;
            }
        }

        public void Update()
        {
            const int TruncateWidth = 25;
            Entries.Clear();
            foreach (var antenna in Satellite.Antennas.Where(a => a.CanTarget))
            {
                var left = String.Empty;
                var right = String.Empty;
                if (antenna.Targets.Count == 0)
                {
                    left = "No Target";
                }
                else if (antenna.Targets[0].IsMultiple) {
                    left = antenna.Targets[0].ToString().Truncate(TruncateWidth);
                }
                else
                {
                    var origin = Satellite.Position;
                    var target = antenna.Targets[0].FirstOrDefault();
                    if (target == null)
                    {
                        left = "Unknown Target";
                    }
                    else
                    {
                        var goal = target.Position;
                        var distance = (goal - origin).magnitude;
                        var formattedDistance = "(" + RTUtil.FormatSI(distance, "m") + ")";
                        left = target.Name.Truncate(TruncateWidth - formattedDistance.Length - 1);
                        right = formattedDistance;
                    }
                }
                Entries.Add(new AntennaEntry(antenna.Name, left, right, antenna, antenna.Powered ? XKCDColors.ElectricLime : XKCDColors.Scarlet));
            }
        }
        public void Draw()
        {
            if (Satellite == null) return;
            if (Event.current.type == EventType.Layout) Update();

            RTGui.HorizontalBlock(() =>
            {
                GUILayout.TextField(Satellite.Name.Truncate(25), GUILayout.ExpandWidth(true));
                RTGui.Button("Name", () =>
                {
                    var vessel = FlightGlobals.Vessels.First(v => v.id == Satellite.Guid);
                    if (vessel) vessel.RenameVessel();
                }, GUILayout.ExpandWidth(false), GUILayout.Height(24));
            });

            RTGui.ScrollViewBlock(ref scrollPosition, () =>
            {
                Color pushColor = GUI.contentColor;
                TextAnchor pushAlign = GUI.skin.button.alignment;
                foreach (var entry in Entries)
                {
                    RTGui.VerticalBlock(() =>
                    {
                        GUILayout.Label(entry.Name);
                        RTGui.HorizontalBlock(() =>
                        {
                            GUILayout.Label(entry.DescLeft);
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(entry.DescRight);
                        });
                    });
                    RTGui.ClickableOverlay(() =>
                    {
                        Antenna = (Antenna != entry.Antenna) ? entry.Antenna : null;
                    }, GUILayoutUtility.GetLastRect());
                }
                GUI.skin.button.alignment = pushAlign;
                GUI.contentColor = pushColor;
            }, GUILayout.ExpandHeight(true));
        }

        private void Refresh(ISatellite sat)
        {
            if (sat == Satellite) Satellite = null;
        }
    }
}
