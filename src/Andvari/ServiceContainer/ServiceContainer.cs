using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace Andvari
{
    public class ServiceContainer : IServiceContainer, IDisposable
    {
        private IDictionary<Type, IList<IService>> serviceInterfaceMap = new Dictionary<Type, IList<IService>>();
        private IDictionary<Type, IService> serviceImplementationMap = new Dictionary<Type, IService>();

        public event EventHandler<ServiceRegistrationEventArgs> ServiceRegistered = delegate {};
        public event EventHandler<ServiceUnregistrationEventArgs> ServiceUnregistered = delegate { };

        public ServiceContainer()
        {
            var self = CreateOwnService();
            serviceInterfaceMap[typeof(ServiceContainer)] = new List<IService>() { self };
            serviceImplementationMap[typeof(ServiceContainer)] = self;
        }

        public void Dispose()
        {
            foreach (var provider in serviceImplementationMap.Values)
            {
                var disposable = provider as IDisposable;
                if (disposable != null && disposable != this) disposable.Dispose();
            }

            serviceInterfaceMap.Clear();
            serviceImplementationMap.Clear();
        }

        public ServiceDescriptor<TImpl> NewService<TImpl>(params Type[] interfaces)
        {
            return new ServiceDescriptor<TImpl>(interfaces);
        }
        public void Register<TImpl>(ServiceDescriptor<TImpl> descriptor)
        {
            if (serviceImplementationMap.ContainsKey(typeof(TImpl)))
                throw new AlreadyRegisteredException(typeof(TImpl));

            var service = CreateService(descriptor);

            serviceImplementationMap[typeof(TImpl)] = service;       

            foreach (var i in service.Interfaces)
            {
                if (!serviceInterfaceMap.ContainsKey(i))
                {
                    serviceInterfaceMap[i] = new List<IService>();
                }

                serviceInterfaceMap[i].Add(service);
            }
        }

        public void RegisterAll(params ServiceDescriptor[] descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (serviceImplementationMap.ContainsKey(descriptor.Implementation))
                    throw new AlreadyRegisteredException(descriptor.Implementation);
            }

            var services = new List<IService>();
            foreach (var descriptor in descriptors)
            {
                var service = CreateService(descriptor);
                services.Add(service);
                serviceImplementationMap[descriptor.Implementation] = service;

                foreach (var i in service.Interfaces)
                {
                    if (!serviceInterfaceMap.ContainsKey(i))
                    {
                        serviceInterfaceMap[i] = new List<IService>();
                    }

                    serviceInterfaceMap[i].Add(service);
                }
            }
        }

        public void Unregister<TImpl>()
        {
            if (!serviceImplementationMap.ContainsKey(typeof(TImpl)))
                return;

            var service = serviceImplementationMap[typeof(TImpl)];

            var toDelete = new List<Type>();
            foreach (var pair in serviceInterfaceMap)
            {
                var match = pair.Value.Where(s => s.Implementation == typeof(TImpl));
                foreach (var s in match) pair.Value.Remove(s);

                if (!pair.Value.Any())
                {
                    toDelete.Add(pair.Key);
                }
            }
            foreach (var t in toDelete) serviceInterfaceMap.Remove(t);

            if (service is IDisposable)
            {
                ((IDisposable)service).Dispose();
            }
        }

        public Object GetInstance(Type iface)
        {
            if (!iface.IsInterface)
                throw new NotAnInterfaceException(iface, iface);
            if (!IsAvailable(iface))
                throw new ImplementationUnavailableException(iface);

            var service = serviceInterfaceMap[iface].OrderByDescending(k => k).First();

            return service.GetInstance();
        }
        public IFace GetInstance<IFace>()
        {
            return (IFace) GetInstance(typeof(IFace));
        }

        public bool IsAvailable(Type iface) {
            return serviceInterfaceMap.ContainsKey(iface) && serviceInterfaceMap[iface].Any();
        }

        public bool IsAvailable<IFace>()
        {
            return IsAvailable(typeof(IFace));
        }

        private IServiceProvider CreateInstanceProvider(Type implementation, bool isSingleton)
        {
            return new ServiceProvider(implementation, this, isSingleton);
        }

        private IService CreateService(ServiceDescriptor descriptor)
        {
            return new Service(descriptor.Implementation, this, descriptor.Interfaces, descriptor.Singleton);
        }

        private IService CreateOwnService()
        {
            return Service.Existing(this, this, new Type[] { typeof(IServiceContainer) });
        }
    }
}
