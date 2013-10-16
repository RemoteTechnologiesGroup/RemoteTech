using System;

namespace RemoteTech
{
    public interface IAntenna
    {
        String Name { get; }
        Guid Guid { get; }
        bool Activated { get; }
        bool Powered { get; }

        bool CanTarget { get; }

        Guid Target { get; set; }
        Dish CurrentDish { get; }
        float CurrentOmni { get; }
    }
}
