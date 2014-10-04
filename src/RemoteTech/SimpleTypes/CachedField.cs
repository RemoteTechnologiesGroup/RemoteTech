using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public struct CachedField<T>
    {
        public T Field;
        public int Frame;
    }
}
