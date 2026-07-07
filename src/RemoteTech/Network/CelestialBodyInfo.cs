using RemoteTech.Collections;
using Unity.Mathematics;

namespace RemoteTech.Network;

internal struct JobBody
{
    public double3 position;
    public double radius;

    public int parent;
    public IntRange subtree;
}
