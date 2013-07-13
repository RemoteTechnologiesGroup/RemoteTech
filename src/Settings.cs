using System;
using KSP.IO;
using UnityEngine;

namespace RemoteTech {
    public class Settings {
        public bool DebugAlwaysConnected = false;
        public float DebugOffsetDelay = 1.0f;

        public Texture2D IconCalc;

        private readonly RTCore mCore;

        public Settings(RTCore core) {
            mCore = core;
            Load();
        }

        public void Load() {
            RTUtil.LoadImage(out IconCalc, "calc.png");

            ConfigNode root = ConfigNode.Load("RemoteTech.cfg");
            ConfigNode rt;
            if (root != null && root.HasNode("REMOTE_TECH")) {
                rt = root.GetNode("REMOTE_TECH");
            } else {
                rt = new ConfigNode();
            }
            mCore.Network.Load(rt);
            mCore.Gui.Load(rt);
            mCore.Renderer.Load(rt);
        }

        public void Save() {
            ConfigNode root = new ConfigNode();
            ConfigNode rt = new ConfigNode("REMOTE_TECH");
            root.AddNode(rt);

            mCore.Network.Save(rt);
            mCore.Gui.Save(rt);
            mCore.Renderer.Save(rt);

            root.Save("RemoteTech.cfg", " RemoteTech2 configuration file.");
        }
    }
}
