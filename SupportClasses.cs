using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class RemoteTechController : MonoBehaviour
    {
        Rect DishSettingPos = new Rect((Screen.width / 2) - 175, (Screen.height / 2) - 300, 100, 200);
        public DishSettingsGUI settings = new DishSettingsGUI();
        public RemoteTechCoreSettings coreSettings = new RemoteTechCoreSettings();
        int vessels = 0;

        bool doOnce = true;
        public void Update()
        {
            if (HighLogic.fetch == null) return;

            try
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    doOnce = true;
                    if (RTGlobals.Manager == null)
                    {
                        RTGlobals.Manager = (OrbitPhysicsManager)GameObject.FindObjectOfType(typeof(OrbitPhysicsManager));
                    }

                    try
                    {
                        if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(RTGlobals.settingsKey))
                            coreSettings.Toggle();
                    }
                    catch { }

                    if (this.vessels != FlightGlobals.Vessels.Count)
                    {
                        RTGlobals.network = new RelayNetwork();
                        this.vessels = FlightGlobals.Vessels.Count;
                    }

                    RTGlobals.coreList.update();

                    if (RTGlobals.coreList.activeVesselIsRemoteTech)
                        RTUtils.applyLocks();
                    else
                        RTUtils.removeLocks();
                }
                else if (doOnce)
                {
                    RTGlobals.coreList.Clear();
                    RTGlobals.coreList.activeVesselIsRemoteTech = doOnce = false;
                    RTUtils.removeLocks();
                }

            }
            catch { }

            if (GUIcreated && !HighLogic.LoadedSceneIsFlight)
                destroyGUI();
        }



        public void createGUI()
        {
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(this.drawGUI));
            RenderingManager.AddToPostDrawQueue(3, new Callback(this.drawGUI));
            GUIcreated = true;
        }

        public void destroyGUI()
        {
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(this.drawGUI));
            GUIcreated = false;
        }

        bool GUIcreated = false;
        public void drawGUI()
        {
            GUI.skin = HighLogic.Skin;

            if (coreSettings.show)
            {
                coreSettings.Pos = GUILayout.Window(coreSettings.WINDOW_ID, coreSettings.Pos, coreSettings.GUI, "RemoteTech Settings", GUILayout.Width(500), GUILayout.Height(385));
                return;
            }

            RTGlobals.coreList.drawGUI();



            if (settings.show && settings.module != null)
            {
                DishSettingPos = GUILayout.Window(settings.WINDOW_ID, DishSettingPos, settings.SettingsGUI, settings.descript, GUILayout.Width(350), GUILayout.Height(600));
            }

        }


        public void OnDestroy()
        {
            RTGlobals.SaveData();
        }
    }

    public class RemoteCoreList : Dictionary<Vessel, RemoteCore>
    {
        public bool activeVesselIsRemoteTech = false;
        public RemoteCore ActiveCore;

        public void recalculate()
        {

            HashSet<Vessel> toDelete = new HashSet<Vessel>();
            foreach (KeyValuePair<Vessel, RemoteCore> pair in this)
            {
                if (!(pair.Key.loaded && RTUtils.IsComsat(pair.Key)))
                {
                    try
                    {
                        pair.Key.OnFlyByWire -= pair.Value.drive;
                    }
                    catch { }

                    toDelete.Add(pair.Key);
                }
            }
            foreach (Vessel v in toDelete)
            {
                this.Remove(v);
            }
            toDelete.Clear();
        }

        public void Add(Vessel V, float energyDrain)
        {
            if (!this.ContainsKey(V))
                this.Add(V, new RemoteCore(V, energyDrain));
        }

        public void update()
        {
            bool temp = false;
            foreach (KeyValuePair<Vessel, RemoteCore> pair in this)
            {
                if (pair.Key.loaded)
                    pair.Value.Update();
                if (pair.Key.isActiveVessel)
                    temp = true;
            }
            activeVesselIsRemoteTech = temp;
        }

        public void drawGUI()
        {
            if (this.TryGetValue(FlightGlobals.ActiveVessel, out ActiveCore))
                ActiveCore.drawGUI();
        }

    }


    public class GUIcontainer
    {
        public FlightComputerGUI gui;
        public int ATTITUDE_ID, THROTTLE_ID;

        public Rect AttitudePos, ThrottlePos;

        public GUIcontainer()
        {
        }

        public GUIcontainer(FlightComputerGUI input, int ATid, int THid)
        {
            this.gui = input;
            this.ATTITUDE_ID = ATid;
            this.THROTTLE_ID = THid;
            AttitudePos = new Rect((Screen.width / 2) - 132, Screen.height / 2, 100, 200);
            ThrottlePos = new Rect((Screen.width / 2) - 109, (Screen.height / 2) - 133, 100, 200);
        }

        public bool Equals(GUIcontainer other)
        {
            return this.gui == other.gui;
        }
    }


    public class DishSettingsGUI
    {
        public PartModule module;
        public int WINDOW_ID = 7283832;
        SatSettingNode settingNode;
        Vector2 SettingListScroll = new Vector2();
        string antennaName;
        public bool show = false;

        public void updateDishes()
        {
            settingNode.ReloadDishRange();
        }

        public DishSettingsGUI()
        {
        }

        public void Open(PartModule moduleIn)
        {
            if (!moduleIn.vessel.loaded)
            {
                Close();
                return;
            }
            this.module = moduleIn;

            RTGlobals.targets = new List<Target>();

            CBOrV SortNetwork = new CBOrV(Planetarium.fetch.Sun, new RelayNode(moduleIn.vessel));
            SortNetwork.createTargets(ref RTGlobals.targets);



            RTGlobals.targets.Add(new Target());
            RTGlobals.targets[RTGlobals.targets.Count - 1].GUIname = RTGlobals.targets[RTGlobals.targets.Count - 1].Name;
            RTGlobals.targets[RTGlobals.targets.Count - 1].color = Color.red;


            if (RTUtils.containsField(module, "dishRange") && (float)module.Fields.GetValue("dishRange") > 0)
            {
                settingNode = new SatSettingNode(module);
                if (RTUtils.containsField(module, "antennaName"))
                    antennaName = (string)module.Fields.GetValue("antennaName");
                else
                    antennaName = "Dish";

                this.show = true;
            }
        }

        public void Close()
        {
            this.show = false;
        }

        public void SaveAndClose()
        {

            this.Close();

            try
            {
                if (FlightGlobals.ActiveVessel.isEVA ? (Vector3d.Distance(module.vessel.transform.position, FlightGlobals.ActiveVessel.transform.position) > 50) : !RTGlobals.coreList[module.vessel].InControl) return;
            }
            catch { return; }

            if (settingNode != null && module.vessel.loaded)
            {
                if (RTUtils.containsField(module, "antennaName"))
                    module.Fields.SetValue("antennaName", antennaName);

                settingNode.save();

                RTGlobals.network = new RelayNetwork();
                RTGlobals.coreList[module.vessel].path = RTGlobals.network.GetCommandPath(RTGlobals.coreList[module.vessel].Rnode);
            }
        }

        public string descript
        {
            get
            {
                return "Range: " + RTUtils.length(settingNode.dishRange * 1000) + "m, Pointed At: " + settingNode.pointedAt.Name;
            }
        }

        public void SettingsGUI(int windowID)
        {
            try
            {
                this.antennaName = GUILayout.TextField(this.antennaName);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save & Close", new GUIStyle(GUI.skin.button))) SaveAndClose();
                if (GUILayout.Button("Close without saving", new GUIStyle(GUI.skin.button))) Close();
                GUILayout.EndHorizontal();

                SettingListScroll = GUILayout.BeginScrollView(SettingListScroll, false, true);

                settingNode.ListTargets();

                GUILayout.EndScrollView();
                //GUI.DragWindow();
            }
            catch (NullReferenceException)
            { this.Close(); }
        }


    }

    public class RemoteTechCoreSettings
    {
        public Rect Pos;
        public int WINDOW_ID = 9818832;
        bool expack;
        string SpeedOfLight,
            RcCrew;
        public bool show, colfriend;

        public RemoteTechCoreSettings()
        {
            Pos = new Rect((Screen.width / 2) - 250, (Screen.height / 2) - 192, 500, 385);
        }

        public void Toggle()
        {
            if (show)
                Close();
            else
                Open();
        }

        public void Open()
        {
            show = RTGlobals.show;
            colfriend = RTGlobals.ColFriend;
            SpeedOfLight = RTGlobals.speedOfLight.ToString();
            RcCrew = RTGlobals.RemoteCommandCrew.ToString();
            expack = RTGlobals.extPack;
        }

        public void Close()
        {
            show = false;

            RTGlobals.speedOfLight = double.Parse(SpeedOfLight);
            RTGlobals.RemoteCommandCrew = int.Parse(RcCrew);
            RTGlobals.extPack = expack;
            RTGlobals.ColFriend = colfriend;

            if (!expack && RTGlobals.Manager != null)
            {
                RTGlobals.Manager.distantLandedPartPackThreshold = 350;
                RTGlobals.Manager.distantLandedPartUnpackThreshold = 200;
                RTGlobals.Manager.distantPartPackThreshold = 5000;
                RTGlobals.Manager.distantPartUnpackThreshold = 200;
            }

            if (KSP.IO.File.Exists<RemoteCore>("Settings.cfg"))
                KSP.IO.File.Delete<RemoteCore>("Settings.cfg");

            KSP.IO.File.WriteAllText<RemoteCore>(
                "//Here you can edit key used to access the RemoteTech settings (modifier key + settings key) (Default: f11):\nSettings Key = " + RTGlobals.settingsKey + 
                "\n\n//Here you can edit the speed of light used to calculate control delay in m/s (Default: 300000000):\nSpeed of Light = " + SpeedOfLight + 
                "\n\n//Here you can edit the required crew for a command station (Minimum: 1, Default: 3)\nRemoteCommand Crew = " + RcCrew + 
                "\n\n//Here you can toggle extended control range for unfocused vessels. If on, this could cause a bit of lag if you try to control an unfocused vessel when there are a lot of vessels in your immediate viscinity (default on)\nExtended Loading Range = " + (expack ? "on" : "off") +
                "\n\n//Here you can toggle Coulourblind friendly mode (default off)\nColourblind friendly mode = " + (colfriend ? "on" : "off")
                , "Settings.cfg");
        }

        public void GUI(int windowID)
        {
            GUILayout.Space(25);
            GUILayout.Label("Speed of light used to calculate control delay in m/s (minimum 1)");
            GUILayout.BeginHorizontal();
            SpeedOfLight = GUILayout.TextField(SpeedOfLight);
            char[] tmp = SpeedOfLight.ToArray();
            SpeedOfLight = "";
            foreach (char a in tmp)
                if ("0123456789".Contains(a))
                    SpeedOfLight += a;
            if (SpeedOfLight == "")
                SpeedOfLight = "0";
            if (float.Parse(SpeedOfLight) < 1)
                SpeedOfLight = "1";
            if (GUILayout.Button("Reset to default", GUILayout.Width(110)))
                SpeedOfLight = "300000000";
            GUILayout.EndHorizontal();

            GUILayout.Space(25);

            GUILayout.Label("Required crew for a command station (minimum 1)");
            GUILayout.BeginHorizontal();
            RcCrew = GUILayout.TextField(RcCrew);
            tmp = RcCrew.ToArray();
            RcCrew = "";
            foreach (char a in tmp)
                if ("0123456789".Contains(a))
                    RcCrew += a;
            if (RcCrew == "")
                RcCrew = "0";
            if (float.Parse(RcCrew) < 1)
                RcCrew = "1";
            if (GUILayout.Button("Reset to default", GUILayout.Width(110)))
                RcCrew = "3";
            GUILayout.EndHorizontal();

            GUILayout.Space(25);

            GUILayout.Label("Extended control range for unfocused vessels. If on, this could cause a bit of lag if you try to control an unfocused vessel when there are a lot of vessels in your immediate viscinity (default on)");

            expack = GUILayout.Toggle(expack, "Extended control range " + (expack ? "on" : "off"));

            GUILayout.Space(25);

            GUILayout.Label("Colourblind friendly mode adds a small extra indicator to the flight computer (default off)");

            colfriend = GUILayout.Toggle(colfriend, "Colourblind friendly mode " + (colfriend ? "on" : "off"));

            GUILayout.Space(25);

            if (GUILayout.Button("Reset window positions to default"))
            {
                RTGlobals.windowPos = new Rect(Screen.width / 4, Screen.height / 4, 350, 200);
                RTGlobals.SettingPos = new Rect((Screen.width / 2) - 175, (Screen.height / 2) - 300, 100, 200);
                RTGlobals.AttitudePos = new Rect((Screen.width / 2) - 132, Screen.height / 2, 100, 200);
                RTGlobals.ThrottlePos = new Rect((Screen.width / 2) - 109, (Screen.height / 2) - 133, 100, 200);

                RTGlobals.SaveData();
            }

        }

    }

}