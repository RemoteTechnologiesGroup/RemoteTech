using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    public class ServiceException : Exception
    {
        public ServiceException() : base() { }
        public ServiceException(String message) : base(message) { }
        public ServiceException(String message, Exception inner) : base(message, inner) { }
    }

    public class AlreadyActivatingException : ServiceException
    {
        public AlreadyActivatingException(Type impl)
            : base(String.Format("The implementation {0} was requested while in a transient state. Possible circular dependency.", impl)) { }
    }

    public class ImplementationUnavailableException : ServiceException
    {
        public ImplementationUnavailableException(Type iface)
            : base(String.Format("Failed to find an implementation for interface {0}.", iface)) { }
    }

    public class NotAnInterfaceException : ServiceException
    {
        public NotAnInterfaceException(Type impl, Type iface)
            : base(String.Format("Refusing to inject type {0} into service {1}: is an implementation.", impl, iface)) { }
    }

    public class AlreadyRegisteredException : ServiceException
    {
        public AlreadyRegisteredException(Type impl)
            : base(String.Format("A service with implementation {0} is already registered.", impl)) { }
    }
}
