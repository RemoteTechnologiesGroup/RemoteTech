using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public interface ICelestialBody
    {
        String Name           { get; }
        Guid Guid             { get; }
        Vector3d Position     { get; }
        ICelestialBody Parent { get; }
        Color Color           { get; }
        Vector3d Up           { get; }
        double Radius         { get; }

        double SemiMajorAxis  { get; }
    }
}
