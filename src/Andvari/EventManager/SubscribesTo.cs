using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SubscribesTo
    {
        public String Name { get; private set; }

        public SubscribesTo(String name)
        {
            this.Name = name;
        }
    }
}
