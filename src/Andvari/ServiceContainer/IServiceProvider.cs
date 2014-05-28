using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    public interface IServiceProvider
    {
        void Resolve();
        Object GetInstance();
    }
}
