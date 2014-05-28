using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    public class ServiceDescriptor
    {
        public Type[] Interfaces { get; private set; }
        public Type Implementation { get; private set; }
        public bool Singleton { get; set; }
        protected ServiceDescriptor(Type implementation, params Type[] interfaces)
        {
            this.Interfaces = interfaces;
            this.Implementation = implementation;
        }

        public ServiceDescriptor AsSingleton()
        {
            Singleton = true;
            return this;
        }
    }

    public class ServiceDescriptor<T> : ServiceDescriptor
    {
        public ServiceDescriptor(params Type[] interfaces) 
            : base(typeof(T), interfaces) {}
    }
}
