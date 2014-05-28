using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Andvari
{
    public interface IEventManager
    {
        void Register<E>(Action<E> action);
        void Unregister<E>(Action<E> action);
    }
}
