using System;
using UnityEngine;

namespace RemoteTech {

    public class PathRenderer {

        VectorLine mLineCache;
        RTSatelliteNetwork mNetwork;
        Vector3[] mLinePoints;
        bool mEnabled;

        public PathRenderer(RTSatelliteNetwork satelliteNetwork) {
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
            if(mNetwork.Path.Count > 0) {
                mLinePoints = new Vector3[mNetwork.Path.Count];
                for (int i = 0; i < mNetwork.Path.Count; i++) {
                    mLinePoints[i] = ScaledSpace.LocalToScaledSpace(mNetwork.Path[i].Position);
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
