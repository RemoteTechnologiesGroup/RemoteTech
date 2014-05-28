using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    public class Service : IService
    {
        public Type Implementation { get; private set; }
        public Type[] Interfaces { get; private set; }
        public bool IsSingleton { get; private set; }
        public ServiceState State { get; private set; }

        private IServiceProvider provider;

        private Service() { }
        public Service(Type implementation, IServiceContainer container, Type[] interfaces, bool isSingleton)
        {
            this.Implementation = implementation;
            this.Interfaces = interfaces;
            this.IsSingleton = isSingleton;

            this.State = ServiceState.Inactive;

            this.provider = new ServiceProvider(implementation, container, isSingleton);
        }
        public void Dispose()
        {
            var disposable = provider as IDisposable;
            if (disposable != null) disposable.Dispose();
        }

        public static Service Existing(Object instance, IServiceContainer container, Type[] interfaces)
        {
            return new Service()
            {
                Implementation = instance.GetType(),
                Interfaces = interfaces,
                IsSingleton = true,
                State = ServiceState.Active,
                provider = ServiceProvider.Existing(instance, container)
            };
        }

        public void Resolve()
        {
            provider.Resolve();
        }

        public Object GetInstance()
        {
            if (State == ServiceState.Inactive) 
                Resolve();

            if (State == ServiceState.Activating)
                throw new AlreadyActivatingException(Implementation);

            try
            {
                State = ServiceState.Activating;
                var instance = provider.GetInstance();
                State = ServiceState.Active;
                return instance;
            }
            catch (ServiceException e)
            {
                throw e;
            }
        }
    }
}
