using System;
using UnityEngine;

namespace RemoteTech {

    public class PathRenderer {

        VectorLine mLineCache;
        RTConnectionManager mNetwork;
        Vector3[] mLinePoints;
        bool mEnabled;

        public PathRenderer(RTConnectionManager satelliteNetwork) {
            mNetwork = satelliteNetwork;
        }

        public void Show() {
            mEnabled = true;
        }

        public void Hide() {
            Vector.DestroyLine(ref mLineCache);
            mEnabled = false;
        }

        public void UpdateLineCache() {
            if(mNetwork.Connection.Exists) {
                mLinePoints = new Vector3[mNetwork.Connection.Nodes.Count];
                for (int i = 0; i < mNetwork.Connection.Nodes.Count; i++) {
                    mLinePoints[i] = ScaledSpace.LocalToScaledSpace(mNetwork.Connection.Nodes[i].Position);
                }
                if(mLineCache == null) {
                    mLineCache = new VectorLine("Path", mLinePoints,
                        XKCDColors.Amber,
                        MapView.fetch.orbitLinesMaterial,
                        5.0f,
                        LineType.Continuous);
                    mLineCache.layer = 31;
                } else {
                    mLineCache.Resize(mLinePoints);
                }
            } else {
                Vector.DestroyLine(ref mLineCache);
            }
        }

        public void Draw() {
            if (!mEnabled)
                return;
            UpdateLineCache();
            if(mLineCache != null) {
                Vector.DrawLine3D(mLineCache);
            }
        }
    }
}
