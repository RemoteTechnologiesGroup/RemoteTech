using System;
using KSP.IO;
using UnityEngine;

namespace RemoteTech {
    public class Settings {
        public bool DebugAlwaysConnected = false;
        public float DebugOffsetDelay = 1.0f;

        public Texture2D IconBook = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        public Texture2D IconCalc = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        public Texture2D IconConf = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        public Texture2D IconConn = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        public Texture2D IconId = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        public Texture2D IconSat = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        public Texture2D IconMark = new Texture2D(32, 32, TextureFormat.ARGB32, false);

        private readonly RTCore mCore;

        public Settings(RTCore core) {
            mCore = core;
            Load();
        }

        public void Load() {
            Load(ref IconBook, "book.png");
            Load(ref IconCalc, "calc.png");
            Load(ref IconConf, "conf.png");
            Load(ref IconConn, "conn.png");
            Load(ref IconId, "id.png");
            Load(ref IconSat, "sat.png");
            Load(ref IconMark, "mark.png");
        }

        public void Save() {
            ConfigNode root = new ConfigNode("REMOTE_TECH");
            ConfigNode renderer = new ConfigNode("RENDERER");
            ConfigNode network = new ConfigNode("NETWORK");
            ConfigNode gui = new ConfigNode("GUI");
            ConfigNode debug = new ConfigNode("DEBUG");

            root.AddNode(renderer);
            root.AddNode(network);
            root.AddNode(gui);
            root.AddNode(debug);

            mCore.Network.Save(network);
            mCore.Gui.Save(gui);
            mCore.Renderer.Save(renderer);
            mCore.Network.Save(network);

            root.Save("RemoteTech.cfg");
        }

        private void Load(ref Texture2D texture, String fileName) {
            try {
                texture.LoadImage(KSP.IO.File.ReadAllBytes<RTCore>(fileName));
            } catch (IOException) {
                texture = null;
            }
        }
    }
}
