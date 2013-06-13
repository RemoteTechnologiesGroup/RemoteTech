using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    [Flags]
    public enum GraphMode {
        None = 0x00,
        MapView = 0x01,
        TrackingStation = 0x02,
    }

    [Flags]
    public enum EdgeType {
        None = 0x00,
        Omni = 0x01,
        Dish = 0x02,
        Connection = 0x04,
    }

    public class RTNetworkRenderer : MonoBehaviour{
        VectorLine[] mLines;
        RTCore mCore;
        HashSet<TypedEdge<ISatellite>> mEdges;
        HashSet<TypedEdge<ISatellite>> mConnectionEdges;
        bool mEnabled;

        public static RTNetworkRenderer Attach(RTCore core) {
            RTNetworkRenderer renderer = MapView.MapCamera.gameObject.GetComponent<RTNetworkRenderer>();
            if (renderer) {
                Destroy(renderer);
            }
            renderer = MapView.MapCamera.gameObject.AddComponent<RTNetworkRenderer>();
            renderer.mCore = core;
            renderer.mLines = new VectorLine[] {};
            renderer.mEdges = new HashSet<TypedEdge<ISatellite>>();
            renderer.mConnectionEdges = new HashSet<TypedEdge<ISatellite>>();
            renderer.mCore.Network.EdgeUpdated += renderer.OnEdgeUpdate;
            renderer.mCore.Network.ConnectionUpdated += renderer.OnConnectionUpdate;
            renderer.mCore.Satellites.Unregistered += renderer.OnSatelliteUnregister;
            return renderer;
        }

        public void OnPreRender() {
            UpdateLineCache();
            if (mLines != null && mEnabled) {
                foreach(VectorLine vl in mLines) {
                    if(MapView.Draw3DLines) {
                        Vector.DrawLine3D(vl);
                    } else {
                        Vector.DrawLine(vl);
                    }
                }

            }
        }

        public void Show() {
            mEnabled = true;
        }

        public void Hide() {
            for (int i = 0; i < mLines.Length; i++) {
                Vector.DestroyLine(ref mLines[i]);
            }
            mLines = new VectorLine[] { };
            mEnabled = false;
        }

        public void UpdateLineCache() {
            if(mCore.Network != null) {
                int oldLength = mLines.Length;
                int newLength = mEdges.Count;
                for (int i = newLength; i < oldLength; i++) {
                    Vector.DestroyLine(ref mLines[i]);
                }
                Array.Resize(ref mLines, newLength);
                var it = mEdges.GetEnumerator();
                for (int i = 0; i < newLength; i++) {
                    it.MoveNext();
                    Vector3[] newPoints = new Vector3[] {
                        ScaledSpace.LocalToScaledSpace(it.Current.A.Position),
                        ScaledSpace.LocalToScaledSpace(it.Current.B.Position)
                    };
                    if(mLines[i] == null) {
                        mLines[i] = new VectorLine("Path", newPoints,
                            GetColorFor(it.Current),
                            MapView.fetch.orbitLinesMaterial,
                            5.0f,
                            LineType.Discrete);
                        mLines[i].layer = 31;
                        mLines[i].mesh.MarkDynamic();
                    } else {
                        mLines[i].Resize(newPoints);
                        Vector.SetColor(mLines[i], GetColorFor(it.Current));
                    }
                    Vector.Active(mLines[i], (mCore.Settings.GRAPH_EDGE & it.Current.Type) == it.Current.Type);
                }
                mEnabled = true;
            } else {
                mEnabled = false;
            }
        }

        Color GetColorFor(TypedEdge<ISatellite> edge) {
            if (mConnectionEdges.Contains(edge)) {
                return XKCDColors.ElectricLime;
            } else {
                if (edge.Type == EdgeType.Omni) 
                    return XKCDColors.BrownGrey;
                if (edge.Type == EdgeType.Dish)
                    return XKCDColors.Amber;
            }
            return XKCDColors.Grey;
        }

        void OnSatelliteUnregister(ISatellite s) {
            mEdges.RemoveWhere(x => x.A == s || x.B == s);
        }

        void OnEdgeUpdate(TypedEdge<ISatellite> edge) {
            if (edge.Type == EdgeType.None) {
                mEdges.Remove(edge);
            } else {
                mEdges.Add(edge);
            }
        }

        void OnConnectionUpdate(Path<ISatellite> conn) {
            mConnectionEdges.Clear();
            for(int i = 1; i < conn.Nodes.Count; i++) {
                mConnectionEdges.Add(new TypedEdge<ISatellite>(conn.Nodes[i - 1], conn.Nodes[i], EdgeType.Connection));
            }
        }

        public void OnDestroy() {
            mCore.Network.EdgeUpdated -= OnEdgeUpdate;
            mCore.Satellites.Unregistered -= OnSatelliteUnregister;
            mCore.Network.ConnectionUpdated -= OnConnectionUpdate;
        }
    }
}
