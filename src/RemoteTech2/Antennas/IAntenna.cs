using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public interface IAntenna
    {
        String Name              { get; }
        Guid Guid                { get; }
        Vector3d Position        { get; }
        bool Activated           { get; set; }
        bool Powered             { get; }
        bool CanTarget           { get; }
        IList<Target> Targets    { get; }
        float CurrentDishRange   { get; }
        double CurrentRadians    { get; }
        float CurrentOmniRange   { get; }
        float CurrentConsumption { get; }
    }
}
