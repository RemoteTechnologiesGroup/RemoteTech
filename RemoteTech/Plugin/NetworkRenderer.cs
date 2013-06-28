using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class NetworkRenderer : MonoBehaviour, IConfigNode {
        public EdgeType MapEdges = EdgeType.Omni | EdgeType.Dish | EdgeType.Connection;
        public GraphMode MapModes = GraphMode.MapView | GraphMode.TrackingStation;
        public bool MapCommand = true;

        private HashSet<TypedEdge<ISatellite>> mConnectionEdges;
        private HashSet<TypedEdge<ISatellite>> mEdges;
        private HashSet<ISatellite> mCommandStations; 
        private List<VectorLine> mLines;

        private RTCore mCore;

        public static NetworkRenderer AttachToMapView(RTCore core) {
            var renderer = MapView.MapCamera.gameObject.GetComponent<NetworkRenderer>();
            if (renderer) Destroy(renderer);
            renderer = MapView.MapCamera.gameObject.AddComponent<NetworkRenderer>();
            renderer.mCore = core;
            renderer.mLines = new List<VectorLine>();
            renderer.mEdges = new HashSet<TypedEdge<ISatellite>>();
            renderer.mConnectionEdges = new HashSet<TypedEdge<ISatellite>>();
            renderer.mCommandStations = new HashSet<ISatellite>();

            core.Network.EdgeUpdated += renderer.OnEdgeUpdate;
            core.Network.ConnectionUpdated += renderer.OnConnectionUpdate;
            core.Satellites.Unregistered += renderer.OnSatelliteUnregister;
            return renderer;
        }

        public void Load(ConfigNode node) {
            node.AddValue("MapEdges", (int) MapEdges);
            node.AddValue("MapModes", (int) MapModes);
            node.AddValue("MapCommand", MapCommand);
        }

        public void Save(ConfigNode node) {
            MapEdges = (EdgeType) Enum.Parse(typeof(EdgeType), node.GetValue("MapEdges"));
            MapModes = (GraphMode)Enum.Parse(typeof(GraphMode), node.GetValue("MapModes"));
            MapCommand = Boolean.Parse(node.GetValue("MapCommand"));
        }

        public void OnPreRender() {
            if (!MapView.MapIsEnabled) return;
            UpdateLineCache();
            foreach (VectorLine vl in mLines) {
                if (MapView.Draw3DLines) Vector.DrawLine3D(vl);
                else Vector.DrawLine(vl);
            }
        }

        public void OnGUI() {
            if (Event.current.type == EventType.Repaint && MapView.MapIsEnabled) {
                foreach (ISatellite s in mCommandStations) {
                    Vector3 pos = MapView.MapCamera.camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(s.Position));
                    Rect screenRect = new Rect((pos.x - 8), (Screen.height - pos.y) - 8, 16, 16);

                    Graphics.DrawTexture(screenRect, RTCore.Instance.Settings.IconMark, 0, 0, 0, 0);
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
                Vector.Active(mLines[i],
                            ((MapEdges & it.Current.Type) == it.Current.Type) &&
                            (it.Current.A.Visible && it.Current.B.Visible));
            }
        }

        private void AssignVectorLine(int i, Vector3[] newPoints, TypedEdge<ISatellite> edge ) {
            if (mLines.Count  <= i) {
                mLines.Add(new VectorLine("Path", newPoints, GetColorFor(edge),
                                          MapView.fetch.orbitLinesMaterial, 5.0f,
                                          LineType.Discrete));
                mLines[mLines.Count - 1].layer = 31;
                mLines[mLines.Count - 1].mesh.MarkDynamic();
            } else {
                mLines[i].Resize(newPoints);
                Vector.SetColor(mLines[i], GetColorFor(edge));
            }
        }

        private Color GetColorFor(TypedEdge<ISatellite> edge) {
            if (mConnectionEdges.Contains(edge)) return XKCDColors.ElectricLime;
            if (edge.Type == EdgeType.Omni) return XKCDColors.BrownGrey;
            if (edge.Type == EdgeType.Dish) return XKCDColors.Amber;

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
            if (edge.A.CommandStation) {
                mCommandStations.Add(edge.A);
            } else {
                mCommandStations.Remove(edge.A);
            }
            /*if (edge.B.CommandStation) {
                mCommandStations.Add(edge.B);
            } else {
                mCommandStations.Remove(edge.B);
            }*/
        }

        private void OnConnectionUpdate(Path<ISatellite> conn) {
            if (FlightGlobals.ActiveVessel && conn.Start.Guid == FlightGlobals.ActiveVessel.id) {
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
