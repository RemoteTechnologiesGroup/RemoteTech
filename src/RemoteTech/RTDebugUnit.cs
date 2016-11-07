using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class RTDebugUnit : MonoBehaviour
    {
        private UI.DebugWindow debugWindow;

        public void Start()
        {
            #if DEBUG
            debugWindow = new UI.DebugWindow();
            #endif
        }

        public void Update()
        {
            if ((Input.GetKeyDown(KeyCode.F11) || Input.GetKeyDown(KeyCode.F12)) && (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium))
            {
                if (Input.GetKeyDown(KeyCode.F11))
                    Dump();
                else
                    debugWindow?.toggleWindow();
            }
        }

        public void Dump()
        {
            var dump = new List<string>();
            dump.AddRange(DumpSatellites());
            dump.Add(Environment.NewLine);
            dump.AddRange(DumpAntennas());
            dump.Add(Environment.NewLine);
            dump.AddRange(DumpEdges());
            dump.Add(Environment.NewLine);
            dump.AddRange(DumpConnectionTests());
            dump.Add(Environment.NewLine);

            System.IO.File.WriteAllText(@"./rt_dump.txt", string.Join(Environment.NewLine, dump.ToArray()));
        }
        
        public string[] DumpSatellites()
        {
            var data = new List<string> {"NetworkManager contents: "};
            var i = 0;
            foreach (var satellite in RTCore.Instance.Network)
            {
                data.Add($"    {i++}: {satellite}");
            }

            return data.ToArray();
        }

        public string[] DumpAntennas()
        {
            var data = new List<string> {"AntennaManager contents: "};
            var i = 0;
            foreach (var antenna in RTCore.Instance.Antennas)
            {
                data.Add($"    {i++}: {antenna}");
            }

            return data.ToArray();
        }

        public string[] DumpEdges()
        {
            var data = new List<string> {"NetworkManager.Graph contents: "};
            var i = 0;
            foreach (var edge in RTCore.Instance.Network.Graph)
            {
                data.Add($"    {i++}: {edge.Key}");
                var j = 0;
                foreach (var target in edge.Value)
                {
                    data.Add($"        {j++}: {target}");
                }
            }

            return data.ToArray();
        }

        public string[] DumpConnectionTests()
        {
            var data = new List<string> {"Forced connection checks: "};
            var i = 0;
            foreach (var sat1 in RTCore.Instance.Network)
            {
                var j = 0;
                foreach (var sat2 in RTCore.Instance.Network)
                {
                    if (sat1 == sat2) continue;
                    data.Add($"    {i} -> {j}: {NetworkManager.GetLink(sat1, sat2)}");
                    j++;
                }
                i++;
            }

            return data.ToArray();
        }
    }
}
