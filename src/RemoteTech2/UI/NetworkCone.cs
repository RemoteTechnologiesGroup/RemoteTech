using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class NetworkCone : MonoBehaviour
    {
        public CelestialBody Planet
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
            SetupMesh();
            gameObject.layer = 31;
            LineWidth = 1.0f;
            Color = Color.white;
            Material = new Material("Shader \"Vertex Colors/Alpha\" {Category{Tags {\"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\"}SubShader {Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha Pass {BindChannels {Bind \"Color\", color Bind \"Vertex\", vertex}}}}}");
        }

        private void UpdateMesh(CelestialBody cb, IAntenna a)
        {
            var camera = MapView.MapCamera.camera;

            var antenna_pos = ScaledSpace.LocalToScaledSpace(RTCore.Instance.Network[Antenna.Guid].Position);
            var planet_pos = ScaledSpace.LocalToScaledSpace(cb.position);

            var up = cb.transform.up;
            var space = Vector3.Cross(planet_pos - antenna_pos, up).normalized * Vector3.Distance(antenna_pos, planet_pos) * (float)Math.Tan(Math.Acos(a.Radians));
            var end1 = antenna_pos + (planet_pos + space - antenna_pos).normalized * Math.Min(a.Dish / ScaledSpace.ScaleFactor, Vector3.Distance(antenna_pos, planet_pos));
            var end2 = antenna_pos + (planet_pos - space - antenna_pos).normalized * Math.Min(a.Dish / ScaledSpace.ScaleFactor, Vector3.Distance(antenna_pos, planet_pos));


            var line_start = camera.WorldToScreenPoint(antenna_pos);
            var line_end1 = camera.WorldToScreenPoint(end1);
            var line_end2 = camera.WorldToScreenPoint(end2);
            var segment1 = new Vector3(line_end1.y - line_start.y, line_start.x - line_end1.x, 0).normalized * (LineWidth / 2);
            var segment2 = new Vector3(line_end2.y - line_start.y, line_start.x - line_end2.x, 0).normalized * (LineWidth / 2);

            if (!MapView.Draw3DLines)
            {
                var dist = Screen.height / 2;
                line_start.z = line_start.z > 0 ? dist : -dist;
                line_end1.z = line_end1.z > 0 ? dist : -dist;
                line_end2.z = line_end2.z > 0 ? dist : -dist;
            }

            mPoints2D[0] = (line_start - segment1);
            mPoints2D[1] = (line_start + segment1);
            mPoints2D[2] = (line_end1 - segment1);
            mPoints2D[3] = (line_end1 + segment1);
            mPoints2D[4] = (line_start - segment2);
            mPoints2D[5] = (line_start + segment2);
            mPoints2D[6] = (line_end2 - segment2);
            mPoints2D[7] = (line_end2 + segment2);

            for (int i = 0; i < 8; i++)
            {
                mPoints3D[i] = camera.ScreenToWorldPoint(mPoints2D[i]);
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
            Active = false;
        }

        public void Destroy()
        {
            GameObject.Destroy(gameObject);
        }
    }
}