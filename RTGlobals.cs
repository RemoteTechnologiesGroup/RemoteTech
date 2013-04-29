using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{

    public static class RTGlobals
    {

        public static GameObject RTobj;
        public static RemoteTechController controller = new RemoteTechController();
        public static RemoteCoreList coreList = new RemoteCoreList();
        public static RelayNetwork network;

        public static OrbitPhysicsManager Manager = null;
        static bool componentAdded = false;
        public static void make()
        {
            if (RTobj == null)
            {
                RTobj = new GameObject();
            }

            if (controller == null)
            {
                controller = new RemoteTechController();
            }

            if (!componentAdded)
            {
                controller = RTGlobals.RTobj.AddComponent<RemoteTechController>();
                HighLogic.DontDestroyOnLoad(RTGlobals.RTobj);
                componentAdded = true;
            }

            LoadData();
        }

        public static string settingsKey = "f11";

        public static bool
            show = true,
            ColFriend = false;
        //Used in RemoteCore
        public static bool
            listComsats = false,
            showPathInMapView = true,
            AdvInfo = false;
        public static Rect windowPos = new Rect(Screen.width / 4, Screen.height / 4, 350, 200);
        public static Vector2 comsatListScroll = new Vector2();
        //public static bool localControl = false;

        //Used in RelayNetwork
        public static int RemoteCommandCrew = 3;

        //Used in SatSettings
        public static Rect SettingPos = new Rect((Screen.width / 2) - 175, (Screen.height / 2) - 300, 100, 200);
        public static List<Target> targets;

        //Used in Flight Computer
        public static bool showFC = false;
        public static Rect AttitudePos = new Rect((Screen.width / 2) - 132, Screen.height / 2, 100, 200);
        public static Rect ThrottlePos = new Rect((Screen.width / 2)-109, (Screen.height / 2) - 133, 100, 200);

        public static double speedOfLight = 300000000.0;
        public static bool extPack = true;

        public static void SaveData()
        {
            string s =
                windowPos.xMin / Screen.width + "\n" + windowPos.yMin / Screen.height + "\n" +
                SettingPos.xMin / Screen.width + "\n" + SettingPos.yMin / Screen.height + "\n" +
                AttitudePos.xMin / Screen.width + "\n" + AttitudePos.yMin / Screen.height + "\n" +
                ThrottlePos.xMin / Screen.width + "\n" + ThrottlePos.yMin / Screen.height + "\n" +
                showPathInMapView + "\n" +
                showFC + "\n" +
                listComsats + "\n" +
                show + "\n" +
                AdvInfo;


                KSP.IO.File.WriteAllText<RemoteCore>(s, "Data.dat");
        }

        public static bool LoadedData = false;
        public static void LoadData()
        {
            if (LoadedData) return;
            LoadedData = true;
            if (KSP.IO.File.Exists<RemoteCore>("Data.dat"))
            {
                try
                {
                    string[] ls = KSP.IO.File.ReadAllLines<RemoteCore>("Data.dat");
                    windowPos.xMin = Mathf.Clamp(float.Parse(ls[0]), 0, 1) * Screen.width;
                    windowPos.yMin = Mathf.Clamp(float.Parse(ls[1]), 0, 1) * Screen.height;
                    SettingPos.xMin = Mathf.Clamp(float.Parse(ls[2]), 0, 1) * Screen.width;
                    SettingPos.yMin = Mathf.Clamp(float.Parse(ls[3]), 0, 1) * Screen.height;
                    AttitudePos.xMin = Mathf.Clamp(float.Parse(ls[4]), 0, 1) * Screen.width;
                    AttitudePos.yMin = Mathf.Clamp(float.Parse(ls[5]), 0, 1) * Screen.height;
                    ThrottlePos.xMin = Mathf.Clamp(float.Parse(ls[6]), 0, 1) * Screen.width;
                    ThrottlePos.yMin = Mathf.Clamp(float.Parse(ls[7]), 0, 1) * Screen.height;
                    showPathInMapView = bool.Parse(ls[8]);
                    showFC = bool.Parse(ls[9]);
                    listComsats = bool.Parse(ls[10]);
                    show = bool.Parse(ls[11]);
                    AdvInfo = bool.Parse(ls[12]);
                }
                catch
                {
                }
            }
        }


        public static void Load()
        {
            if (KSP.IO.File.Exists<RemoteCore>("Settings.cfg"))
            {
                string[] ls = KSP.IO.File.ReadAllLines<RemoteCore>("Settings.cfg");
                string
                    SPEEDOFLIGHT = "",
                    RCC = "",
                    SETKEY = "",
                    EXTPACK = "",
                    COLFRIEND = "";

                foreach (string s in ls)
                {
                    if (!s.StartsWith("//") && s.Length > 2)
                    {
                        if (s.StartsWith("Speed of Light"))
                            SPEEDOFLIGHT = s;
                        else if (s.StartsWith("RemoteCommand Crew"))
                            RCC = s;
                        else if (s.StartsWith("Settings Key"))
                            SETKEY = s;
                        else if (s.StartsWith("Extended Loading Range"))
                            EXTPACK = s;
                        else if (s.StartsWith("Colourblind friendly mode"))
                            COLFRIEND = s;
                    }
                }

                string[] temp = SPEEDOFLIGHT.Split("=".ToCharArray());
                string tmp = temp[temp.Length - 1];
                temp = tmp.Split(" ".ToCharArray());
                try
                {
                    speedOfLight = double.Parse(temp[temp.Length - 1]);
                }
                catch (Exception)
                {
                    speedOfLight = 300000000;
                }

                temp = RCC.Split("=".ToCharArray());
                tmp = temp[temp.Length - 1];
                temp = tmp.Split(" ".ToCharArray());
                try
                {
                    int crew = int.Parse(temp[temp.Length - 1]);
                    if (crew > 0)
                        RemoteCommandCrew = crew;
                    else
                        RemoteCommandCrew = 1;
                }
                catch (Exception)
                {
                    RemoteCommandCrew = 3;
                }

                temp = SETKEY.Split("=".ToCharArray());
                tmp = temp[temp.Length - 1];
                temp = tmp.Split(" ".ToCharArray());
                try
                {
                    Input.GetKey(temp[temp.Length - 1]);
                    settingsKey = temp[temp.Length - 1];
                }
                catch (Exception)
                {
                    settingsKey = "f11";
                }

                temp = EXTPACK.Split("=".ToCharArray());
                tmp = temp[temp.Length - 1];
                temp = tmp.Split(" ".ToCharArray());
                try
                {
                    if (temp[temp.Length - 1].ToLower().Equals("on")) extPack = true;
                    if (temp[temp.Length - 1].ToLower().Equals("off")) extPack = false;
                }
                catch (Exception)
                {
                    extPack = true;
                }

                temp = COLFRIEND.Split("=".ToCharArray());
                tmp = temp[temp.Length - 1];
                temp = tmp.Split(" ".ToCharArray());
                try
                {
                    if (temp[temp.Length - 1].ToLower().Equals("on")) ColFriend = true;
                    if (temp[temp.Length - 1].ToLower().Equals("off")) ColFriend = false;
                }
                catch (Exception)
                {
                    ColFriend = false;
                }

            }
            else
                KSP.IO.File.WriteAllText<RemoteCore>(
    "//Here you can edit key used to access the RemoteTech settings (modifier key + settings key) (Default: f11):\nSettings Key = " + "f11" +
    "\n\n//Here you can edit the speed of light used to calculate control delay in m/s (Default: 300000000):\nSpeed of Light = " + "300000000" +
    "\n\n//Here you can edit the required crew for a command station (Minimum: 1, Default: 3)\nRemoteCommand Crew = " + "3" +
    "\n\n//Here you can toggle extended control range for unfocused vessels. If on, this could cause a bit of lag if you try to control an unfocused vessel when there are a lot of vessels in your immediate viscinity (default on)\nExtended Loading Range = " + "on" +
    "\n\n//Here you can toggle coulourblind friendly mode (default off)\nColourblind friendly mode = " + "off"
    , "Settings.cfg");
        }

    }
}