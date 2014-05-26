using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class CachedField<T>
    {
        private T field;
        private int frame;

        public T Cache(Func<T> getter)
        {
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                return field = getter();
            }
            else
            {
                return field;
            }
        }
    }
}
