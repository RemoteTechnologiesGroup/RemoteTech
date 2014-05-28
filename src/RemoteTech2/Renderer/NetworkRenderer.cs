using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    [Flags]
    public enum MapFilter
    {
        None = 0,
        Omni = 1,
        Dish = 2,
        Planet = 4,
        Path = 8
    }

    public class NetworkRenderer : MonoBehaviour
    {
        public MapFilter Filter { 
            get 
            {
                return RTSettings.Instance.MapFilter;
            } 
            set 
            {
                RTSettings.Instance.MapFilter = value;
                RTSettings.Instance.Save(); 
            } 
        }

        private HashSet<NetworkLink<ISatellite>> edgeMap = new HashSet<NetworkLink<ISatellite>>();
        private List<NetworkLine> lines = new List<NetworkLine>();
        private List<NetworkCone> cones = new List<NetworkCone>();

        public bool ShowOmni { get { return (Filter & MapFilter.Omni) == MapFilter.Omni; } }
        public bool ShowDish { get { return (Filter & MapFilter.Dish) == MapFilter.Dish; } }
        public bool ShowPath { get { return (Filter & MapFilter.Path) == MapFilter.Path; } }
        public bool ShowPlanet { get { return (Filter & MapFilter.Planet) == MapFilter.Planet; } }

        public static NetworkRenderer CreateAndAttach()
        {
            var renderer = MapView.MapCamera.gameObject.GetComponent<NetworkRenderer>();
            if (renderer)
            {
                Destroy(renderer);
            }

            renderer = MapView.MapCamera.gameObject.AddComponent<NetworkRenderer>();
            RTCore.Instance.Network.LinkAdded += renderer.OnLinkAdd;
            RTCore.Instance.Network.LinkRemoved += renderer.OnLinkRemove;
            RTCore.Instance.Satellites.OnUnregister += renderer.OnSatelliteUnregister;
            return renderer;
        }

        public void OnPreCull()
        {
            if (MapView.MapIsEnabled)
            {
                UpdateNetworkEdges();
                UpdateNetworkCones();
            }
        }

        public void OnGUI()
        {
            if (Event.current.type == EventType.Repaint && MapView.MapIsEnabled)
            {
                foreach (ISatellite s in RTCore.Instance.Satellites.FindCommandStations().Concat(RTCore.Instance.Network.GroundStations.Values))
                {
                    var world_pos = ScaledSpace.LocalToScaledSpace(s.Position);
                    if (MapView.MapCamera.transform.InverseTransformPoint(world_pos).z < 0f) continue;
                    Vector3 pos = MapView.MapCamera.camera.WorldToScreenPoint(world_pos);
                    Rect screenRect = new Rect((pos.x - 8), (Screen.height - pos.y) - 8, 16, 16);
                    Graphics.DrawTexture(screenRect, Textures.Mark, 0, 0, 0, 0);
                }
            }
        }

        private void UpdateNetworkCones()
        {
            var targets = new List<KeyValuePair<IAntenna, ISatellite>>(100);
            if (ShowPlanet)
            {
                foreach (var antenna in RTCore.Instance.Antennas)
                {
                    if (!antenna.Powered || !antenna.CanTarget) continue;
                    foreach (var target in antenna.Targets)
                    {
                        if (target.IsMultiple) continue;
                        foreach (var satellite in target)
                        {
                            targets.Add(new KeyValuePair<IAntenna, ISatellite>(antenna, satellite));
                        }
                    }
                }
            }

            int oldLength = cones.Count;
            int newLength = targets.Count;

            // Free any unused lines
            for (int i = newLength; i < oldLength; i++)
            {
                GameObject.Destroy(cones[i]);
                cones[i] = null;
            }
            cones.RemoveRange(Math.Min(oldLength, newLength), Math.Max(oldLength - newLength, 0));
            cones.AddRange(Enumerable.Repeat((NetworkCone) null, Math.Max(newLength - oldLength, 0)));

            for (int i = 0; i < newLength; i++)
            {
                cones[i] = cones[i] ?? NetworkCone.Instantiate();
                cones[i].Material = MapView.fetch.orbitLinesMaterial;
                cones[i].LineWidth = 2.0f;
                cones[i].Antenna = targets[i].Key;
                cones[i].Target = targets[i].Value;
                cones[i].Color = Color.gray;
                cones[i].Active = ShowPlanet;
            }
        }

        private void UpdateNetworkEdges()
        {
            var edges = edgeMap.Where(e => CheckVisibility(e)).ToList();
            int oldLength = lines.Count;
            int newLength = edges.Count;

            // Free any unused lines
            for (int i = newLength; i < oldLength; i++)
            {
                GameObject.Destroy(lines[i]);
                lines[i] = null;
            }
            lines.RemoveRange(Math.Min(oldLength, newLength), Math.Max(oldLength - newLength, 0));
            lines.AddRange(Enumerable.Repeat<NetworkLine>(null, Math.Max(newLength - oldLength, 0)));

            // Iterate over all satellites, updating or creating new lines.
            var it = edges.GetEnumerator();
            for (int i = 0; i < newLength; i++)
            {
                it.MoveNext();
                lines[i] = lines[i] ?? NetworkLine.Instantiate();
                lines[i].Material = MapView.fetch.orbitLinesMaterial;
                lines[i].LineWidth = 3.0f;
                lines[i].Edge = it.Current;
                lines[i].Color = CheckColor(it.Current);
                lines[i].Active = true;
            }
        }

        private bool CheckVisibility(NetworkLink<ISatellite> edge)
        {
            if (edge.A.IsVisible && edge.B.IsVisible)
            {
                if (edge.LinkType == LinkType.Omni && ShowOmni) return true;
                if (edge.LinkType == LinkType.Dish && ShowDish) return true;
            }

            var satellite = RTCore.Instance.Satellites.SelectedSatellite;
            if (satellite != null && ShowPath)
            {
                var connections = RTCore.Instance.Network[satellite];
                if (connections.Any() && connections.ShortestDelay().Contains(edge))
                    return true;
            }

            return false;
        }

        private Color CheckColor(NetworkLink<ISatellite> edge)
        {
            var satellite = RTCore.Instance.Satellites.SelectedSatellite;
            if (satellite != null && ShowPath)
            {
                var connections = RTCore.Instance.Network[satellite];
                if (connections.Any() && connections.ShortestDelay().Contains(edge))
                    return RTSettings.Instance.ActiveConnectionColor;
            }

            if (edge.LinkType == LinkType.Omni)
                return RTSettings.Instance.OmniConnectionColor;
            if (edge.LinkType == LinkType.Dish)
                return RTSettings.Instance.DishConnectionColor;

            return XKCDColors.Grey;
        }

        private void OnSatelliteUnregister(ISatellite s)
        {
            edgeMap.RemoveWhere(e => e.A == s || e.B == s);
        }

        private void OnLinkAdd(NetworkLink<ISatellite> link)
        {
            edgeMap.Add(link);
        }

        private void OnLinkRemove(NetworkLink<ISatellite> link)
        {
            edgeMap.Remove(link);
        }

        public void Detach()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                GameObject.DestroyImmediate(lines[i]);
            }
            lines.Clear();
            for (int i = 0; i < cones.Count; i++)
            {
                GameObject.DestroyImmediate(cones[i]);
            }
            cones.Clear();
            DestroyImmediate(this);
        }

        public void OnDestroy()
        {
            RTCore.Instance.Network.LinkAdded -= OnLinkAdd;
            RTCore.Instance.Network.LinkRemoved -= OnLinkRemove;
            RTCore.Instance.Satellites.OnUnregister -= OnSatelliteUnregister;
        }
    }
}