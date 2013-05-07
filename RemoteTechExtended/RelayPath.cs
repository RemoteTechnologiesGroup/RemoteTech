using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class RelayPath
    {
        //nodes through which the signal is relayed
        public List<RelayNode> nodes = new List<RelayNode>();

        public RelayPath(List<RelayNode> nodes)
        {
            this.nodes = nodes;
        }

        public double Length
        {
            get
            {
                double length = 0;
                for (int i = 1; i < nodes.Count; i++)
                {
                    length += (nodes[i].Position - nodes[i - 1].Position).magnitude;
                }
                return length;
            }
        }

        public double ControlDelay
        {
            get
            {
                return (2 * this.Length / RTGlobals.speedOfLight);
            }
        }


        public override String ToString()
        {
            String ret;
            if (nodes.Count > 0) ret = nodes[nodes.Count - 1].ToString();
            else ret = "empty path???????";

            for (int i = nodes.Count - 2; i >= 0; i--)
            {
                ret += " → " + nodes[i].ToString();
            }
            return ret;
        }
    }
}
