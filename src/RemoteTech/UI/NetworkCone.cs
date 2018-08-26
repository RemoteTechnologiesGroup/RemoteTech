using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech.UI
{
    public class NetworkCone : MonoBehaviour
    {
        private static Material CommNetMaterial = null;

        public Vector3d Center
        {
            set
            {
                UpdateMesh(value, Antenna);
            }
        }

        public IAntenna Antenna { get; set; }

        public float LineWidth { get; set; }

        public Material Material
        {
            set
            {
                mRenderer.material = value;
            }
        }

        public Color Color
        {
            set
            {
                mMeshFilter.mesh.colors = Enumerable.Repeat(value, 8).ToArray();
            }
        }

        public bool Active
        {
            set
            {
                mRenderer.enabled = value;
                gameObject.SetActive(value);
            }
        }

        private MeshFilter mMeshFilter;
        private MeshRenderer mRenderer;
        private Vector3[] mPoints2D = new Vector3[8];
        private Vector3[] mPoints3D = new Vector3[8];

        public static NetworkCone Instantiate()
        {
            return new GameObject("NetworkCone", typeof(NetworkCone)).GetComponent<NetworkCone>();
        }

        public void Awake()
        {
            if (CommNetMaterial == null) { CommNetMaterial = Resources.Load<Material>("Telemetry/TelemetryMaterial"); }

            SetupMesh();
            gameObject.layer = 31;
            LineWidth = 1.0f;
            Color = Color.white;
            Material = CommNetMaterial;
        }

        private void UpdateMesh(Vector3d center, IAntenna dish)
        {
            var camera = PlanetariumCamera.Camera;

            Vector3d antennaPos = ScaledSpace.LocalToScaledSpace(RTCore.Instance.Network[dish.Guid].Position);
            Vector3d planetPos = ScaledSpace.LocalToScaledSpace(center);

            CelestialBody refFrame = (MapView.MapCamera.target.vessel != null 
                ? MapView.MapCamera.target.vessel.mainBody
                : MapView.MapCamera.target.celestialBody);
            Vector3 up = (refFrame != null ? refFrame.transform.up : Vector3.up);

            Vector3 space = Vector3.Cross(planetPos - antennaPos, up).normalized 
                * Vector3.Distance(antennaPos, planetPos) 
                * (float)Math.Tan(Math.Acos(dish.CosAngle));
            Vector3d end1 = antennaPos + (planetPos + space - antennaPos).normalized 
                * Math.Min(dish.Dish / ScaledSpace.ScaleFactor, Vector3.Distance(antennaPos, planetPos));
            Vector3d end2 = antennaPos + (planetPos - space - antennaPos).normalized 
                * Math.Min(dish.Dish / ScaledSpace.ScaleFactor, Vector3.Distance(antennaPos, planetPos));

            Vector3 lineStart = camera.WorldToScreenPoint(antennaPos);
            Vector3 lineEnd1 = camera.WorldToScreenPoint(end1);
            Vector3 lineEnd2 = camera.WorldToScreenPoint(end2);
            var segment1 = new Vector3(lineEnd1.y - lineStart.y, lineStart.x - lineEnd1.x, 0).normalized * (LineWidth / 2);
            var segment2 = new Vector3(lineEnd2.y - lineStart.y, lineStart.x - lineEnd2.x, 0).normalized * (LineWidth / 2);

            if (!MapView.Draw3DLines)
            {
                //if position is behind camera 
                if (lineStart.z < 0)
                {
                    Vector3 coneCenter = camera.WorldToScreenPoint(planetPos);
                    lineStart = NetworkLine.FlipDirection(lineStart, coneCenter);
                }
                else if (lineEnd1.z < 0 || lineEnd2.z < 0)
                {
                    lineEnd1 = NetworkLine.FlipDirection(lineEnd1, lineStart);
                    lineEnd2 = NetworkLine.FlipDirection(lineEnd2, lineStart);
                }

                int dist = Screen.height / 2;
                lineStart.z = lineStart.z > 0 ? dist : -dist;
                lineEnd1.z = lineEnd1.z > 0 ? dist : -dist;
                lineEnd2.z = lineEnd2.z > 0 ? dist : -dist;
                
                mPoints2D[0] = (lineStart - segment1);
                mPoints2D[1] = (lineStart + segment1);
                mPoints2D[2] = (lineEnd1 - segment1);
                mPoints2D[3] = (lineEnd1 + segment1);
                mPoints2D[4] = (lineStart - segment2);
                mPoints2D[5] = (lineStart + segment2);
                mPoints2D[6] = (lineEnd2 - segment2);
                mPoints2D[7] = (lineEnd2 + segment2);
            }
            else
            {
                mPoints3D[0] = camera.ScreenToWorldPoint(lineStart - segment1);
                mPoints3D[1] = camera.ScreenToWorldPoint(lineStart + segment1);
                mPoints3D[2] = camera.ScreenToWorldPoint(lineEnd1 - segment1);
                mPoints3D[3] = camera.ScreenToWorldPoint(lineEnd1 + segment1);
                mPoints3D[4] = camera.ScreenToWorldPoint(lineStart - segment2);
                mPoints3D[5] = camera.ScreenToWorldPoint(lineStart + segment2);
                mPoints3D[6] = camera.ScreenToWorldPoint(lineEnd2 - segment2);
                mPoints3D[7] = camera.ScreenToWorldPoint(lineEnd2 + segment2);
            }

            mMeshFilter.mesh.vertices = MapView.Draw3DLines ? mPoints3D : mPoints2D;

            if (!MapView.Draw3DLines)
            {
                var bounds = new Bounds();
                bounds.center = new Vector3(Screen.width / 2, Screen.height / 2, Screen.height / 2);
                bounds.extents = new Vector3(Screen.width * 100, Screen.height * 100, 0.1f);
                mMeshFilter.mesh.bounds = bounds;
            }
            else
            {
                mMeshFilter.mesh.RecalculateBounds();
            }
        }

        private void SetupMesh()
        {
            mMeshFilter = gameObject.AddComponent<MeshFilter>();
            mMeshFilter.mesh = new Mesh();
            mRenderer = gameObject.AddComponent<MeshRenderer>();
            mMeshFilter.mesh.name = "NetworkLine";
            mMeshFilter.mesh.vertices = new Vector3[8];
            mMeshFilter.mesh.uv = new Vector2[8] { new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0) };
            mMeshFilter.mesh.SetIndices(new int[] { 0, 2, 1, 2, 3, 1,  4, 6, 5, 6, 7, 5}, MeshTopology.Triangles, 0);
            mMeshFilter.mesh.MarkDynamic();
            Active = false;
        }

        public void OnDestroy()
        {
            Active = false;
            Destroy(mMeshFilter);
            Destroy(mRenderer);
        }
    }
}