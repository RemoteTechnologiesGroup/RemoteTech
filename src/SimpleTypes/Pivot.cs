using System;
using UnityEngine;

namespace RemoteTech {
    public class Pivot {
        Transform pivot;
        float increment, angleMinus, anglePlus;
        bool fullcircle = false;

        Vector3 parentOrigRef;

        Vector2 OrigRef {
            get {
                Vector3 tmp = pivot.InverseTransformPoint(pivot.parent.TransformPoint(parentOrigRef));
                return new Vector2(tmp.x, tmp.y);
            }
        }

        public Pivot(Transform pivotin, float incrementIn, Vector2 bounds) {
            pivot = pivotin;
            increment = Mathf.Deg2Rad * incrementIn;
            angleMinus = Mathf.Deg2Rad * bounds.y;
            anglePlus = Mathf.Deg2Rad * bounds.x;
            fullcircle = bounds == Vector2.zero;

            parentOrigRef = pivot.parent.InverseTransformPoint(pivot.TransformPoint(Vector3.up));
        }

        public void SnapToTarget(DynamicTarget tgt) {
            Vector3 tmpTGT = tgt.NoTarget ? (Vector3)OrigRef : pivot.InverseTransformPoint(tgt.Position);
            Vector2 target = new Vector2(tmpTGT.x, tmpTGT.y);

            float angle = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);

            if (angle == 0) return;

            if (target.x > 0) {
                angle = -angle;
            }

            if (!fullcircle) {
                tmpTGT = OrigRef;
                target = new Vector2(tmpTGT.x, tmpTGT.y);
                float angleRef = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);
                if (target.x > 0) {
                    angleRef = -angleRef;
                }

                angle = Mathf.Clamp(angle, angleRef - angleMinus, angleRef + anglePlus);

                if (angle == 0) return;
            }

            pivot.RotateAround(pivot.forward, angle);
        }

        public void RotToTarget(DynamicTarget tgt) {
            Vector3 tmpTGT = tgt.NoTarget ? (Vector3)OrigRef : pivot.InverseTransformPoint(tgt.Position);
            Vector2 target = new Vector2(tmpTGT.x, tmpTGT.y);

            float angle = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);

            if (angle == 0) return;

            angle = Mathf.Clamp(increment * TimeWarp.fixedDeltaTime, 0, angle);
            if (target.x > 0) {
                angle = -angle;
            }

            if (!fullcircle) {
                tmpTGT = OrigRef;
                target = new Vector2(tmpTGT.x, tmpTGT.y);
                float angleRef = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);
                if (target.x > 0) {
                    angleRef = -angleRef;
                }

                angle = Mathf.Clamp(angle, angleRef - angleMinus, angleRef + anglePlus);

                if (angle == 0) return;
            }

            pivot.RotateAround(pivot.forward, angle);
        }

        public bool RotToOrigin() {
            Vector3 tmpTGT = OrigRef;
            Vector2 target = new Vector2(tmpTGT.x, tmpTGT.y);

            float angle = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);

            if (angle == 0) return true;

            angle = Mathf.Clamp(increment * TimeWarp.fixedDeltaTime, 0, angle);
            if (target.x > 0)
                angle = -angle;

            if (!fullcircle) {
                target = new Vector2(tmpTGT.x, tmpTGT.y);
                float angleRef = Mathf.Deg2Rad * Vector2.Angle(Vector2.up, target);
                if (target.x > 0) {
                    angleRef = -angleRef;
                }

                angle = Mathf.Clamp(angle, angleRef - angleMinus, angleRef + anglePlus);

                if (angle != 0)
                    pivot.RotateAround(pivot.forward, angle);
            }
            else
                pivot.RotateAround(pivot.forward, angle);

            return false;
        }

    }
}
