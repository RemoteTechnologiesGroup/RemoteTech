using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    public delegate void ServiceRegistrationEvent(ServiceRegistrationEventArgs args);
    public class ServiceRegistrationEventArgs 
    {
        public IServiceContainer Container { get; set; }
        public IService Service { get; set; }
    }

    public delegate void ServiceUnregistrationEvent(ServiceUnregistrationEventArgs args);
    public class ServiceUnregistrationEventArgs
    {
        public IServiceContainer Container { get; set; }
        public IService Service { get; set; }
    }
    public interface IServiceContainer
    {
        ServiceDescriptor<T> NewService<T>(params Type[] interfaces);
        void Register<T>(ServiceDescriptor<T> service);
        T GetInstance<T>();
        Object GetInstance(Type iface);
        bool IsAvailable<T>();
        bool IsAvailable(Type iface);

        event ServiceRegistrationEvent ServiceRegistered;
        event ServiceUnregistrationEvent ServiceUnregistered;
    }
}
