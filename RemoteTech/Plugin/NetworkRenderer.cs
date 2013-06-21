using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class NetworkRenderer : MonoBehaviour {
        private HashSet<TypedEdge<ISatellite>> mConnectionEdges;
        private RTCore mCore;
        private HashSet<TypedEdge<ISatellite>> mEdges;
        private bool mEnabled;
        private VectorLine[] mLines;

        public static NetworkRenderer AttachToMapView(RTCore core, NetworkManager network) {
            var renderer = MapView.MapCamera.gameObject.GetComponent<NetworkRenderer>();
            if (renderer) Destroy(renderer);
            renderer = MapView.MapCamera.gameObject.AddComponent<NetworkRenderer>();
            renderer.mCore = core;
            renderer.mLines = new VectorLine[] {};
            renderer.mEdges = new HashSet<TypedEdge<ISatellite>>();
            renderer.mConnectionEdges = new HashSet<TypedEdge<ISatellite>>();
            network.EdgeUpdated += renderer.OnEdgeUpdate;
            network.ConnectionUpdated += renderer.OnConnectionUpdate;
            core.Satellites.Unregistered += renderer.OnSatelliteUnregister;
            return renderer;
        }

        public void OnPreRender() {
            UpdateLineCache();
            if (mLines == null || !mEnabled) return;
            foreach (VectorLine vl in mLines) {
                if (MapView.Draw3DLines) Vector.DrawLine3D(vl);
                else Vector.DrawLine(vl);
            }
        }

        public void Show() {
            mLines = new VectorLine[] {};
            mEnabled = true;
        }

        public void Hide() {
            if (mLines != null) {
                for (int i = 0; i < mLines.Length; i++) {
                    Vector.DestroyLine(ref mLines[i]);
                }
                mLines = null;
            }
            mEnabled = false;
        }

        private void UpdateLineCache() {
            if (mLines != null && mCore.Network != null) {
                int oldLength = mLines.Length;
                int newLength = mEdges.Count;
                for (int i = newLength; i < oldLength; i++) {
                    Vector.DestroyLine(ref mLines[i]);
                }
                Array.Resize(ref mLines, newLength);
                HashSet<TypedEdge<ISatellite>>.Enumerator it = mEdges.GetEnumerator();
                for (int i = 0; i < newLength; i++) {
                    it.MoveNext();
                    var newPoints = new Vector3[] {
                    ScaledSpace.LocalToScaledSpace(it.Current.A.Position),
                    ScaledSpace.LocalToScaledSpace(it.Current.B.Position)
                    };
                    if (mLines[i] == null) {
                        mLines[i] = new VectorLine("Path", newPoints, GetColorFor(it.Current),
                                                   MapView.fetch.orbitLinesMaterial, 5.0f,
                                                   LineType.Discrete);
                        mLines[i].layer = 31;
                        mLines[i].mesh.MarkDynamic();
                    } else {
                        mLines[i].Resize(newPoints);
                        Vector.SetColor(mLines[i], GetColorFor(it.Current));
                    }
                    Vector.Active(mLines[i],
                                  ((mCore.Settings.GRAPH_EDGE & it.Current.Type) == it.Current.Type) &&
                                  (it.Current.A.Visible && it.Current.B.Visible));
                }
                mEnabled = true;
            } else mEnabled = false;
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
            if (edge.Type == EdgeType.None) mEdges.Remove(edge);
            else mEdges.Add(edge);
        }

        private void OnConnectionUpdate(Path<ISatellite> conn) {
            mConnectionEdges.Clear();
            for (int i = 1; i < conn.Nodes.Count; i++) {
                mConnectionEdges.Add(new TypedEdge<ISatellite>(conn.Nodes[i - 1], conn.Nodes[i],
                                                               EdgeType.Connection));
            }
        }

        public void Detach() {
            Hide();
            Destroy(this);
        }

        public void OnDestroy() {
            mCore.Network.EdgeUpdated -= OnEdgeUpdate;
            mCore.Satellites.Unregistered -= OnSatelliteUnregister;
            mCore.Network.ConnectionUpdated -= OnConnectionUpdate;
        }
    }
}
