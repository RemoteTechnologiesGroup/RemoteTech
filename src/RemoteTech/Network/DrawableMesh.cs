using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace RemoteTech.Network;

/// <summary>
/// A persistent dynamic mesh plus the buffer sizes it was last declared with, so
/// vertex/index buffer params are only re-set when the counts actually change.
/// </summary>
internal struct DrawableMesh
{
    private const MeshUpdateFlags MeshFlags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds;

    // Orbit lines draw on layer 10, we do the same here in order to match appropriately.
    private const int ScaledSceneryLayer = 10;

    public Mesh mesh;
    private int lastVc, lastIc;

    public void Ensure(string name)
    {
        if (mesh != null) return;
        mesh = new Mesh { name = name };
        mesh.MarkDynamic();
    }

    public void Upload(
        NativeArray<LineVertex> verts,
        NativeArray<uint> indices,
        in Bounds3 b,
        VertexAttributeDescriptor[] layout)
    {
        int vc = verts.Length, ic = indices.Length;

        if (vc != lastVc) { mesh.SetVertexBufferParams(vc, layout); lastVc = vc; }
        mesh.SetVertexBufferData(verts, 0, 0, vc, 0, MeshFlags);
        if (ic != lastIc) { mesh.SetIndexBufferParams(ic, IndexFormat.UInt32); lastIc = ic; }
        mesh.SetIndexBufferData(indices, 0, 0, ic, MeshFlags);
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, ic, MeshTopology.Triangles), MeshUpdateFlags.DontRecalculateBounds);

        float3 center = (b.min + b.max) * 0.5f, size = b.max - b.min;
        mesh.bounds = new Bounds(new Vector3(center.x, center.y, center.z), new Vector3(size.x, size.y, size.z));
    }

    public void Draw(Material material, Camera camera, Matrix4x4 model)
        => Graphics.DrawMesh(mesh, model, material, ScaledSceneryLayer, camera, 0, null, false, false, false);

    public void Destroy()
    {
        if (mesh == null) return;
        Object.Destroy(mesh);
        mesh = null;
    }
}

/// <summary>
/// The map-view connection-line mesh: its persistent GPU buffers together with
/// this tick's pending build, scheduled in LateUpdate and drawn in OnPreCull.
/// </summary>
internal struct LineMesh
{
    public DrawableMesh drawable;
    public LineMeshData frame;
    public bool hasFrame;

    public void Ensure() => drawable.Ensure("RTNetworkLines");

    public void SetFrame(in LineMeshData f)
    {
        frame = f;
        hasFrame = true;
    }

    /// <summary>
    /// Waits on the build, then uploads and draws it translated by <paramref name="delta"/>
    /// onto the current ScaledSpace origin (the build was billboarded against a stale one).
    /// </summary>
    public void CompleteAndDraw(VertexAttributeDescriptor[] layout, Material material, Camera camera, float3 delta)
    {
        frame.handle.Complete();
        if (frame.verts.Length != 0 && frame.indices.Length != 0)
        {
            drawable.Upload(frame.verts.AsArray(), frame.indices.AsArray(), frame.bounds[0], layout);
            drawable.Draw(material, camera, Matrix4x4.Translate(delta));
        }
        DisposeFrame();
    }

    public void Drop()
    {
        if (!hasFrame) return;
        frame.handle.Complete();
        DisposeFrame();
    }

    private void DisposeFrame()
    {
        frame.Dispose();
        hasFrame = false;
    }

    public void Destroy()
    {
        Drop();
        drawable.Destroy();
    }
}

/// <summary>
/// The map-view dish-cone mesh, mirroring <see cref="LineMesh"/>: persistent GPU
/// buffers plus this tick's pending build.
/// </summary>
internal struct ConeMesh
{
    public DrawableMesh drawable;
    public ConeMeshData frame;
    public bool hasFrame;

    public void Ensure() => drawable.Ensure("RTNetworkCones");

    public void SetFrame(in ConeMeshData f)
    {
        frame = f;
        hasFrame = true;
    }

    public void CompleteAndDraw(VertexAttributeDescriptor[] layout, Material material, Camera camera, float3 delta)
    {
        frame.handle.Complete();
        if (frame.verts.Length != 0 && frame.indices.Length != 0)
        {
            drawable.Upload(frame.verts, frame.indices, frame.bounds[0], layout);
            drawable.Draw(material, camera, Matrix4x4.Translate(delta));
        }
        DisposeFrame();
    }

    public void Drop()
    {
        if (!hasFrame)
            return;
        frame.handle.Complete();
        DisposeFrame();
    }

    private void DisposeFrame()
    {
        frame.Dispose();
        hasFrame = false;
    }

    public void Destroy()
    {
        Drop();
        drawable.Destroy();
    }
}

/// <summary>
/// The map-view satellite marks, mirroring <see cref="LineMesh"/>: this tick's
/// pending filter/projection build plus the state that produced it (needed to
/// resolve a mark back to its satellite for the mouse-over panel). Unlike the
/// meshes these are drawn as GUI textures in OnGUI, so the frame is harvested
/// there rather than in OnPreCull.
/// </summary>
internal struct SatelliteMarks
{
    public SatelliteMarkData frame;
    public NetworkState state;
    public bool hasFrame;

    public void SetFrame(in SatelliteMarkData f, NetworkState s)
    {
        frame = f;
        state = s;
        hasFrame = true;
    }

    /// <summary>
    /// Waits on the build and returns the projected marks; the frame is kept until
    /// the next <see cref="Drop"/> so repeated OnGUI repaints can redraw it.
    /// </summary>
    public NativeArray<SatelliteMark> Complete()
    {
        frame.handle.Complete();
        return frame.marks;
    }

    public void Drop()
    {
        if (!hasFrame)
            return;

        frame.Dispose();
        state = null;
        hasFrame = false;
    }

    public void Destroy() => Drop();
}
