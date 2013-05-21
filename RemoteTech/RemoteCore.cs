using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{


    public class RemoteCore
    {
        public FlightCtrlStateBuffer delayedBuffer = new FlightCtrlStateBuffer();
        public FlightComputer computer;
        public FlightComputerGUI flightComputerGUI;
        int ticks = 0;
        public SatSettings settings;
        bool MechJeb = false;
        float EnergyDrain = 0;
        public bool localControl = false;
        const int WINDOW_ID = 72398;

        public Vessel vessel;
        public RelayNode Rnode;
        public RelayPath path;
        public List<GUIcontainer> otherGUI = new List<GUIcontainer>();

        public bool InContact
        {
            get
            {
                return path != null;
            }
        }

        GameObject obj = new GameObject("Line");

        private LineRenderer line = null;
        PlanetariumCamera planetariumCamera = null;

        float lastThrottle = 0;
        float LastAxisThrottle = 0;


        public RemoteCore(Vessel v, float energyDrain)
        {
            if (v == null) return;
            vessel = v;
            this.EnergyDrain = energyDrain;

            settings = new SatSettings(this);
            computer = new FlightComputer(this);
            flightComputerGUI = new FlightComputerGUI(this);

            Rnode = new RelayNode(vessel);

            try
            {
                vessel.OnFlyByWire -= new FlightInputCallback(this.drive);
            }
            catch { }
            try
            {
                vessel.OnFlyByWire += new FlightInputCallback(this.drive);
            }
            catch { }

            try
            {
                path = RTGlobals.network.GetCommandPath(Rnode);
            }
            catch
            {
                RTGlobals.network = new RelayNetwork();
                this.path = RTGlobals.network.GetCommandPath(Rnode);
            }

            UpdateOtherModules();

            planetariumCamera = (PlanetariumCamera)GameObject.FindObjectOfType(typeof(PlanetariumCamera));

            obj = new GameObject("Line");

            line = null;

            obj.layer = 9;
            line = obj.AddComponent<LineRenderer>();
            line.transform.parent = vessel.rootPart.transform;
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Particles/Additive"));
            line.SetColors(Color.blue, Color.blue);
            line.SetWidth(0, 0);


            localControl = vessel.GetCrewCount() > 0 || MechJeb;
        }

        public bool Wheel = false;
        public void UpdateOtherModules()
        {
            //this checks if there are any MechJebCores or wheels on the vessel.

            bool MechJebTMP = false, WheelTMP = false;

            foreach (Part p in vessel.parts)
            {
                if (p.Modules.Contains("MechJebCore"))
                    MechJebTMP = true;
                if (p.Modules.Contains("ModuleWheel"))
                    WheelTMP = true;

                if (MechJebTMP && WheelTMP) break;
            }


            MechJeb = MechJebTMP;
            Wheel = WheelTMP;
        }

        public bool powered = true;
        void RequestPower()
        {
            if (TimeWarp.deltaTime == 0) return;
            float amount = vessel.rootPart.RequestResource("ElectricCharge", EnergyDrain * TimeWarp.deltaTime);
            powered = amount != 0;
        }

        public bool RCSoverride = false;
        Queue<TriggerState> states = new Queue<TriggerState>();
        public void UpdateTriggers()
        {
            if (localControl || (InContact && powered))
            {
                if (GameSettings.SAS_TOGGLE.GetKeyDown() || GameSettings.SAS_HOLD.GetKeyDown() || GameSettings.SAS_HOLD.GetKeyUp())
                    flightComputerGUI.attitudeButtons[0].on = !flightComputerGUI.attitudeButtons[0].on;


                TriggerState state = RTUtils.triggerstate;

                if (state.ActionGroup != KSPActionGroup.None)
                {
                    if (localControl)
                        applyTrigger(state.ActionGroup);
                    else
                    {
                        state.ActTime = Planetarium.GetUniversalTime() + path.ControlDelay;
                        states.Enqueue(state);
                    }
                }

                if (states.Count > 0 && states.Peek().ActTime <= Planetarium.GetUniversalTime())
                    applyTrigger(states.Dequeue().ActionGroup);
            }
        }


        void applyTrigger(KSPActionGroup ActionGroup)
        {
            if (ActionGroup == KSPActionGroup.Stage)
            {
                if (!vessel.HoldPhysics && !DockingMode)
                {
                    Staging.ActivateNextStage();
                    vessel.ActionGroups.ToggleGroup(ActionGroup);
                }
            }
            else if (ActionGroup == KSPActionGroup.RCS)
            {
                RCSoverride = !RCSoverride;
                vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, RCSoverride);
            }
            else
            {
                vessel.ActionGroups.ToggleGroup(ActionGroup);

                if (ActionGroup == KSPActionGroup.Gear)
                {
                    foreach (HLandingLeg l in vessel.parts.OfType<HLandingLeg>())
                    {
                        if (vessel.ActionGroups.groups[1])
                            l.RaiseAction(new KSPActionParam(KSPActionGroup.Gear, KSPActionType.Activate));
                        else
                            l.LowerAction(new KSPActionParam(KSPActionGroup.Gear, KSPActionType.Activate));
                    }
                }
            }

            if (FlightInputHandler.fetch.rcslock != RCSoverride)
                FlightInputHandler.fetch.rcslock = RCSoverride;
        }

        bool DockingMode
        {
            get
            {
                return !(FlightUIModeController.Instance.dockingButton.currentState == 0);
            }
        }



        void UpdateOtherGUI()
        {
            if (otherGUI.Count == 0) return;

            if (RTGlobals.extPack && RTGlobals.Manager != null && RTGlobals.Manager.distantLandedPartPackThreshold == 350)
            {
                RTGlobals.Manager.distantLandedPartPackThreshold = RTGlobals.Manager.distantPartPackThreshold = 2250;
                RTGlobals.Manager.distantLandedPartUnpackThreshold = RTGlobals.Manager.distantPartUnpackThreshold = 2000;
                return;
            }


            GUIcontainer remove = null;
            foreach (GUIcontainer c in otherGUI)
                if (c.gui.core.vessel.loaded && FlightGlobals.Vessels.Contains(c.gui.core.vessel) && !c.gui.core.vessel.packed && c.gui.core.InControl)
                    c.gui.update();
                else
                    remove = c;
            if (remove != null)
            {
                otherGUI.Remove(remove);

                if (RTGlobals.extPack && otherGUI.Count == 0 && RTGlobals.Manager != null)
                {
                    RTGlobals.Manager.distantLandedPartPackThreshold = 350;
                    RTGlobals.Manager.distantLandedPartUnpackThreshold = 200;
                    RTGlobals.Manager.distantPartPackThreshold = 5000;
                    RTGlobals.Manager.distantPartUnpackThreshold = 200;
                }
            }

        }


        public void Update()
        {
            if (EnergyDrain > 0)
            {
                RequestPower();
            }

            flightComputerGUI.update();

            UpdateOtherGUI();

            if (ticks++ > 100)
            {
                ticks = 0;

                UpdateOtherModules();
                try
                {
                    path = RTGlobals.network.GetCommandPath(Rnode);
                }
                catch
                {
                    RTGlobals.network = new RelayNetwork();
                    this.path = RTGlobals.network.GetCommandPath(Rnode);
                }
            }


            if (!vessel.isActiveVessel) return;


            if (InContact && powered && RTGlobals.showPathInMapView && MapView.MapIsEnabled)
            {
                line.enabled = true;
                line.SetVertexCount(path.nodes.Count);
                for (int i = 0; i < path.nodes.Count; i++)
                {
                    line.SetPosition(i, path.nodes[i].ScaledPosition);
                }

                line.SetWidth((float)(0.005 * planetariumCamera.Distance), (float)(0.005 * planetariumCamera.Distance));

            }
            else
            {
                if (line != null)
                {
                    line.enabled = false;
                }
            }
            UpdateTriggers();
        }

        public bool InControl
        {
            get
            {
                return localControl || InContact;
            }
        }

        float AlarmTime = 0;
        bool alarmShow = false;
        void WindowGUI(int windowID)
        {
            Color savedColor = GUI.color;
            Color savedContentColor = GUI.contentColor;
            bool CrewControl = vessel.GetCrewCount() > 0;
            GUIStyle Alarm = new GUIStyle(GUI.skin.label);
            Alarm.fontStyle = FontStyle.Bold;
            Alarm.normal.textColor = Color.red;

            if (!powered)
            {
                GUI.DragWindow();

                string alarmMessage = "Out of power";
                if (AlarmTime < Time.time)
                {
                    AlarmTime = Time.time + 1;
                    alarmShow = !alarmShow;
                }
                if (alarmShow)
                    alarmMessage += " !";
                GUILayout.Label(alarmMessage, Alarm);
                return;
            }

            try
            {
                if (InContact)
                {
                    if (GUILayout.Button("Path length: " + RTUtils.length(path.Length) + "m, delay: " + RTUtils.time(path.ControlDelay) +
                        (RTGlobals.AdvInfo ?
                        "\nRelay path: " + path.ToString() :
                        ""),
                        GUI.skin.label, GUILayout.ExpandWidth(true)))
                    {
                        RTGlobals.AdvInfo = !RTGlobals.AdvInfo;
                    }

                }
                else
                {
                    string alarmMessage = "Out of contact";
                    if (AlarmTime < Time.time)
                    {
                        AlarmTime = Time.time + 1;
                        alarmShow = !alarmShow;
                    }
                    if (alarmShow)
                        alarmMessage += " !";
                    GUILayout.Label(alarmMessage, Alarm);
                }
            }
            catch (NullReferenceException)
            {
                RTGlobals.network = new RelayNetwork();
                path = RTGlobals.network.GetCommandPath(Rnode);
            }
            GUI.color = savedColor;

            GUILayout.BeginHorizontal();

            RTGlobals.listComsats = GUILayout.Toggle(RTGlobals.listComsats, "List comsats", GUI.skin.button, GUILayout.Height(20));


            if (!CrewControl && !(MechJeb && InContact)) GUI.contentColor = Color.red;
            localControl = GUILayout.Toggle(localControl, (!CrewControl && MechJeb) ? "MechJeb Control" : "Local Control", GUI.skin.button, GUILayout.Height(20));
            if (!CrewControl && !(MechJeb && InContact))
            {
                localControl = false;
                GUI.contentColor = savedContentColor;
            }


            RTGlobals.showPathInMapView = GUILayout.Toggle(RTGlobals.showPathInMapView, "Show path on map", GUI.skin.button, GUILayout.Height(20));

            RTGlobals.showFC = GUILayout.Toggle(RTGlobals.showFC, "Flight Computer", GUI.skin.button, GUILayout.Height(20));

            GUILayout.EndHorizontal();

            if (RTGlobals.listComsats)
            {

                if (!InControl) GUI.contentColor = Color.red;
                if (GUILayout.Button(Rnode.descript, new GUIStyle(GUI.skin.button)) && InControl)
                {
                    settings.Open(Rnode);
                }
                if (!InControl) GUI.contentColor = savedContentColor;

                RTGlobals.comsatListScroll = GUILayout.BeginScrollView(RTGlobals.comsatListScroll, false, true);

                //compiles a list of comsat vessels that are in the current RelayNetwork, Coloring the ones in the current RelayPath green.
                if (InContact || Rnode.HasCommand)
                    foreach (RelayNode node in RTGlobals.network.all)
                    {
                        if (!node.Equals(Rnode) && node.Vessel != null)
                        {
                            GUILayout.BeginHorizontal();
                            bool connection = RTGlobals.network.inContactWith(Rnode, node);

                            if (!connection)
                            {
                                if (GUI.contentColor != Color.red)
                                    GUI.contentColor = Color.red;
                            }
                            else
                                if (InContact && path.nodes.Contains(node))
                                {
                                    if (GUI.contentColor != Color.green)
                                        GUI.contentColor = Color.green;
                                }
                                else if (GUI.contentColor != Color.white) GUI.contentColor = Color.white;
                            if (GUILayout.Button(node.descript, new GUIStyle(GUI.skin.button), GUILayout.Height(50.0F)) && connection)
                            {
                                settings.Open(node);
                            }
                            if ((InContact || Rnode.HasCommand) && connection && node.Vessel.loaded && (RTGlobals.extPack ? Vector3d.Distance(vessel.transform.position, node.Vessel.transform.position) < 2000 : !node.Vessel.packed))
                            {
                                if (GUILayout.Button("Ctrl", new GUIStyle(GUI.skin.button), GUILayout.Width(50.0F), GUILayout.Height(50.0F)))
                                {
                                    bool isThere = false;
                                    int ATid = this.flightComputerGUI.ATTITUDE_ID + 1,
                                    THid = this.flightComputerGUI.THROTTLE_ID + 1;
                                    GUIcontainer remove = new GUIcontainer();
                                    foreach (GUIcontainer c in this.otherGUI)
                                    {
                                        if (c.gui == RTGlobals.coreList[node.Vessel].flightComputerGUI)
                                        {
                                            isThere = true;
                                            remove = c;
                                            break;
                                        }
                                        ATid = c.ATTITUDE_ID + 1;
                                        THid = c.THROTTLE_ID + 1;
                                    }

                                    if (isThere)
                                    {
                                        otherGUI.Remove(remove);
                                        if (RTGlobals.extPack && otherGUI.Count == 0 && RTGlobals.Manager != null)
                                        {
                                            RTGlobals.Manager.distantLandedPartPackThreshold = 350;
                                            RTGlobals.Manager.distantLandedPartUnpackThreshold = 200;
                                            RTGlobals.Manager.distantPartPackThreshold = 5000;
                                            RTGlobals.Manager.distantPartUnpackThreshold = 200;
                                        }
                                    }
                                    else
                                    {
                                        otherGUI.Add(new GUIcontainer(RTGlobals.coreList[node.Vessel].flightComputerGUI, ATid, THid));
                                    }
                                }
                            }

                            GUILayout.EndHorizontal();
                        }
                    }
                else
                {
                    GUI.contentColor = Color.red;
                    foreach (RelayNode node in RTGlobals.network.all)
                    {
                        if (!node.Equals(Rnode) && node.Vessel != null)
                        {
                            GUILayout.Label(node.descript, new GUIStyle(GUI.skin.button));
                        }
                    }
                }

                GUI.color = savedColor;
                GUI.contentColor = savedContentColor;
                GUILayout.EndScrollView();
            }

            GUI.DragWindow();
        }

        public void drawGUI()
        {
            if (!vessel.isActiveVessel || !RTGlobals.show) return;

            GUI.skin = HighLogic.Skin;

            RTGlobals.windowPos = GUILayout.Window(WINDOW_ID, RTGlobals.windowPos, WindowGUI, "Comms status", GUILayout.Width(300), GUILayout.Height((RTGlobals.listComsats && powered ? 600 : 50)));
            if (!powered) return;

            if (settings.show)
                RTGlobals.SettingPos = GUILayout.Window(settings.WINDOW_ID, RTGlobals.SettingPos, settings.SettingsGUI, "Relay Settings", GUILayout.Width(350), GUILayout.Height(600));

            if (RTGlobals.showFC)
            {
                RTGlobals.AttitudePos = GUILayout.Window(flightComputerGUI.ATTITUDE_ID, RTGlobals.AttitudePos, flightComputerGUI.AttitudeGUI, "Computer", GUILayout.Width(30), GUILayout.Height(60));
                RTGlobals.ThrottlePos = GUILayout.Window(flightComputerGUI.THROTTLE_ID, RTGlobals.ThrottlePos, flightComputerGUI.ThrottleGUI, "Throttle", GUILayout.Width(30), GUILayout.Height(60));
            }

            if (otherGUI.Count > 0)
            {
                for (int i = 0; i < otherGUI.Count; i++)
                {
                    otherGUI[i].AttitudePos = GUILayout.Window(otherGUI[i].ATTITUDE_ID, otherGUI[i].AttitudePos, otherGUI[i].gui.AttitudeGUI, "Computer: " + otherGUI[i].gui.core.vessel.vesselName, GUILayout.Width(30), GUILayout.Height(60));
                    otherGUI[i].ThrottlePos = GUILayout.Window(otherGUI[i].THROTTLE_ID, otherGUI[i].ThrottlePos, otherGUI[i].gui.ThrottleGUI, "Throttle: " + otherGUI[i].gui.core.vessel.vesselName, GUILayout.Width(30), GUILayout.Height(60));
                }
            }

        }



        public void drive(FlightCtrlState s)
        {
            if (!vessel.isActiveVessel)
                delayedBuffer.setNeutral(s);
            if (!localControl)
            {
                if (!InContact || !powered)
                {
                    //lock out the player if we are out of radio contact or out of power
                    delayedBuffer.setNeutral(s);
                }
                else
                    if (vessel.isActiveVessel)
                    {
                        //this is a somewhat crude fix for the buggy throttle controls introduced in KSP 0.17. It works nicely though.
                        if (GameSettings.AXIS_THROTTLE.GetAxis() == this.LastAxisThrottle && !DockingMode)
                        {
                            if (GameSettings.THROTTLE_UP.GetKey()) lastThrottle = Mathf.Clamp(lastThrottle + 0.01f, 0, 1);
                            if (GameSettings.THROTTLE_DOWN.GetKey()) lastThrottle = Mathf.Clamp(lastThrottle - 0.01f, 0, 1);
                            s.mainThrottle = lastThrottle;
                        }
                        else
                        {
                            LastAxisThrottle = GameSettings.AXIS_THROTTLE.GetAxis();
                            s.mainThrottle = lastThrottle = Mathf.Clamp((LastAxisThrottle + 1) / 2, 0, 1);
                        }

                        if (GameSettings.THROTTLE_CUTOFF.GetKey())
                        {
                            s.mainThrottle = lastThrottle = 0;
                        }

                        delayedBuffer.push(s, Planetarium.GetUniversalTime());
                        delayedBuffer.pop(s, Planetarium.GetUniversalTime() - path.ControlDelay);
                    }
            }

            s.killRot = flightComputerGUI.attitudeButtons[0].state.Active;
            vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, flightComputerGUI.attitudeButtons[0].state.Active);

            if (powered)
                computer.drive(s);

            //this makes sure that the navball readout shows the delayed throttle. Since it reads directly from the FlightInputHandlers throttle state.
            if (vessel.isActiveVessel)
                FlightInputHandler.state.mainThrottle = s.mainThrottle;
        }

    }
}
