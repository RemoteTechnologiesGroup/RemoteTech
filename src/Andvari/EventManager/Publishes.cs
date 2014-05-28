using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Andvari
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Publishes
    {
        public String Name { get; private set; }

        public Publishes(String name)
        {
            this.Name = name;
        }
    }
}
