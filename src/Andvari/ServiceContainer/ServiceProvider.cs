using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    public class ServiceProvider : IServiceProvider, IDisposable
    {
        public Type Implementation { get; private set; }
        public bool Singleton { get; private set; }

        private Object instance;
        private IServiceContainer container;

        public ServiceProvider(Type implementation, IServiceContainer container, bool isSingleton)
        {
            this.Singleton = isSingleton;
            this.Implementation = implementation;
            this.container = container;
        }

        public static ServiceProvider Existing(Object instance, IServiceContainer container)
        {
            var existing = new ServiceProvider(instance.GetType(), container, true);
            existing.instance = instance;

            return existing;
        }

        public void Dispose()
        {
            var disposable = instance as IDisposable;
            if (disposable != null) disposable.Dispose();
        }

        public void Resolve()
        {
            FindParams();
        }

        public Object GetInstance()
        {
            return Singleton ? instance ?? (instance = CreateInstance()) : CreateInstance();
        }

        private Object CreateInstance() 
        {
            var functors = FindParams();
            var parameters = new List<Object>();
            foreach (var functor in functors) {
                parameters.Add(functor.Invoke());
            }

            return Activator.CreateInstance(Implementation, parameters.ToArray());
        }

        private List<Func<Object>> FindParams()
        {
            var constructor = Implementation.GetConstructors().First();

            var parameters = new List<Func<Object>>();
            foreach (var parameter in constructor.GetParameters())
            {
                if (!parameter.ParameterType.IsInterface)
                    throw new NotAnInterfaceException(parameter.ParameterType, Implementation);
                if (!container.IsAvailable(parameter.ParameterType))
                    throw new ImplementationUnavailableException(parameter.ParameterType);

                parameters.Add(() => container.GetInstance(parameter.ParameterType));
            }

            return parameters;
        }
    }
}
