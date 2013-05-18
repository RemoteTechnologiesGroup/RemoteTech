using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public sealed class RTCore {

        private static readonly RTCore mSingleton = new RTCore();

        public static RTCore Instance { get { return mSingleton; } }

        public RTSettings Settings { get; private set; }

        RTCore() {
            Settings = new RTSettings();
        }

        public void Load() {
            Settings.Load();
        }

        public void Save() {
            Settings.Save();
        }

    }

    class RemoteTechCoreHook : PartModule {

        public override void OnLoad() {
            RTCore.Instance.Load();
        }

        public override void OnSave(ConfigNode node) {
            RTCore.Instance.Save();
        }

        public override void OnAwake() {
            if(RTCore.Instance != null) {
                RTUtil.Log("RTCore initialized.");
            }
        }

        public override void OnUpdate() {

        }
    }
}
