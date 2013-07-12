using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    [Flags]
    public enum MapFilter {
        None = 0,
        Omni = 1,
        Dish = 2,
        OmniDish = MapFilter.Omni | MapFilter.Dish,
        Any = 4,
        OnlyPath = 8
    }

    public class NetworkRenderer : MonoBehaviour, IConfigNode {
        public MapFilter Filter { get; set; }

        private static Texture2D mTexMark;
        private HashSet<TypedEdge<ISatellite>> mConnectionEdges;
        private HashSet<TypedEdge<ISatellite>> mEdges;
        private List<VectorLine> mLines;
        private RTCore mCore;

        private bool ShowOmni { get {
            return (Filter & (MapFilter.Any | MapFilter.Omni)) == (MapFilter.Any | MapFilter.Omni);
        } }

        private bool ShowDish { get {
            return (Filter & (MapFilter.Any | MapFilter.Dish)) == (MapFilter.Any | MapFilter.Dish);
        } }

        private bool ShowPath { get { return (Filter & MapFilter.OnlyPath) == MapFilter.OnlyPath; } }

        private bool ShowAll { get { return (Filter & MapFilter.Any) == MapFilter.Any; } }

        static NetworkRenderer() {
            RTUtil.LoadImage(out mTexMark, "mark.png");    
        }

        public static NetworkRenderer AttachToMapView(RTCore core) {
            var renderer = MapView.MapCamera.gameObject.GetComponent<NetworkRenderer>();
            if (renderer) Destroy(renderer);
            renderer = MapView.MapCamera.gameObject.AddComponent<NetworkRenderer>();
            renderer.mCore = core;
            renderer.mLines = new List<VectorLine>();
            renderer.mEdges = new HashSet<TypedEdge<ISatellite>>();
            renderer.mConnectionEdges = new HashSet<TypedEdge<ISatellite>>();
            renderer.Filter = MapFilter.Any;

            core.Network.EdgeUpdated += renderer.OnEdgeUpdate;
            core.Network.ConnectionUpdated += renderer.OnConnectionUpdate;
            core.Satellites.Unregistered += renderer.OnSatelliteUnregister;
            return renderer;
        }

        public void Load(ConfigNode node) {
            try {
                if (node.HasValue("MapFilter")) throw new ArgumentException("MapFilter non-exist");
                Filter = (MapFilter)Enum.Parse(typeof(MapFilter), node.GetValue("MapFilter"));
            } catch (ArgumentException) {
                Filter = MapFilter.OnlyPath | MapFilter.Dish;
            }
        }

        public void Save(ConfigNode node) {
            node.AddValue("MapFilter", Filter.ToString());
        }

        public void OnPreRender() {
            if (MapView.MapIsEnabled) {
                UpdateLineCache();
                foreach (VectorLine vl in mLines) {
                    if (MapView.Draw3DLines)
                        Vector.DrawLine3D(vl);
                    else
                        Vector.DrawLine(vl);
                }
            }
        }

        public void OnGUI() {
            if (Event.current.type == EventType.Repaint && MapView.MapIsEnabled) {
                foreach (ISatellite s in mCore.Satellites.FindCommandStations()
                        .Concat(new [] { mCore.Network.MissionControl })) {
                    Vector3 pos = MapView.MapCamera.camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(s.Position));
                    Rect screenRect = new Rect((pos.x - 8), (Screen.height - pos.y) - 8, 16, 16);

                    Graphics.DrawTexture(screenRect, mTexMark, 0, 0, 0, 0);
                }
            }
        }

        private void UpdateLineCache() {
            int oldLength = mLines.Count;
            int newLength = mEdges.Count;
            for (int i = newLength; i < oldLength; i++) {
                VectorLine line = mLines[i];
                Vector.DestroyLine(ref line);
            }
            if (newLength < oldLength) {
                mLines.RemoveRange(newLength, oldLength - newLength);
            }
            HashSet<TypedEdge<ISatellite>>.Enumerator it = mEdges.GetEnumerator();
            for (int i = 0; i < newLength; i++) {
                it.MoveNext();
                var newPoints = new Vector3[] {
                    ScaledSpace.LocalToScaledSpace(it.Current.A.Position),
                    ScaledSpace.LocalToScaledSpace(it.Current.B.Position)
                };
                AssignVectorLine(i, newPoints, it.Current);
                Vector.Active(mLines[i], CheckVisibility(it.Current));
            }
        }

        private void AssignVectorLine(int i, Vector3[] newPoints, TypedEdge<ISatellite> edge) {
            if (mLines.Count <= i) {
                mLines.Add(new VectorLine("Path", newPoints, CheckColor(edge),
                                          MapView.fetch.orbitLinesMaterial, 5.0f,
                                          LineType.Discrete));
                mLines[mLines.Count - 1].layer = 31;
                mLines[mLines.Count - 1].mesh.MarkDynamic();
            } else {
                mLines[i].Resize(newPoints);
                Vector.SetColor(mLines[i], CheckColor(edge));
            }
        }

        private bool CheckVisibility(TypedEdge<ISatellite> edge) {
            if (mConnectionEdges.Contains(edge) && (ShowPath || ShowAll))
                return true;
            if (edge.Type == EdgeType.Omni && !ShowOmni)
                return false;
            if (edge.Type == EdgeType.Dish && !ShowDish)
                return false;
            if (!edge.A.Visible || !edge.B.Visible)
                return false;
            return true;
        }

        private Color CheckColor(TypedEdge<ISatellite> edge) {
            if (mConnectionEdges.Contains(edge))
                return XKCDColors.ElectricLime;
            if (edge.Type == EdgeType.Omni)
                return XKCDColors.BrownGrey;
            if (edge.Type == EdgeType.Dish)
                return XKCDColors.Amber;

            return XKCDColors.Grey;
        }

        private void OnSatelliteUnregister(ISatellite s) {
            mEdges.RemoveWhere(x => x.A == s || x.B == s);
        }

        private void OnEdgeUpdate(TypedEdge<ISatellite> edge) {
            if (edge.Type == EdgeType.None) {
                mEdges.Remove(edge);
            } else {
                mEdges.Add(edge);
            }
        }

        private void OnConnectionUpdate(Path<ISatellite> conn) {
            if ((FlightGlobals.ActiveVessel && conn.Start.Guid == FlightGlobals.ActiveVessel.id) ||
                (RTCore.Instance.IsTrackingStation && 
                    PlanetariumCamera.fetch.target.vessel != null && 
                    PlanetariumCamera.fetch.target.vessel.id == conn.Start.Guid)) {
                mConnectionEdges.Clear();
                for (int i = 1; i < conn.Nodes.Count; i++) {
                    mConnectionEdges.Add(new TypedEdge<ISatellite>(conn.Nodes[i - 1], conn.Nodes[i],
                                                                   EdgeType.Connection));
                }
            }
        }

        public void Detach() {
            for (int i = 0; i < mLines.Count; i++) {
                VectorLine line = mLines[i];
                Vector.DestroyLine(ref line);
            }
            Destroy(this);
        }

        public void OnDestroy() {
            mCore.Network.EdgeUpdated -= OnEdgeUpdate;
            mCore.Satellites.Unregistered -= OnSatelliteUnregister;
            mCore.Network.ConnectionUpdated -= OnConnectionUpdate;
        }
    }
}
