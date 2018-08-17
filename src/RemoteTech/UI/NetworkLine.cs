using RemoteTech.SimpleTypes;
using System.Linq;
using UnityEngine;

namespace RemoteTech.UI
{
    public class NetworkLine : MonoBehaviour
    {
        private static Material CommNetMaterial = null;

        public BidirectionalEdge<ISatellite> Edge
        {
            set
            {
                UpdateMesh(value);
            }
        }

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
                mMeshFilter.mesh.colors = Enumerable.Repeat(value, 4).ToArray();
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
        private Vector3[] mPoints2D = new Vector3[4];
        private Vector3[] mPoints3D = new Vector3[4];

        public static NetworkLine Instantiate()
        {
            return new GameObject("NetworkLine", typeof(NetworkLine)).GetComponent<NetworkLine>();
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

        private void UpdateMesh(BidirectionalEdge<ISatellite> edge)
        {
            var camera = PlanetariumCamera.Camera;

            var start = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(edge.A.Position));
            var end = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(edge.B.Position));

            var segment = new Vector3(end.y - start.y, start.x - end.x, 0).normalized * (LineWidth / 2);

            if (!MapView.Draw3DLines)
            {
                var dist = Screen.height / 2 + 0.01f;
                start.z = start.z >= 0.15f ? dist : -dist;
                end.z = end.z >= 0.15f ? dist : -dist;
            }

            mPoints2D[0] = (start - segment);
            mPoints2D[1] = (start + segment);
            mPoints2D[2] = (end - segment);
            mPoints2D[3] = (end + segment);

            mPoints3D[0] = camera.ScreenToWorldPoint(mPoints2D[0]);
            mPoints3D[1] = camera.ScreenToWorldPoint(mPoints2D[1]);
            mPoints3D[2] = camera.ScreenToWorldPoint(mPoints2D[2]);
            mPoints3D[3] = camera.ScreenToWorldPoint(mPoints2D[3]);

            mMeshFilter.mesh.vertices = MapView.Draw3DLines ? mPoints3D : mPoints2D;
            mMeshFilter.mesh.RecalculateBounds();
            mMeshFilter.mesh.MarkDynamic();
        }

        private void SetupMesh()
        {
            mMeshFilter = gameObject.AddComponent<MeshFilter>();
            mMeshFilter.mesh = new Mesh();
            mRenderer = gameObject.AddComponent<MeshRenderer>();
            mMeshFilter.mesh.name = "NetworkLine";
            mMeshFilter.mesh.vertices = new Vector3[4];
            mMeshFilter.mesh.uv = new Vector2[4] { new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0) };
            mMeshFilter.mesh.SetIndices(new int[] { 0, 2, 1, 2, 3, 1 }, MeshTopology.Triangles, 0);
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