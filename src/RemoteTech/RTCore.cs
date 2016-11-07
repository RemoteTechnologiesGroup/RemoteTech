using System;
using System.Linq;
using System.Collections.Generic;
using RemoteTech.FlightComputer.Commands;
using RemoteTech.UI;
using UnityEngine;

namespace RemoteTech
{
    /// <summary>
    /// Main base class of RemoteTech. It is called by various inheriting classes: 
    ///  * RTCoreFlight (Flight scene)
    ///  * RTCoreTracking (Tracking station scene)
    ///  * RTMainMenu (Main menu scene)
    /// </summary>
    public abstract class RTCore : MonoBehaviour
    {
        /// <summary>
        /// Main class instance.
        /// </summary>
        public static RTCore Instance { get; protected set; }

        /// <summary>
        /// RemoteTech satellites manager.
        /// </summary>
        public SatelliteManager Satellites { get; protected set; }

        /// <summary>
        /// RemoteTech antennas manager.
        /// </summary>
        public AntennaManager Antennas { get; protected set; }

        /// <summary>
        /// RemotTech network manager. 
        /// </summary>
        public NetworkManager Network { get; protected set; }

        /// <summary>
        /// RemoteTech UI network renderer.
        /// </summary>
        public NetworkRenderer Renderer { get; protected set; }

        /*
         * Add-ons
         */

        /// <summary>
        /// Kerbal Alarm Clock Add-on.
        /// </summary>
        public AddOns.KerbalAlarmClockAddon KacAddon { get; protected set; }

        /*
         * Events
         */

        /// <summary>
        /// Methods can register to this event to be called during the Update() method of the Unity engine (Game Logic engine phase).
        /// </summary>
        public event Action OnFrameUpdate = delegate { };
        /// <summary>
        /// Methods can register to this event to be called during the FixedUpdate() method of the Unity engine (Physics engine phase).
        /// </summary>
        public event Action OnPhysicsUpdate = delegate { };
        /// <summary>
        /// Methods can register to this event to be called during the OnGUI() method of the Unity engine (GUI Rendering engine phase).
        /// </summary>
        public event Action OnGuiUpdate = delegate { };

        /*
         * UI Overlays
         */

        /// <summary>
        /// UI overlay for the Tracking station or Flight map view (draw and handle buttons in the bottom right corner).
        /// </summary>
        public FilterOverlay FilterOverlay { get; protected set; }
        /// <summary>
        /// UI overlay for the "focus view" in the Tracking station scene.
        /// </summary>
        public FocusOverlay FocusOverlay { get; protected set; }
        /// <summary>
        /// UI overlay to add a new button to maneuver nodes.
        /// </summary>
        public ManeuverNodeOverlay ManeuverNodeOverlay { get; protected set; }
        /// <summary>
        /// UI overlay used to display and handle the status quadrant (time delay) and the flight computer button.
        /// </summary>
        public TimeWarpDecorator TimeWarpDecorator { get; protected set; }

        // New for handling the F2 GUI Hiding
        private bool _guiVisible = true;


        /// <summary>
        /// Called by Unity engine during initialization phase.
        /// Only ever called once.
        /// </summary>
        public void Start()
        {
            // Destroy the Core instance if != null or if RemoteTech is disabled
            if (Instance != null || !RTSettings.Instance.RemoteTechEnabled)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            // enable or disable KSP CommNet depending on settings.
            HighLogic.fetch.currentGame.Parameters.Difficulty.EnableCommNet = RTSettings.Instance.CommNetEnabled;

            // add-ons
            KacAddon = new AddOns.KerbalAlarmClockAddon();

            // managers
            Satellites = new SatelliteManager();
            Antennas = new AntennaManager();
            Network = new NetworkManager();
            Renderer = NetworkRenderer.CreateAndAttach();

            // overlays
            FilterOverlay = new FilterOverlay();
            FocusOverlay = new FocusOverlay();
            TimeWarpDecorator = new TimeWarpDecorator();

            // Handling new F2 GUI Hiding
            GameEvents.onShowUI.Add(UiOn);
            GameEvents.onHideUI.Add(UiOff);

            RTLog.Notify("RTCore {0} loaded successfully.", RTUtil.Version);

            // register vessels and antennas
            foreach (var vessel in FlightGlobals.Vessels)
            {
                // do not try to register vessel types that have no chance of being RT controlled.
                // includes: debris, SpaceObject, unknown, EVA and flag
                if ((vessel.vesselType <= VesselType.Unknown) || (vessel.vesselType >= VesselType.EVA))
                    continue;

                Satellites.RegisterProto(vessel);
                Antennas.RegisterProtos(vessel);
            }
        }

        /// <summary>
        /// F2 GUI Hiding functionality; called when the UI must be displayed.
        /// </summary>
        public void UiOn()
        {
            _guiVisible = true;
        }

        /// <summary>
        /// F2 GUI Hiding functionality; called when the UI must be hidden.
        /// </summary>
        public void UiOff()
        {
            _guiVisible = false;
        }

        /// <summary>
        /// Called by the Unity engine during the game logic phase.
        /// This function is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
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

        /// <summary>
        /// Prevent duplicate calls for the OnFrameUpdate event.
        /// </summary>
        /// <param name="action">The action to be added to the OnFrameUpdate event.</param>
        public void AddOnceOnFrameUpdate(Action action)
        {
            if (!Instance.OnFrameUpdate.GetInvocationList().Contains(action))
                Instance.OnFrameUpdate += action;
        }

        /// <summary>
        /// Called by the Unity engine during the Physics phase.
        /// Note that FixedUpdate() is called before the internal engine physics update. This function is often called more frequently than Update().
        /// </summary>
        public void FixedUpdate()
        {
            OnPhysicsUpdate.Invoke();
        }

        /// <summary>
        /// Called by the Unity engine during the GUI rendering phase.
        /// Note that OnGUI() is called multiple times per frame in response to GUI events.
        /// The Layout and Repaint events are processed first, followed by a Layout and keyboard/mouse event for each input event.
        /// </summary>
        public void OnGUI()
        {
            if (!_guiVisible)
                return;

            if (TimeWarpDecorator != null)
                TimeWarpDecorator.Draw();

            GUI.depth = 0;
            OnGuiUpdate.Invoke();

            Action windows = delegate { };
            foreach (var window in AbstractWindow.Windows.Values)
            {
                windows += window.Draw;
            }
            windows.Invoke();
        }

        /// <summary>
        /// Called by the Unity engine during the Decommissioning phase of the Engine.
        /// This is used to clean up everything before quiting.
        /// </summary>
        public void OnDestroy()
        {
            if (FocusOverlay != null) FocusOverlay.Dispose();
            if (ManeuverNodeOverlay != null) ManeuverNodeOverlay.Dispose();
            if (FilterOverlay != null) FilterOverlay.Dispose();
            if (Renderer != null) Renderer.Detach();
            if (Network != null) Network.Dispose();
            if (Satellites != null) Satellites.Dispose();
            if (Antennas != null) Antennas.Dispose();

            // Remove GUI stuff
            GameEvents.onShowUI.Remove(UiOn);
            GameEvents.onHideUI.Remove(UiOff);

			// add-ons
            if (KacAddon != null) KacAddon = null;

            Instance = null;
        }

        /// <summary>
        /// Release RemoteTech UI locks (enable usage of UI buttons).
        /// </summary>
        private static void ReleaseLocks()
        {
            InputLockManager.RemoveControlLock("RTLockStaging");
            InputLockManager.RemoveControlLock("RTLockSAS");
            InputLockManager.RemoveControlLock("RTLockRCS");
            InputLockManager.RemoveControlLock("RTLockActions");
        }

        /// <summary>
        /// Acquire RemoteTech UI locks (disable usage of UI buttons).
        /// </summary>
        private static void GetLocks()
        {
            InputLockManager.SetControlLock(ControlTypes.STAGING, "RTLockStaging");
            InputLockManager.SetControlLock(ControlTypes.SAS, "RTLockSAS");
            InputLockManager.SetControlLock(ControlTypes.RCS, "RTLockRCS");
            InputLockManager.SetControlLock(ControlTypes.GROUPS_ALL, "RTLockActions");
        }
        
        // Monstrosity that should fix the kOS control locks without modifications on their end.
        private static IEnumerable<KSPActionGroup> GetActivatedGroup()
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

    /// <summary>
    /// Main class, instantiated during Flight scene.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RTCoreFlight : RTCore
    {
        public new void Start()
        {
            base.Start();
            if (Instance == null)
                return;

            FlightUIPatcher.Patch();
            ManeuverNodeOverlay = new ManeuverNodeOverlay();
            ManeuverNodeOverlay.OnEnterMapView();
        }

        private new void OnDestroy()
        {
            if (Instance != null)
            {
                ManeuverNodeOverlay.OnExitMapView();
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Main class, instantiated during Tracking station scene.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class RTCoreTracking : RTCore
    {
        public new void Start()
        {
            base.Start();
            if (Instance == null)
                return;

            FilterOverlay.OnEnterMapView();
            FocusOverlay.OnEnterMapView();
        }

        private new void OnDestroy()
        {
            if (Instance != null)
            {
                FilterOverlay.OnExitMapView();
                FocusOverlay.OnExitMapView();
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Main class, instantiated during Main menu scene.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class RTMainMenu : MonoBehaviour
    {
        public void Start()
        {
            // Set the loaded trigger to false, this we will load a new
            // settings after selecting a save game. This is necessary
            // for switching between saves without shutting down the KSP
            // instance.
            RTSettings.Instance.SettingsLoaded = false;
        }
    }
}
