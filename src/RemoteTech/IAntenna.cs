using System;

namespace RemoteTech
{
    public interface IAntenna : IComparable<IAntenna>
    {
        String Name { get; }
        Guid Guid { get; }
        bool Activated { get; set; }
        bool Powered { get; }
        bool CanTarget { get; }
        Guid Target { get; set; }
        float Dish { get; }
        double CosAngle { get; }
        float Omni { get; }
        float Consumption { get; }

        void OnConnectionRefresh();
    }
}
