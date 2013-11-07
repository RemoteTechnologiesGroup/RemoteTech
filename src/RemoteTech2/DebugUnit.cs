using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class DebugUnit : MonoBehaviour
    {

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Dump();
            }
        }

        public void Dump()
        {
            var dump = new List<String>();
            dump.AddRange(DumpSatellites());
            dump.Add(Environment.NewLine);
            dump.AddRange(DumpAntennas());
            dump.Add(Environment.NewLine);
            dump.AddRange(DumpEdges());
            dump.Add(Environment.NewLine);
            dump.AddRange(DumpConnectionTests());
            dump.Add(Environment.NewLine);

            System.IO.File.WriteAllText(@"./rt_dump.txt", String.Join(Environment.NewLine, dump.ToArray()));
        }
        
        public String[] DumpSatellites()
        {
            var data = new List<String>();
            data.Add("NetworkManager contents: ");
            int i = 0;
            foreach (var s in RTCore.Instance.Network)
            {
                data.Add(String.Format("    {0}: {1}", i++, s));
            }

            return data.ToArray();
        }

        public String[] DumpAntennas()
        {
            var data = new List<String>();
            data.Add("AntennaManager contents: ");
            int i = 0;
            foreach (var a in RTCore.Instance.Antennas)
            {
                data.Add(String.Format("    {0}: {1}", i++, a));
            }

            return data.ToArray();
        }

        public String[] DumpEdges()
        {
            var data = new List<String>();
            data.Add("NetworkManager.Graph contents: ");
            int i = 0;
            foreach (var edge in RTCore.Instance.Network.Graph)
            {
                data.Add(String.Format("    {0}: {1}", i++, edge.Key));
                int j = 0;
                foreach (var target in edge.Value)
                {
                    data.Add(String.Format("        {0}: {1}", j++, target));
                }
            }

            return data.ToArray();
        }

        public String[] DumpConnectionTests()
        {
            var data = new List<String>();
            data.Add("Forced connection checks: ");
            int i = 0;
            foreach (var sat1 in RTCore.Instance.Network)
            {
                i++;
                int j = 0;
                foreach (var sat2 in RTCore.Instance.Network)
                {
                    j++;
                    if (sat1 == sat2) continue;
                    data.Add(String.Format("    {0} -> {1}: {2}", i, j, NetworkManager.GetLink(sat1, sat2)));
                }
            }

            return data.ToArray();
        }
    }
}
