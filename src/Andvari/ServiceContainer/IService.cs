using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    public enum ServiceState
    {
        Inactive = 0,
        Activating = 1,
        Active = 2,
    }

    public delegate void ServiceStateChangeEvent(IService service, ServiceState state);
    public interface IService
    {
        Type Implementation { get; }
        Type[] Interfaces { get; }
        bool IsSingleton { get; }
        ServiceState State { get; }

        void Resolve();
        Object GetInstance();

        event ServiceStateChangeEvent OnServiceStateChanged;
    }
}
