using System;
using RemoteTech.Collections;
using RemoteTech.Network;
using RemoteTech.SimpleTypes;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

using KSP.Localization;
using System.Text;

namespace RemoteTech;

[Flags]
public enum MapFilter
{
    None   = 0,
    Omni   = 1,
    Dish   = 2,
    Sphere = 4,
    Cone   = 8,
    Planet = 8,     // For backward compatibility with RemoteTech 1.4 and earlier
                    // Cone should be first, so that it's the one that appears in settings file
    Path   = 16,
    MultiPath = 32
}

/// <summary>
/// RemoteTech UI network render in charre of drawing connection links in tracking station or flight map scenes.
/// </summary>
// ScaledSpace.LateUpdate runs at execution order 9000, we need to run after that.
[DefaultExecutionOrder(30000)]
public class NetworkRenderer : MonoBehaviour
{
    public MapFilter Filter
    {
        get => RTSettings.Instance.MapFilter;
        set
        {
            RTSettings.Instance.MapFilter = value;
            RTSettings.Instance.Save();
        }
    }

    private static readonly Texture2D mTexMark;
    private static float mLineWidth = 1f;

    // Connection lines and dish cones are each one dynamic mesh, built by a job
    // chain in LateUpdate and drawn with a single Graphics.DrawMesh on the
    // scaled-space camera in OnPreCull.
    private static Material mLineMaterial;
    private VertexAttributeDescriptor[] mVertexLayout;
    private LineMesh mLine;
    private ConeMesh mCone;

    // Satellite marks are filtered and projected by a job in LateUpdate and drawn
    // as GUI textures in OnGUI.
    private SatelliteMarks mMarks;

    public bool ShowOmni  { get { return (Filter & MapFilter.Omni)   == MapFilter.Omni; } }
    public bool ShowDish  { get { return (Filter & MapFilter.Dish)   == MapFilter.Dish; } }
    public bool ShowPath  { get { return (Filter & MapFilter.Path)   == MapFilter.Path; } }
    public bool ShowMultiPath { get { return (Filter & MapFilter.MultiPath) == MapFilter.MultiPath; } }
    public bool ShowRange { get { return (Filter & MapFilter.Sphere) == MapFilter.Sphere; } }
    public bool ShowCone  { get { return (Filter & MapFilter.Cone)   == MapFilter.Cone; } }

    public GUIStyle smallStationText;
    public GUIStyle smallStationHead;

    static NetworkRenderer()
    {
        RTUtil.LoadImage(out mTexMark, "mark");

        if(Versioning.version_major == 1)
        {
            switch(Versioning.version_minor)
            {
                case 4:
                    mLineWidth = 1f; //1f is matching to CommNet's line width
                    break;
                default:
                    mLineWidth = 3f;
                    break;
            }
        }
    }

    public static NetworkRenderer CreateAndAttach()
    {
        var renderer = MapView.MapCamera.gameObject.GetComponent<NetworkRenderer>();
        if (renderer)
        {
            Destroy(renderer);
        }

        renderer = MapView.MapCamera.gameObject.AddComponent<NetworkRenderer>();

        renderer.smallStationHead = new GUIStyle(HighLogic.Skin.label)
        {
            fontSize = 12
        };

        renderer.smallStationText = new GUIStyle(HighLogic.Skin.label)
        {
            fontSize = 10,
            normal = { textColor = Color.white }
        };

        return renderer;
    }

    public void LateUpdate()
    {
        // Defensive: a prior frame scheduled but OnPreCull/OnGUI never harvested it.
        mLine.Drop();
        mCone.Drop();
        mMarks.Drop();

        if (!MapView.MapIsEnabled && HighLogic.LoadedScene != GameScenes.TRACKSTATION) return;

        // Next, not Current, to avoid a full physics tick of lag.
        var state = RTCore.Instance.Network.Next;
        if (state == null) return;
        state.Complete();

        EnsureMeshes();

        if (state.NodeCount >= 2 && state.PairCount >= 1)
        {
            var p = CaptureDrawParams(state, state.NodeCount, state.PairCount);
            var view = CaptureViewParams();
            mLine.SetFrame(state.ScheduleLineMesh(p, view));
        }

        if (MapView.MapIsEnabled && ShowCone && state.ConeCandidateCount > 0)
        {
            var cp = CaptureConeViewParams();
            mCone.SetFrame(state.ScheduleConeMesh(cp));
        }

        if (MapView.MapIsEnabled && state.MarkCandidateCount > 0)
            mMarks.SetFrame(state.ScheduleSatelliteMarks(CaptureMarkViewParams()), state);

        JobHandle.ScheduleBatchedJobs();
    }

    public void OnPreCull()
    {
        if (mLine.hasFrame)
        {
            if (MapView.MapIsEnabled || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                float3 delta = (float3)(mLine.frame.builtOffset - CurrentTotalOffset());
                mLine.CompleteAndDraw(mVertexLayout, mLineMaterial, PlanetariumCamera.Camera, delta);
            }
            else
                mLine.Drop();
        }

        if (mCone.hasFrame)
        {
            if (MapView.MapIsEnabled)
            {
                float3 delta = (float3)(mCone.frame.builtOffset - CurrentTotalOffset());
                mCone.CompleteAndDraw(mVertexLayout, mLineMaterial, PlanetariumCamera.Camera, delta);
            }
            else
                mCone.Drop();
        }
    }

    private void EnsureMeshes()
    {
        if (mLineMaterial == null)
            mLineMaterial = Resources.Load<Material>("Telemetry/TelemetryMaterial");
        mLine.Ensure();
        mCone.Ensure();
        mVertexLayout ??=
        [
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0),
        ];
    }

    private DrawEdgeParams CaptureDrawParams(NetworkState state, int nodeCount, int pairCount)
    {
        var settings = RTSettings.Instance;
        Vessel target = PlanetariumCamera.fetch != null && PlanetariumCamera.fetch.target != null
            ? PlanetariumCamera.fetch.target.vessel : null;
        ISatellite targetSat = target != null ? RTCore.Instance.Satellites[target] : null;
        int targetNode = ShowPath && targetSat != null && state.TryGetNode(targetSat, out int tn) ? tn : -1;

        return new DrawEdgeParams
        {
            targetNode = targetNode,
            nodeCount = nodeCount,
            pairCount = pairCount,
            showOmni = (byte)(ShowOmni ? 1 : 0),
            showDish = (byte)(ShowDish ? 1 : 0),
            showPath = (byte)(ShowPath ? 1 : 0),
            showMultiPath = (byte)(ShowMultiPath ? 1 : 0),
            signalRelay = (byte)(settings.SignalRelayEnabled ? 1 : 0),
            active = settings.ActiveConnectionColor,
            direct = settings.DirectConnectionColor,
            omni = settings.OmniConnectionColor,
            dish = settings.DishConnectionColor,
            grey = (Color32)XKCDColors.Grey,
        };
    }

    // LocalToScaledSpace(p) = p*InverseScaleFactor - totalOffset (the exact float scale KSP uses).
    private static double3 CurrentTotalOffset()
    {
        Vector3d originScaled = ScaledSpace.LocalToScaledSpace(Vector3d.zero); // == -totalOffset
        return new double3(-originScaled.x, -originScaled.y, -originScaled.z);
    }

    private LineMeshViewParams CaptureViewParams()
    {
        Camera cam = PlanetariumCamera.Camera;
        return new LineMeshViewParams
        {
            invScale = ScaledSpace.InverseScaleFactor,
            totalOffset = CurrentTotalOffset(),
            view = ToFloat4x4(cam.worldToCameraMatrix),
            proj = ToFloat4x4(cam.projectionMatrix),
            invView = ToFloat4x4(cam.worldToCameraMatrix.inverse),
            invProj = ToFloat4x4(cam.projectionMatrix.inverse),
            pixelWidth = cam.pixelWidth,
            pixelHeight = cam.pixelHeight,
            halfWidth = mLineWidth * 0.5f,
            mode = MapView.Draw3DLines ? 0 : 1,
        };
    }

    private ConeViewParams CaptureConeViewParams()
    {
        Camera cam = PlanetariumCamera.Camera;
        CelestialBody refFrame = MapView.MapCamera.target.vessel != null
            ? MapView.MapCamera.target.vessel.mainBody
            : MapView.MapCamera.target.celestialBody;
        Vector3 up = refFrame != null ? refFrame.transform.up : Vector3.up;

        return new ConeViewParams
        {
            invScale = ScaledSpace.InverseScaleFactor,
            totalOffset = CurrentTotalOffset(),
            refUp = up,
            view = ToFloat4x4(cam.worldToCameraMatrix),
            proj = ToFloat4x4(cam.projectionMatrix),
            invView = ToFloat4x4(cam.worldToCameraMatrix.inverse),
            invProj = ToFloat4x4(cam.projectionMatrix.inverse),
            pixelWidth = cam.pixelWidth,
            pixelHeight = cam.pixelHeight,
            halfWidth = mLineWidth * 0.5f,
            mode = MapView.Draw3DLines ? 0 : 1,
            color = Color.gray,
        };
    }

    private SatelliteMarkViewParams CaptureMarkViewParams()
    {
        Camera cam = PlanetariumCamera.Camera;
        var settings = RTSettings.Instance;
        Vector3d camLocal = ScaledSpace.ScaledToLocalSpace(cam.transform.position);

        return new SatelliteMarkViewParams
        {
            invScale = ScaledSpace.InverseScaleFactor,
            totalOffset = CurrentTotalOffset(),
            camPosLocal = new double3(camLocal.x, camLocal.y, camLocal.z),
            view = ToFloat4x4(cam.worldToCameraMatrix),
            proj = ToFloat4x4(cam.projectionMatrix),
            pixelWidth = cam.pixelWidth,
            pixelHeight = cam.pixelHeight,
            screenHeight = Screen.height,
            hideBehindBody = settings.HideGroundStationsBehindBody,
            hideOnDistance = settings.HideGroundStationsOnDistance,
            distanceThreshold = settings.DistanceToHideGroundStations,
        };
    }

    private static float4x4 ToFloat4x4(Matrix4x4 m) => new float4x4(
        m.m00, m.m01, m.m02, m.m03,
        m.m10, m.m11, m.m12, m.m13,
        m.m20, m.m21, m.m22, m.m23,
        m.m30, m.m31, m.m32, m.m33);

    public void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !MapView.MapIsEnabled || !mMarks.hasFrame)
            return;

        var marks = mMarks.Complete();
        for (int i = 0; i < marks.Length; i++)
            DrawSatelliteMark(marks[i]);
    }

    private static readonly string NetworkFBOmni = Localizer.Format("#RT_NetworkFB_Omni");
    private static readonly string NetworkFBDish = Localizer.Format("#RT_NetworkFB_Dish");
    private void DrawSatelliteMark(in SatelliteMark mark)
    {
        var screenRect = new Rect(mark.screenPos.x - 8, mark.screenPos.y - 8, 16, 16);

        Color pushColor = GUI.color;
        // tint the white mark.png into the defined color
        GUI.color = mark.color;
        // draw the mark.png
        GUI.DrawTexture(screenRect, mTexMark, ScaleMode.ScaleToFit, true);
        GUI.color = pushColor;

        if (!RTSettings.Instance.ShowMouseOverInfoGroundStations)
            return;

        ISatellite s = mMarks.state.SatAt(mark.nodeIndex);
        if (s is not MissionControlSatellite || !screenRect.ContainsMouse())
            return;

        // Show Mouse over informations to the ground station
        Rect headline = screenRect;
        Vector2 nameDim = this.smallStationHead.CalcSize(new GUIContent(s.Name));

        headline.x -= nameDim.x + 10;
        headline.y -= 3;
        headline.width = nameDim.x;
        headline.height = 14;
        // draw headline of the station
        GUI.Label(headline, s.Name, this.smallStationHead);

        // loop antennas
        var satelliteMarkBuilder = StringBuilderCache.Acquire();
        foreach (var antenna in s.Antennas)
        {
            if(antenna.Omni > 0)
            {
                // Omni:
                satelliteMarkBuilder.AppendFormat(
                    "{0}{1}{2}",
                    NetworkFBOmni,
                    RTUtil.FormatSI(antenna.Omni, "m"),
                    Environment.NewLine
                );
            }

            if (antenna.Dish > 0)
            {
                // Dish: =
                satelliteMarkBuilder.AppendFormat(
                    "{0}{1}{2}",
                    NetworkFBDish,
                    RTUtil.FormatSI(antenna.Dish, "m"),
                    Environment.NewLine
                );
            }
        }

        var antennaRanges = satelliteMarkBuilder.ToStringAndRelease();
        if (string.IsNullOrEmpty(antennaRanges))
            return;

        Rect antennas = screenRect;
        var content = new GUIContent(antennaRanges);

        Vector2 antennaDim = smallStationText.CalcSize(content);
        float maxHeight = smallStationText.CalcHeight(content, antennaDim.x);

        antennas.y += headline.height - 3;
        antennas.x -= antennaDim.x + 10;
        antennas.width = antennaDim.x;
        antennas.height = maxHeight;

        // draw antenna infos of the station
        GUI.Label(antennas, antennaRanges, smallStationText);
    }

    public void Detach()
    {
        Destroy(this);
    }

    public void OnDestroy()
    {
        mLine.Destroy();
        mCone.Destroy();
        mMarks.Destroy();
    }
}