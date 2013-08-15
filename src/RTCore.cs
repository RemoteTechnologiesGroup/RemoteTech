using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech {
    public delegate void OnTick();
    public abstract class RTCore : MonoBehaviour {
        public static RTCore Instance { get; protected set; }

        public Settings Settings { get; protected set; }
        public NetworkManager Network { get; protected set; }
        public SatelliteManager Satellites { get; protected set; }
        public AntennaManager Antennas { get; protected set; }
        public GuiManager Gui { get; protected set; }
        public NetworkRenderer Renderer { get; protected set; }
        public DebugUnit Debug { get; protected set; }

        public bool IsTrackingStation { get; protected set; }

        public event OnTick FrameUpdated;
        public event OnTick PhysicsUpdated;
        public event OnTick GuiUpdated;

        public void Start() {
            if(Instance != null) {
                Destroy(this);
                return;
            }

            Instance = this;

            Satellites = new SatelliteManager(this);
            Antennas = new AntennaManager(this);
            Network = new NetworkManager(this);
            Gui = new GuiManager();
            Renderer = NetworkRenderer.AttachToMapView(this);
            Settings = new Settings(this);
            Debug = new DebugUnit(this);

            RTUtil.Log("RTCore loaded.");

            foreach (var v in FlightGlobals.Vessels) {
                Satellites.RegisterProto(v);
                Antennas.RegisterProtos(v);
            }
        }

        public void Update() {
            if (FlightGlobals.ActiveVessel != null) {
                VesselSatellite vs = Satellites[FlightGlobals.ActiveVessel];
                if (vs != null) {
                    GetLocks();
                    if (vs.Master.FlightComputer != null && 
                                vs.Master.FlightComputer.InputAllowed) {
                        foreach (KSPActionGroup g in GetActivatedGroup()) {
                            vs.Master.FlightComputer.Enqueue(ActionGroupCommand.Group(g));
                        }
                    }
                } else {
                    ReleaseLocks();
                }
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
            Debug.Dispose();

            Instance = null;
        }

        private void ReleaseLocks() {
            InputLockManager.RemoveControlLock("LockStaging");
            InputLockManager.RemoveControlLock("LockSAS");
            InputLockManager.RemoveControlLock("LockRCS");
            InputLockManager.RemoveControlLock("LockActions");
        }

        private void GetLocks() {
            InputLockManager.SetControlLock(ControlTypes.STAGING, "LockStaging");
            InputLockManager.SetControlLock(ControlTypes.SAS, "LockSAS");
            InputLockManager.SetControlLock(ControlTypes.RCS, "LockRCS");
            InputLockManager.SetControlLock(ControlTypes.GROUPS_ALL, "LockActions");
        }

        private IEnumerable<KSPActionGroup> GetActivatedGroup() {
            if (GameSettings.LAUNCH_STAGES.GetKeyDown())
                yield return KSPActionGroup.Stage;
            if (GameSettings.AbortActionGroup.GetKeyDown())
                yield return KSPActionGroup.Abort;
            if (GameSettings.RCS_TOGGLE.GetKeyDown())
                yield return KSPActionGroup.RCS;
            if (GameSettings.SAS_TOGGLE.GetKeyDown())
                yield return KSPActionGroup.SAS;
            if (GameSettings.SAS_HOLD.GetKeyDown())
                yield return KSPActionGroup.SAS;
            if (GameSettings.SAS_HOLD.GetKeyUp())
                yield return KSPActionGroup.SAS;
            if (GameSettings.BRAKES.GetKeyDown())
                yield return KSPActionGroup.Brakes;
            if (GameSettings.LANDING_GEAR.GetKeyDown())
                yield return KSPActionGroup.Gear;
            if (GameSettings.HEADLIGHT_TOGGLE.GetKeyDown())
                yield return KSPActionGroup.Light;
            if (GameSettings.CustomActionGroup1.GetKeyDown())
                yield return KSPActionGroup.Custom01;
            if (GameSettings.CustomActionGroup2.GetKeyDown())
                yield return KSPActionGroup.Custom02;
            if (GameSettings.CustomActionGroup3.GetKeyDown())
                yield return KSPActionGroup.Custom03;
            if (GameSettings.CustomActionGroup4.GetKeyDown())
                yield return KSPActionGroup.Custom04;
            if (GameSettings.CustomActionGroup5.GetKeyDown())
                yield return KSPActionGroup.Custom05;
            if (GameSettings.CustomActionGroup6.GetKeyDown())
                yield return KSPActionGroup.Custom06;
            if (GameSettings.CustomActionGroup7.GetKeyDown())
                yield return KSPActionGroup.Custom07;
            if (GameSettings.CustomActionGroup8.GetKeyDown())
                yield return KSPActionGroup.Custom08;
            if (GameSettings.CustomActionGroup9.GetKeyDown())
                yield return KSPActionGroup.Custom09;
            if (GameSettings.CustomActionGroup10.GetKeyDown())
                yield return KSPActionGroup.Custom10;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RTCoreFlight : RTCore {
        public new void Start() {
            IsTrackingStation = false;
            base.Start();
        }
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class RTCoreTracking : RTCore {
        public new void Start() {
            IsTrackingStation = true;
            base.Start();
        }
    }
}
