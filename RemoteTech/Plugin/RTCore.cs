using UnityEngine;

namespace RemoteTech {
    public abstract class RTCore : MonoBehaviour {
        public static RTCore Instance { get; protected set; }

        public Settings Settings { get; protected set; }
        public NetworkManager Network { get; protected set; }
        public SatelliteManager Satellites { get; protected set; }
        public AntennaManager Antennas { get; protected set; }
        public GuiManager Gui { get; protected set; }
        public NetworkRenderer Renderer { get; protected set; }

        public void GetLocks() {
            if (!InputLockManager.IsLocked(ControlTypes.STAGING)) {
                InputLockManager.SetControlLock(ControlTypes.STAGING, "LockStaging");
            }
            if (!InputLockManager.IsLocked(ControlTypes.SAS)) {
                InputLockManager.SetControlLock(ControlTypes.SAS, "LockSAS");
            }
            if (!InputLockManager.IsLocked(ControlTypes.RCS)) {
                InputLockManager.SetControlLock(ControlTypes.RCS, "LockRCS");
            }
            if (!InputLockManager.IsLocked(ControlTypes.GROUPS_ALL)) {
                InputLockManager.SetControlLock(ControlTypes.GROUPS_ALL, "LockActions");
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal class RTCoreFlight : RTCore {
        private void Init() {
            Instance = GameObject.Find("RTCoreFlight").GetComponent<RTCoreFlight>();

            Settings = new Settings(this);
            Satellites = new SatelliteManager(this);
            Antennas = new AntennaManager(this);
            Network = new NetworkManager(this);
            Gui = new GuiManager(this);
            Renderer = NetworkRenderer.AttachToMapView(this);

            RTUtil.Log("RTCore loaded.");
        }

        public void Start() {
            Init();
            foreach (Vessel v in FlightGlobals.Vessels) {
                Satellites.RegisterProtoFor(v);
                Antennas.RegisterProtoFor(v);
            }
        }

        public void FixedUpdate() {
            StartCoroutine(Network.Tick());
        }

        public void OnGUI() {
            Gui.Draw();
        }

        private void OnDestroy() {
            Gui.Dispose();
            Renderer.Detach();
            Network.Dispose();
            Satellites.Dispose();
            Antennas.Dispose();
            InputLockManager.RemoveControlLock("LockStaging");
            InputLockManager.RemoveControlLock("LockSAS");
            InputLockManager.RemoveControlLock("LockRCS");
            InputLockManager.RemoveControlLock("LockActions");
            Instance = null;
        }
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    internal class RTCoreTracking : RTCore {
        private void Init() {
            Instance = GameObject.Find("RTCoreTracking").GetComponent<RTCoreTracking>();

            Settings = new Settings(this);
            Satellites = new SatelliteManager(this);
            Antennas = new AntennaManager(this);
            Network = new NetworkManager(this);
            Gui = new GuiManager(this);
            Renderer = NetworkRenderer.AttachToMapView(this);
        }

        public void Start() {
            Init();
            foreach (Vessel v in FlightGlobals.Vessels) {
                Satellites.RegisterProtoFor(v);
                Antennas.RegisterProtoFor(v);
            }
            (new MapViewSatelliteWindow(false)).Show();
        }

        public void FixedUpdate() {
            StartCoroutine(Network.Tick());
        }

        public void OnGUI() {
            Gui.Draw();
        }

        private void OnDestroy() {
            Gui.Dispose();
            Renderer.Detach();
            Network.Dispose();
            Satellites.Dispose();
            Antennas.Dispose();
            Instance = null;
        }
    }
}
