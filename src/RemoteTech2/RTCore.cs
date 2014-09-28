using System;
using System.Linq;
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

        public FilterOverlay FilterOverlay { get; protected set; }
        public FocusOverlay FocusOverlay { get; protected set; }
        public TimeWarpDecorator TimeWarpDecorator { get; protected set; }

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
            Renderer = NetworkRenderer.CreateAndAttach();

            FilterOverlay = new FilterOverlay();
            FocusOverlay = new FocusOverlay();
            TimeWarpDecorator = new TimeWarpDecorator();

            FlightUIPatcher.Patch();

            RTLog.Notify("RTCore loaded successfully.");

            foreach (var vessel in FlightGlobals.Vessels)
            {
                Satellites.RegisterProto(vessel);
                Antennas.RegisterProtos(vessel);
            }
        }

        public void Update()
        {
            OnFrameUpdate.Invoke();

            if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.packed) return;
            var vs = Satellites[FlightGlobals.ActiveVessel];
            if (vs != null)
            {
                GetLocks();
                if (vs.HasLocalControl)
                {
                    ReleaseLocks();
                }
                else if (vs.FlightComputer != null && vs.FlightComputer.InputAllowed)
                {
                    foreach (KSPActionGroup ag in GetActivatedGroup())
                    {
                        vs.FlightComputer.Enqueue(ActionGroupCommand.WithGroup(ag));
                    }
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
            GUI.depth = 0;
            OnGuiUpdate.Invoke();

            Action windows = delegate { };
            foreach (var window in AbstractWindow.Windows.Values)
            {
                windows += window.Draw;
            }
            windows.Invoke();
        }

        public void OnDestroy()
        {
            if (FocusOverlay != null) FocusOverlay.Dispose();
            if (FilterOverlay != null) FilterOverlay.Dispose();
            if (FilterOverlay != null) FilterOverlay.Dispose();
            if (Renderer != null) Renderer.Detach();
            if (Network != null) Network.Dispose();
            if (Satellites != null) Satellites.Dispose();
            if (Antennas != null) Antennas.Dispose();

            Instance = null;
        }

        private void ReleaseLocks()
        {
            InputLockManager.RemoveControlLock("RTLockStaging");
            InputLockManager.RemoveControlLock("RTLockSAS");
            InputLockManager.RemoveControlLock("RTLockRCS");
            InputLockManager.RemoveControlLock("RTLockActions");
        }

        private void GetLocks()
        {
            InputLockManager.SetControlLock(ControlTypes.STAGING, "RTLockStaging");
            InputLockManager.SetControlLock(ControlTypes.SAS, "RTLockSAS");
            InputLockManager.SetControlLock(ControlTypes.RCS, "RTLockRCS");
            InputLockManager.SetControlLock(ControlTypes.GROUPS_ALL, "RTLockActions");
        }
        
        // Monstrosity that should fix the kOS control locks without modifications on their end.
        private IEnumerable<KSPActionGroup> GetActivatedGroup()
        {
            if (GameSettings.LAUNCH_STAGES.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.STAGING) == ControlTypes.STAGING && !l.Key.Equals("RTLockStaging"))) 
                    yield return KSPActionGroup.Stage;
            if (GameSettings.AbortActionGroup.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.GROUP_ABORT) == ControlTypes.GROUP_ABORT && !l.Key.Equals("RTLockActions"))) 
                    yield return KSPActionGroup.Abort;
            if (GameSettings.RCS_TOGGLE.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.RCS) == ControlTypes.RCS && !l.Key.Equals("RTLockRCS"))) 
                    yield return KSPActionGroup.RCS;
            if (GameSettings.SAS_TOGGLE.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.SAS) == ControlTypes.SAS && !l.Key.Equals("RTLockSAS"))) 
                    yield return KSPActionGroup.SAS;
            if (GameSettings.SAS_HOLD.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.SAS) == ControlTypes.SAS && !l.Key.Equals("RTLockSAS"))) 
                    yield return KSPActionGroup.SAS;
            if (GameSettings.SAS_HOLD.GetKeyUp())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.SAS) == ControlTypes.SAS && !l.Key.Equals("RTLockSAS"))) 
                    yield return KSPActionGroup.SAS;
            if (GameSettings.BRAKES.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.GROUP_BRAKES) == ControlTypes.GROUP_BRAKES && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Brakes;
            if (GameSettings.LANDING_GEAR.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.GROUP_GEARS) == ControlTypes.GROUP_GEARS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Gear;
            if (GameSettings.HEADLIGHT_TOGGLE.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.GROUP_LIGHTS) == ControlTypes.GROUP_LIGHTS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Light;
            if (GameSettings.CustomActionGroup1.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom01;
            if (GameSettings.CustomActionGroup2.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom02;
            if (GameSettings.CustomActionGroup3.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom03;
            if (GameSettings.CustomActionGroup4.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom04;
            if (GameSettings.CustomActionGroup5.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom05;
            if (GameSettings.CustomActionGroup6.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom06;
            if (GameSettings.CustomActionGroup7.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom07;
            if (GameSettings.CustomActionGroup8.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom08;
            if (GameSettings.CustomActionGroup9.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
                    yield return KSPActionGroup.Custom09;
            if (GameSettings.CustomActionGroup10.GetKeyDown())
                if (!InputLockManager.lockStack.Any(l => ((ControlTypes)l.Value & ControlTypes.CUSTOM_ACTION_GROUPS) == ControlTypes.CUSTOM_ACTION_GROUPS && !l.Key.Equals("RTLockActions")))
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
            FilterOverlay.OnEnterMapView();
            FocusOverlay.OnEnterMapView();
        }

        private new void OnDestroy()
        {
            FilterOverlay.OnExitMapView();
            FocusOverlay.OnExitMapView();
            base.OnDestroy();
        }
    }
}
