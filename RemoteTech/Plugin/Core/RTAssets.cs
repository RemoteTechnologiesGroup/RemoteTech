using System;
using UnityEngine;

namespace RemoteTech {
    public class RTAssets {

        public Texture2D ImgSat { get; private set; }

        RTCore mCore;

        public RTAssets(RTCore core) {
            mCore = core;
            Load();
        }

        public void Load() {
            try {
                ImgSat = new Texture2D(32, 32, TextureFormat.ARGB32, false);
                ImgSat.LoadImage(KSP.IO.File.ReadAllBytes<RTCore>("img_sat.png"));
            } catch (Exception e) {
                RTUtil.Log(e.ToString());
                ImgSat = null;
            }
            RTUtil.Log("RTAssets loaded.");
        }
    }
}
