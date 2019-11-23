﻿using System;
using System.Linq;
using System.Collections.Generic;
using RemoteTech.SimpleTypes;
using RemoteTech.UI;
using UnityEngine;

using Debug = System.Diagnostics.Debug;
using KSP.Localization;

namespace RemoteTech
{
    [Flags]
    public enum MapFilter
    {
        None   = 0,
        Omni   = 1,
        Dish   = 2,
        Sphere = 4,
        Cone   = 8,
        Planet = 8,     // For backward compatibility with RemoteTech 1.4 and earlier
                        // Cone should be first, so that it's the one that appears in settings file
        Path   = 16,
        MultiPath = 32
    }

    /// <summary>
    /// RemoteTech UI network render in charre of drawing connection links in tracking station or flight map scenes.
    /// </summary>
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

        private static readonly Texture2D mTexMark;
        private readonly HashSet<BidirectionalEdge<ISatellite>> mEdges = new HashSet<BidirectionalEdge<ISatellite>>();
        private readonly List<NetworkLine> mLines = new List<NetworkLine>();
        private readonly List<NetworkCone> mCones = new List<NetworkCone>();
        private static float mLineWidth = 1f;

        public bool ShowOmni  { get { return (Filter & MapFilter.Omni)   == MapFilter.Omni; } }
        public bool ShowDish  { get { return (Filter & MapFilter.Dish)   == MapFilter.Dish; } }
        public bool ShowPath  { get { return (Filter & MapFilter.Path)   == MapFilter.Path; } }
        public bool ShowMultiPath { get { return (Filter & MapFilter.MultiPath) == MapFilter.MultiPath; } }
        public bool ShowRange { get { return (Filter & MapFilter.Sphere) == MapFilter.Sphere; } }
        public bool ShowCone  { get { return (Filter & MapFilter.Cone)   == MapFilter.Cone; } }

        public GUIStyle smallStationText;
        public GUIStyle smallStationHead;

        static NetworkRenderer()
        {
            RTUtil.LoadImage(out mTexMark, "mark");

            if(Versioning.version_major == 1)
            {
                switch(Versioning.version_minor)
                {
                    case 4:
                        mLineWidth = 1f; //1f is matching to CommNet's line width
                        break;
                    default:
                        mLineWidth = 3f;
                        break;
                }
            }
        }

        public static NetworkRenderer CreateAndAttach()
        {
            var renderer = MapView.MapCamera.gameObject.GetComponent<NetworkRenderer>();
            if (renderer)
            {
                Destroy(renderer);
            }

            renderer = MapView.MapCamera.gameObject.AddComponent<NetworkRenderer>();
            RTCore.Instance.Network.OnLinkAdd += renderer.OnLinkAdd;
            RTCore.Instance.Network.OnLinkRemove += renderer.OnLinkRemove;
            RTCore.Instance.Satellites.OnUnregister += renderer.OnSatelliteUnregister;

            renderer.smallStationHead = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12
            };

            renderer.smallStationText = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 10,
                normal = { textColor = Color.white }
            };

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
                    bool showOnMapview = true;
                    var worldPos = ScaledSpace.LocalToScaledSpace(s.Position);
                    if (MapView.MapCamera.transform.InverseTransformPoint(worldPos).z < 0f) continue;
                    Vector3 pos = PlanetariumCamera.Camera.WorldToScreenPoint(worldPos);
                    var screenRect = new Rect((pos.x - 8), (Screen.height - pos.y) - 8, 16, 16);
                    
                    // Hide the current ISatellite if it is behind its body
                    if (RTSettings.Instance.HideGroundStationsBehindBody && IsOccluded(s.Position, s.Body))
                        showOnMapview = false;

                    if (RTSettings.Instance.HideGroundStationsOnDistance && !IsOccluded(s.Position, s.Body) && this.IsCamDistanceToWide(s.Position))
                        showOnMapview = false;

                    // orbiting remote stations are always shown
                    if(s.isVessel && !s.parentVessel.Landed)
                        showOnMapview = true;

                    if (showOnMapview)
                    {
                        Color pushColor = GUI.color;
                        // tint the white mark.png into the defined color
                        GUI.color = s.MarkColor;
                        // draw the mark.png
                        GUI.DrawTexture(screenRect, mTexMark, ScaleMode.ScaleToFit, true);
                        GUI.color = pushColor;

                        // Show Mouse over informations to the ground station
                        if (RTSettings.Instance.ShowMouseOverInfoGroundStations && s is MissionControlSatellite && screenRect.ContainsMouse())
                        {
                            Rect headline = screenRect;
                            Vector2 nameDim = this.smallStationHead.CalcSize(new GUIContent(s.Name));

                            headline.x -= nameDim.x + 10;
                            headline.y -= 3;
                            headline.width = nameDim.x;
                            headline.height = 14;
                            // draw headline of the station
                            GUI.Label(headline, s.Name, this.smallStationHead);

                            // loop antennas
                            String antennaRanges = String.Empty;
                            foreach (var antenna in s.Antennas)
                            {
                                if(antenna.Omni > 0)
                                {
                                    antennaRanges += Localizer.Format("#RT_NetworkFB_Omni") + RTUtil.FormatSI(antenna.Omni,"m") + Environment.NewLine;//"Omni: "
                                }
                                if (antenna.Dish > 0)
                                {
                                    antennaRanges +=  Localizer.Format("#RT_NetworkFB_Dish") + RTUtil.FormatSI(antenna.Dish, "m") + Environment.NewLine;//"Dish: "
                                }
                            }

                            if(!antennaRanges.Equals(String.Empty))
                            {
                                Rect antennas = screenRect;
                                GUIContent content = new GUIContent(antennaRanges);

                                Vector2 antennaDim = this.smallStationText.CalcSize(content);
                                float maxHeight = this.smallStationText.CalcHeight(content, antennaDim.x);

                                antennas.y += headline.height - 3;
                                antennas.x -= antennaDim.x + 10;
                                antennas.width = antennaDim.x;
                                antennas.height = maxHeight;

                                // draw antenna infos of the station
                                GUI.Label(antennas, antennaRanges, this.smallStationText);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the location is behind the body
        /// Original code by regex from https://github.com/NathanKell/RealSolarSystem/blob/master/Source/KSCSwitcher.cs
        /// </summary>
        private bool IsOccluded(Vector3d loc, CelestialBody body)
        {
            Vector3d camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);

            if (Vector3d.Angle(camPos - loc, body.position - loc) > 90) { return false; }
            return true;
        }

        /// <summary>
        /// Calculates the distance between the camera position and the ground station, and
        /// returns true if the distance is >= DistanceToHideGroundStations from the settings file.
        /// </summary>
        /// <param name="loc">Position of the ground station</param>
        /// <returns>True if the distance is to wide, otherwise false</returns>
        private bool IsCamDistanceToWide(Vector3d loc)
        {
            Vector3d camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);
            float distance = Vector3.Distance(camPos, loc);
            
            // distance to wide?
            if(distance >= RTSettings.Instance.DistanceToHideGroundStations)
                return true;

            return false;
        }

        private void UpdateNetworkCones()
        {
            List<IAntenna> antennas = (ShowCone ? RTCore.Instance.Antennas.Where(
                                        ant => ant.Powered && ant.CanTarget && RTCore.Instance.Satellites[ant.Guid] != null 
                                        && ant.Target != Guid.Empty)
                                     : Enumerable.Empty<IAntenna>()).ToList();
            int oldLength = mCones.Count;
            int newLength = antennas.Count;

            // Free any unused lines
            for (int i = newLength; i < oldLength; i++)
            {
                GameObject.Destroy(mCones[i]);
                mCones[i] = null;
            }
            mCones.RemoveRange(Math.Min(oldLength, newLength), Math.Max(oldLength - newLength, 0));
            mCones.AddRange(Enumerable.Repeat((NetworkCone) null, Math.Max(newLength - oldLength, 0)));

            for (int i = 0; i < newLength; i++)
            {
                var center = RTCore.Instance.Network.GetPositionFromGuid(antennas[i].Target);
                Debug.Assert(center != null,
                             "center != null",
                             String.Format("GetPositionFromGuid returned a null value for the target {0}",
                                           antennas[i].Target)
                             );

                if (!center.HasValue) continue;

                mCones[i] = mCones[i] ?? NetworkCone.Instantiate();
                mCones[i].LineWidth = mLineWidth;
                mCones[i].Antenna = antennas[i];
                mCones[i].Color = Color.gray;
                mCones[i].Active = ShowCone;
                mCones[i].Center = center.Value;
            }
        }

        private void UpdateNetworkEdges()
        {
            var edges = mEdges.Where(CheckVisibility).ToList();
            int oldLength = mLines.Count;
            int newLength = edges.Count;

            // Free any unused lines
            for (int i = newLength; i < oldLength; i++)
            {
                Destroy(mLines[i]);
                mLines[i] = null;
            }
            mLines.RemoveRange(Math.Min(oldLength, newLength), Math.Max(oldLength - newLength, 0));
            mLines.AddRange(Enumerable.Repeat<NetworkLine>(null, Math.Max(newLength - oldLength, 0)));

            // Iterate over all satellites, updating or creating new lines.
            var it = edges.GetEnumerator();
            for (int i = 0; i < newLength; i++)
            {
                it.MoveNext();
                mLines[i] = mLines[i] ?? NetworkLine.Instantiate();
                mLines[i].LineWidth = mLineWidth;
                mLines[i].Edge = it.Current;
                mLines[i].Color = CheckColor(it.Current);
                mLines[i].Active = true;
            }
        }

        private bool CheckVisibility(BidirectionalEdge<ISatellite> edge)
        {
            var vessel = PlanetariumCamera.fetch.target.vessel;
            var satellite = RTCore.Instance.Satellites[vessel];
            if (satellite != null && ShowPath)
            {
                var connections = RTCore.Instance.Network[satellite];
                if (connections.Any() && connections[0].Contains(edge))
                    return true;
            }
            if (ShowMultiPath && edge.A.Visible && edge.B.Visible) // purpose of edge-visibility condition is to prevent unnecessary performance off-screen
            {
                var satellites = RTCore.Instance.Network.ToArray();
                for (int i = 0; i < satellites.Length; i++)
                {
                    var connections = RTCore.Instance.Network[satellites[i]]; // get the working-connection path of every satellite
                    if (connections.Any() && connections[0].Contains(edge))
                        return true;
                }
            }
            if (edge.Type == LinkType.Omni && !ShowOmni)
                return false;
            if (edge.Type == LinkType.Dish && !ShowDish)
                return false;
            if (!edge.A.Visible || !edge.B.Visible)
                return false;
            return true;
        }

        private Color CheckColor(BidirectionalEdge<ISatellite> edge)
        {
            var vessel = PlanetariumCamera.fetch.target.vessel;
            var satellite = RTCore.Instance.Satellites[vessel];
            if (satellite != null && ShowPath)
            {
                var connections = RTCore.Instance.Network[satellite];
                if (connections.Any() && connections[0].Contains(edge))
                    return RTSettings.Instance.ActiveConnectionColor;
            }
            if (ShowMultiPath && edge.A.Visible && edge.B.Visible) // purpose of edge-visibility condition is to prevent unnecessary performance off-screen
            {
                var satellites = RTCore.Instance.Network.ToArray();
                for (int i = 0; i < satellites.Length; i++)
                {
                    var connections = RTCore.Instance.Network[satellites[i]]; // get the working-connection path of every satellite
                    if (connections.Any() && connections[0].Contains(edge))
                        return RTSettings.Instance.ActiveConnectionColor;
                }
            }

            if (RTSettings.Instance.SignalRelayEnabled)
            {
                var satA = RTCore.Instance.Satellites[edge.A.Guid];
                var satB = RTCore.Instance.Satellites[edge.B.Guid];
                if ((satA != null && !satA.CanRelaySignal) || (satB != null && !satB.CanRelaySignal))
                    return RTSettings.Instance.DirectConnectionColor;
            }

            if (edge.Type == LinkType.Omni)
                return RTSettings.Instance.OmniConnectionColor;
            if (edge.Type == LinkType.Dish)
                return RTSettings.Instance.DishConnectionColor;

            return XKCDColors.Grey;
        }

        private void OnSatelliteUnregister(ISatellite s)
        {
            mEdges.RemoveWhere(e => e.A == s || e.B == s);
        }

        private void OnLinkAdd(ISatellite a, NetworkLink<ISatellite> link)
        {
            mEdges.Add(new BidirectionalEdge<ISatellite>(a, link.Target, link.Port));
        }

        private void OnLinkRemove(ISatellite a, NetworkLink<ISatellite> link)
        {
            mEdges.Remove(new BidirectionalEdge<ISatellite>(a, link.Target, link.Port));
        }

        public void Detach()
        {
            for (int i = 0; i < mLines.Count; i++)
            {
                GameObject.DestroyImmediate(mLines[i]);
            }
            mLines.Clear();
            for (int i = 0; i < mCones.Count; i++)
            {
                GameObject.DestroyImmediate(mCones[i]);
            }
            mCones.Clear();
            DestroyImmediate(this);
        }

        public void OnDestroy()
        {
            RTCore.Instance.Network.OnLinkAdd -= OnLinkAdd;
            RTCore.Instance.Network.OnLinkRemove -= OnLinkRemove;
            RTCore.Instance.Satellites.OnUnregister -= OnSatelliteUnregister;
        }
    }
}