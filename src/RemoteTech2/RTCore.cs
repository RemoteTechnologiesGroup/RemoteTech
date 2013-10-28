using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public abstract class RTCore : MonoBehaviour
    {
        public static RTCore Instance { get; protected set; }

        public SatelliteManager Satellites { get; protected set; }
        public AntennaManager Antennas { get; protected set; }
        public NetworkManager Network { get; protected set; }
        public NetworkRenderer Renderer { get; protected set; }

        public event Action OnFrameUpdate = delegate { };
        public event Action OnPhysicsUpdate = delegate { };
        public event Action OnGuiUpdate = delegate { };

        private MapViewConfigFragment mConfig;
        private TimeQuadrantPatcher mTimePatcher;

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            Satellites = new SatelliteManager();
            Antennas = new AntennaManager();
            Network = new NetworkManager();
            Renderer = NetworkRenderer.AttachToMapView();

            mConfig = new MapViewConfigFragment();
            mTimePatcher = new TimeQuadrantPatcher();

            mTimePatcher.Patch();
            FlightUIPatcher.Patch();

            RTUtil.Log("RTCore loaded successfully.");

            foreach (var vessel in FlightGlobals.Vessels)
            {
                Satellites.RegisterProto(vessel);
                Antennas.RegisterProtos(vessel);
            }

        }

        public void Update()
        {
            if (FlightGlobals.ActiveVessel == null) return;
            if (FlightGlobals.ActiveVessel.packed) return;
            var vs = Satellites[FlightGlobals.ActiveVessel];
            if (vs != null)
            {
                GetLocks();
                if (vs.FlightComputer != null && vs.FlightComputer.InputAllowed)
                {
                    foreach (KSPActionGroup ag in GetActivatedGroup())
                    {
                        vs.FlightComputer.Enqueue(ActionGroupCommand.Group(ag));
                    }
                }
                else if (vs.HasLocalControl)
                {
                    ReleaseLocks();
                }
            }
            else
            {
                ReleaseLocks();
            }
        }

        public void FixedUpdate()
        {
            OnPhysicsUpdate.Invoke();
        }

        public void OnGUI()
        {
            OnGuiUpdate.Invoke();

            if (MapView.MapIsEnabled)
            {
                mConfig.Draw();
            }
        }

        private void OnDestroy()
        {
            mTimePatcher.Undo();
            mConfig.Dispose();
            Renderer.Detach();
            Network.Dispose();
            Satellites.Dispose();
            Antennas.Dispose();

            Instance = null;
        }

        private void ReleaseLocks()
        {
            InputLockManager.RemoveControlLock("LockStaging");
            InputLockManager.RemoveControlLock("LockSAS");
            InputLockManager.RemoveControlLock("LockRCS");
            InputLockManager.RemoveControlLock("LockActions");
        }

        private void GetLocks()
        {
            InputLockManager.SetControlLock(ControlTypes.STAGING, "LockStaging");
            InputLockManager.SetControlLock(ControlTypes.SAS, "LockSAS");
            InputLockManager.SetControlLock(ControlTypes.RCS, "LockRCS");
            InputLockManager.SetControlLock(ControlTypes.GROUPS_ALL, "LockActions");
        }

        private IEnumerable<KSPActionGroup> GetActivatedGroup()
        {
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
    public class RTCoreFlight : RTCore
    {
        public new void Start()
        {
            base.Start();
        }
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class RTCoreTracking : RTCore
    {
        public new void Start()
        {
            base.Start();
        }
    }
}
