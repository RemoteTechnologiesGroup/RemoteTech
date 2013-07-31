using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public class VectorFin {
        Transform Pivot,
            Tip,
            Anchor;

        float Circ1Rad, TargetDist;

        Vector3 ParentFixPoint;
        Vector2 FPP {
            get {
                Vector3 tmp = Pivot.InverseTransformPoint(Pivot.parent.TransformPoint(ParentFixPoint));
                return new Vector2(tmp.x, tmp.y);
            }
        }

        Vector3 SphCenter {
            get {
                return Pivot.InverseTransformPoint(Anchor.position);
            }
        }

        Vector3 TipPos {
            get {
                return Tip.localPosition;
            }
        }


        public VectorFin(Transform PivotIn, Transform TipIn, Transform AnchorIn) {
            Pivot = PivotIn;
            Tip = TipIn;
            Anchor = AnchorIn;

            ParentFixPoint = Pivot.parent.InverseTransformPoint(Tip.position);
            Circ1Rad = Vector3.Distance(Vector3.zero, TipPos);
            TargetDist = Vector3.Distance(Tip.localPosition, SphCenter);
        }

        public void Update() {
            Vector3 TIPtmp = TipPos, SPHtmp = SphCenter;

            if (Vector3.Distance(TIPtmp, SPHtmp) == TargetDist) return;

            // Project centers and coordinates within Pivot localSpace to the xy plane.
            Vector2
                tip = new Vector2(TIPtmp.x, TIPtmp.y),
                Circ1Cen = new Vector2(0, 0),
                Circ2Cen = new Vector2(SPHtmp.x, SPHtmp.y),
                FixPoint = FPP;

            // Find the radius of the circle intersection between the xy plane and a sphere with the anchor as center and target distance as radius. Using pythagoras.
            float Circ2Rad = (float)Math.Sqrt(Math.Pow(TargetDist, 2) + Math.Pow(SPHtmp.z, 2)), dist = Vector2.Distance(Circ1Cen, Circ2Cen);

            // Find a and h.
            double a = (Circ1Rad * Circ1Rad - Circ2Rad * Circ2Rad + dist * dist) / (2 * dist);
            double h = Math.Sqrt(Circ1Rad * Circ1Rad - a * a);

            // Find P2.
            double cx2 = Circ1Cen.x + a * (Circ2Cen.x - Circ1Cen.x) / dist;
            double cy2 = Circ1Cen.y + a * (Circ2Cen.y - Circ1Cen.y) / dist;

            // Get the points P3.
            Vector2 intersection1 = new Vector2(
                (float)(cx2 + h * (Circ2Cen.y - Circ1Cen.y) / dist),
                (float)(cy2 - h * (Circ2Cen.x - Circ1Cen.x) / dist));
            Vector2 intersection2 = new Vector2(
                (float)(cx2 - h * (Circ2Cen.y - Circ1Cen.y) / dist),
                (float)(cy2 + h * (Circ2Cen.x - Circ1Cen.x) / dist));

            float angle = 0;
            if (Vector2.Distance(intersection1, FixPoint) < Vector2.Distance(intersection2, FixPoint)) {
                angle = Mathf.Deg2Rad * Vector2.Angle(tip, intersection1);
                if (intersection1.x > tip.x)
                    angle = -angle;
            } else {
                angle = Mathf.Deg2Rad * Vector2.Angle(tip, intersection2);
                if (intersection2.x > tip.x)
                    angle = -angle;
            }

            Pivot.RotateAround(Pivot.forward, angle);
        }

    }

    public class FinSet : HashSet<VectorFin> {
        public void Update(bool Active) {
            if (Active) {
                foreach (VectorFin fin in this)
                    fin.Update();
            }
        }
    }
}
