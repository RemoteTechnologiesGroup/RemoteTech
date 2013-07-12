using UnityEngine;

namespace RemoteTech {
    public delegate void RTOnUpdate();
    public delegate void RTOnFixedUpdate();
    public delegate void RTOnGui();
    public abstract class RTCore : MonoBehaviour {
        public static RTCore Instance { get; protected set; }

        public Settings Settings { get; protected set; }
        public NetworkManager Network { get; protected set; }
        public SatelliteManager Satellites { get; protected set; }
        public AntennaManager Antennas { get; protected set; }
        public GuiManager Gui { get; protected set; }
        public NetworkRenderer Renderer { get; protected set; }

        public bool IsTrackingStation { get; protected set; }

        public event RTOnUpdate FrameUpdated;
        public event RTOnFixedUpdate PhysicsUpdated;
        public event RTOnGui GuiUpdated;

        public void Start() {
            if(Instance != null) {
                Destroy(this);
                return;
            }

            Instance = this;

            Satellites = new SatelliteManager(this);
            Antennas = new AntennaManager(this);
            Network = new NetworkManager(this);
            Gui = new GuiManager(this);
            Renderer = NetworkRenderer.AttachToMapView(this);
            Settings = new Settings(this);

            RTUtil.Log("RTCore loaded.");

            foreach (Vessel v in FlightGlobals.Vessels) {
                Satellites.RegisterProto(v);
                Antennas.RegisterProtoFor(v);
            }
        }

        public void Update() {
            if (FrameUpdated != null) {
                FrameUpdated.Invoke();
            }
        }

        public void FixedUpdate() {
            if (PhysicsUpdated != null) {
                PhysicsUpdated.Invoke();
            }
        }

        public void OnGUI() {
            if (GuiUpdated != null) {
                GuiUpdated.Invoke();
            }
        }

        private void OnDestroy() {
            Settings.Save();
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
    public class RTCoreFlight : RTCore {
        public new void Start() {
            base.Start();
            IsTrackingStation = false;
        }
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class RTCoreTracking : RTCore {
        public new void Start() {
            base.Start();
            IsTrackingStation = true;
        }
    }
}
