using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {

    public class PathRenderer {

        VectorLine mLineCache;
        SatelliteNetwork mNetwork;

        public PathRenderer(SatelliteNetwork satelliteNetwork) {
            mNetwork = satelliteNetwork;
        }

        public void Update() {
            if(mNetwork.Path.Count > 0) {
                Vector3[] points = new Vector3[mNetwork.Path.Count];
                for (int i = 0; i < mNetwork.Path.Count; i++) {
                    Vector3 scaled = ScaledSpace.LocalToScaledSpace(mNetwork.Path[i].Position);
                    points[i] = scaled;
                }
                if(mLineCache == null) {
                    mLineCache = new VectorLine("Path", points,
                        XKCDColors.Amber,
                        MapView.fetch.orbitLinesMaterial,
                        5.0f,
                        LineType.Continuous);
                    mLineCache.layer = 0x1f;
                } else {
                    mLineCache.points3 = points;
                }
                
            }
        }

        public void Draw() {
            if (mLineCache == null)
                return;
            Vector.Active(mLineCache, mLineCache != null && FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && MapView.MapIsEnabled);
            if(mLineCache.active) {
                Vector.DrawLine3D(mLineCache);
            }
        }
    }
}
