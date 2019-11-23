﻿using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace RemoteTech.UI
{
    class DebugWindow : AbstractWindow
    {
        #region AbstractWindow-Definitions

        public DebugWindow()
            : base(new Guid("B17930C0-EDE6-4299-BE78-D975EAD1986B"), Localizer.Format("#RT_DEBUG_title"),//"RemoteTech DebugWindow"
                   new Rect(Screen.width / 2 - 250, Screen.height / 2 - 225, 500, 450), WindowAlign.Floating)
        {
            this.mSavePosition = true;
            this.initializeDebugMenue();
        }

        public override void Hide()
        {
            base.Hide();
        }
        #endregion

        #region Member
        /// <summary>Scroll position of the debug log textarea</summary>
        private Vector2 debugLogScrollPosition;
        /// <summary>Scroll position of the content area</summary>
        private Vector2 contentScrollPosition;
        /// <summary>Current selected log level</summary>
        private RTLogLevel currentLogLevel = RTLogLevel.LVL1;
        /// <summary>Current selected menue item</summary>
        private int currentDebugMenue = 0;
        /// <summary>List of all menue items</summary>
        private List<string> debugMenueItems = new List<string>();

        private int deactivatedMissionControls = 0;

        /// API Input fields
        private string HasFlightComputerGuidInput = "";
        private string HasAnyConnectionGuidInput = "";
        private string HasConnectionToKSCGuidInput = "";
        private string GetShortestSignalDelayGuidInput = "";
        private string GetSignalDelayToKSCGuidInput = "";
        private string GetSignalDelayToSatelliteGuidAInput = "";
        private string GetSignalDelayToSatelliteGuidBInput = "";
        private string ReceivDataVesselGuidInput = "";
        private string HasLocalControlGuidInput = "";
        private string GetMaxRangeDistanceSatelliteGuidAInput = "";
        private string GetMaxRangeDistanceSatelliteGuidBInput = "";
        private string GetRangeDistanceSatelliteGuidAInput = "";
        private string GetRangeDistanceSatelliteGuidBInput = "";
        #endregion

        #region Base-drawing
        /// <summary>
        /// Draws the content of the window
        /// </summary>
        public override void Window(int uid)
        {
            // push the current GUI.skin
            var pushSkin = GUI.skin;
            GUI.skin = HighLogic.Skin;

            GUILayout.BeginVertical(GUILayout.Width(500), GUILayout.Height(350));
            {
                #region Draw debug menue
                // Draw the debug menue
                GUILayout.BeginHorizontal();
                {
                    // push the font size of buttons
                    var pushFontsize = GUI.skin.button.fontSize;
                    GUI.skin.button.fontSize = 11;

                    int menueItemCounter = 0;
                    foreach (string menueItem in this.debugMenueItems)
                    {
                        RTUtil.FakeStateButton(new GUIContent(menueItem), () => { this.currentDebugMenue = menueItemCounter; }, currentDebugMenue, menueItemCounter, GUILayout.Height(16));
                        menueItemCounter++;
                    }

                    // pop the saved button size back
                    GUI.skin.button.fontSize = pushFontsize;
                }
                GUILayout.EndHorizontal();
                #endregion

                #region Draw content
                contentScrollPosition = GUILayout.BeginScrollView(contentScrollPosition);
                {
                    switch (this.currentDebugMenue)
                    {
                        case 0: { this.drawRTSettingsTab(); break; }
                        case 1: { this.drawAPITester(); break; }
                        case 2: { this.drawGuidReader(); break; }
                        default: { GUILayout.Label("Item " + this.currentDebugMenue.ToString() + " not yet implemented"); break; }
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndScrollView();
                #endregion

                #region Draw debug log
                // Draw a 100 height debug-console at the bottom of the debug-window
                GUILayout.BeginVertical(GUILayout.Height(150));
                this.drawRTDebugLogEntrys();
                GUILayout.EndVertical();
                // Draw the clear log button
                // Clear Logs Button
                GUILayout.BeginHorizontal();
                {
                    var pushFontsize = GUI.skin.button.fontSize;
                    GUI.skin.button.fontSize = 12;
                    RTUtil.Button(new GUIContent(Localizer.Format("#RT_DEBUG_Clearbutton",this.currentLogLevel.ToString()), Localizer.Format("#RT_DEBUG_Clearbutton_desc")), () => RTLog.RTLogList[this.currentLogLevel].Clear());//"Clear Logs in ""tbd."
                    GUI.skin.button.fontSize = pushFontsize;
                }
                GUILayout.EndHorizontal();
                #endregion
            }
            GUILayout.EndVertical();

            base.Window(uid);

            // pop back the saved skin
            GUI.skin = pushSkin;
        }
        #endregion

        private void initializeDebugMenue()
        {
            this.debugMenueItems.Add(Localizer.Format("#RT_DEBUG_RTSettings"));//"RemoteTech Settings"
            this.debugMenueItems.Add(Localizer.Format("#RT_DEBUG_APITester"));//"API-Tester"
            this.debugMenueItems.Add(Localizer.Format("#RT_DEBUG_GUIDReader"));//"GUID-Reader"
        }

        /// <summary>
        /// Draws all debug logs
        /// </summary>
        private void drawRTDebugLogEntrys()
        {
            GUIStyle lablestyle = new GUIStyle(GUI.skin.label);
            lablestyle.wordWrap = false;
            lablestyle.fontSize = 11;
            lablestyle.normal.textColor = Color.white;

            // draw the vertical buttons for each debug lvl
            GUILayout.BeginHorizontal();
            {
                var pushFontsize = GUI.skin.button.fontSize;
                GUI.skin.button.fontSize = 11;
                foreach (RTLogLevel lvl in Enum.GetValues(typeof(RTLogLevel)))
                {
                    RTUtil.FakeStateButton(new GUIContent(lvl.ToString()), () => { this.currentLogLevel = lvl; }, (int)currentLogLevel, (int)lvl, GUILayout.Height(16));
                }
                GUI.skin.button.fontSize = pushFontsize;
            }
            GUILayout.EndHorizontal();

            // draw the input of the selected debug list
            debugLogScrollPosition = GUILayout.BeginScrollView(debugLogScrollPosition);
            {
                foreach (var logEntry in RTLog.RTLogList[this.currentLogLevel])
                {
                    GUILayout.Label(logEntry, lablestyle, GUILayout.Height(13));
                }
            }
            GUILayout.EndScrollView();

            // If the mouse is not over the debug window we will flip the scrollposition.y to maximum
            if (!this.backupPosition.ContainsMouse())
            {
                debugLogScrollPosition.y = Mathf.Infinity;
            }
        }

        /// <summary>
        /// Draws the RTSettings section
        /// </summary>
        private void drawRTSettingsTab()
        {
            var settings = RTSettings.Instance;
            int firstColWidth = 250;

            var pushLabelSize = GUI.skin.label.fontSize;
            var pushButtonSize = GUI.skin.button.fontSize;
            GUI.skin.label.fontSize = 12;
            GUI.skin.button.fontSize = 12;

            // Deaktivate Mission Control 
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#RT_DEBUG_DeactivateKSC"), GUILayout.Width(firstColWidth));//"Deactivate KSC: "
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Onbtton")), () => { foreach (MissionControlSatellite mcs in settings.GroundStations) { mcs.togglePower(false); }; deactivatedMissionControls = 1; }, deactivatedMissionControls, 1);//"On"
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Offbtton")), () => { foreach (MissionControlSatellite mcs in settings.GroundStations) { mcs.togglePower(true); }; deactivatedMissionControls = 0; }, deactivatedMissionControls, 0);//"Off"
            }
            GUILayout.EndHorizontal();


            GUILayout.Space(10);
            GUILayout.Label(Localizer.Format("#RT_DEBUG_CheatOptions"));//"Cheat Options"

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#RT_DEBUG_SignalThroughBodies"), GUILayout.Width(firstColWidth));//"Signal Through Bodies: "
                int cheatLineOfSight = (RTSettings.Instance.IgnoreLineOfSight) ? 1 : 0;
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Onbtton")), () => { RTSettings.Instance.IgnoreLineOfSight = true; }, cheatLineOfSight, 1);//"On"
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Offbtton")), () => { RTSettings.Instance.IgnoreLineOfSight = false; }, cheatLineOfSight, 0);//"Off"
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#RT_DEBUG_InfiniteFuel"), GUILayout.Width(firstColWidth));//"Infinite Fuel: "
                int cheatinfiniteFuel = (CheatOptions.InfinitePropellant) ? 1 : 0;
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Onbtton")), () => { CheatOptions.InfinitePropellant = true; }, cheatinfiniteFuel, 1);//"On"
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Offbtton")), () => { CheatOptions.InfinitePropellant = false; }, cheatinfiniteFuel, 0);//"Off"
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Localizer.Format("#RT_DEBUG_InfiniteRCSFuel"), GUILayout.Width(firstColWidth));//"Infinite RCS Fuel: "
                int cheatinfiniteRCSFuel = (CheatOptions.InfinitePropellant) ? 1 : 0;
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Onbtton")), () => { CheatOptions.InfinitePropellant = true; }, cheatinfiniteRCSFuel, 1);//"On"
                RTUtil.FakeStateButton(new GUIContent(Localizer.Format("#RT_DEBUG_Offbtton")), () => { CheatOptions.InfinitePropellant = false; }, cheatinfiniteRCSFuel, 0);//"Off"
            }
            GUILayout.EndHorizontal();

            GUI.skin.label.fontSize = pushLabelSize;
            GUI.skin.button.fontSize = pushButtonSize;
        }

        /// <summary>
        /// Draws the API Tester section
        /// </summary>
        private void drawAPITester()
        {
            // switch to the API Debug log
            this.currentLogLevel = RTLogLevel.API;

            #region API.HasFlightComputer
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.HasFlightComputer; Guid: ", GUILayout.ExpandWidth(true));
                this.HasFlightComputerGuidInput = GUILayout.TextField(this.HasFlightComputerGuidInput, GUILayout.Width(160));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.HasFlightComputer(new Guid(this.HasFlightComputerGuidInput));
                        RTLog.Verbose("API.HasFlightComputer({0}) = {1}", this.currentLogLevel, this.HasFlightComputerGuidInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.HasAnyConnection
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.HasAnyConnection; Guid: ", GUILayout.ExpandWidth(true));
                this.HasAnyConnectionGuidInput = GUILayout.TextField(this.HasAnyConnectionGuidInput, GUILayout.Width(160));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.HasAnyConnection(new Guid(this.HasAnyConnectionGuidInput));
                        RTLog.Verbose("API.HasAnyConnection({0}) = {1}", this.currentLogLevel, this.HasAnyConnectionGuidInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.HasConnectionToKSC
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.HasConnectionToKSC; Guid: ", GUILayout.ExpandWidth(true));
                this.HasConnectionToKSCGuidInput = GUILayout.TextField(this.HasConnectionToKSCGuidInput, GUILayout.Width(160));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.HasConnectionToKSC(new Guid(this.HasConnectionToKSCGuidInput));
                        RTLog.Verbose("API.HasConnectionToKSC({0}) = {1}", this.currentLogLevel, this.HasConnectionToKSCGuidInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region APi.GetShortestSignalDelay
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.GetShortestSignalDelay; Guid: ", GUILayout.ExpandWidth(true));
                this.GetShortestSignalDelayGuidInput = GUILayout.TextField(this.GetShortestSignalDelayGuidInput, GUILayout.Width(160));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.GetShortestSignalDelay(new Guid(this.GetShortestSignalDelayGuidInput));
                        RTLog.Verbose("API.GetShortestSignalDelayGuidInput({0}) = {1}", this.currentLogLevel, this.GetShortestSignalDelayGuidInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.GetSignalDelayToKSC
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.GetSignalDelayToKSC; Guid: ", GUILayout.ExpandWidth(true));
                this.GetSignalDelayToKSCGuidInput = GUILayout.TextField(this.GetSignalDelayToKSCGuidInput, GUILayout.Width(160));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.GetSignalDelayToKSC(new Guid(this.GetSignalDelayToKSCGuidInput));
                        RTLog.Verbose("API.GetSignalDelayToKSC({0}) = {1}", this.currentLogLevel, this.GetSignalDelayToKSCGuidInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.GetSignalDelayToSatellite
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.GetSignalDelayToSatellite; Guid: ", GUILayout.ExpandWidth(true));
                this.GetSignalDelayToSatelliteGuidAInput = GUILayout.TextField(this.GetSignalDelayToSatelliteGuidAInput, GUILayout.Width(70));
                GUILayout.Label("to: ", GUILayout.ExpandWidth(true));
                this.GetSignalDelayToSatelliteGuidBInput = GUILayout.TextField(this.GetSignalDelayToSatelliteGuidBInput, GUILayout.Width(70));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.GetSignalDelayToSatellite(new Guid(this.GetSignalDelayToSatelliteGuidAInput), new Guid(this.GetSignalDelayToSatelliteGuidBInput));
                        RTLog.Verbose("API.GetSignalDelayToSatellite({0},{1}) = {2}", this.currentLogLevel, this.GetSignalDelayToSatelliteGuidAInput, this.GetSignalDelayToSatelliteGuidBInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.QueueCommandToFlightComputer
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.QueueCommandToFlightComputer; Guid: ", GUILayout.ExpandWidth(true));
                this.ReceivDataVesselGuidInput = GUILayout.TextField(this.ReceivDataVesselGuidInput, GUILayout.Width(160));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        ConfigNode dataNode = new ConfigNode();

                        dataNode.AddValue("Executor", "RemoteTech");
                        dataNode.AddValue("QueueLabel", "Receive Data");
                        dataNode.AddValue("ActiveLabel", "Receiveing Data ...");
                        dataNode.AddValue("ShortLabel", "Receive Data");
                        dataNode.AddValue("ReflectionType", "RemoteTech.UI.DebugWindow");
                        dataNode.AddValue("ReflectionPopMethod", "ReceiveDataPop");
                        dataNode.AddValue("ReflectionExecuteMethod", "ReceiveDataExec");
                        dataNode.AddValue("ReflectionAbortMethod", "ReceiveDataAbort");
                        dataNode.AddValue("GUIDString", this.ReceivDataVesselGuidInput);
                        dataNode.AddValue("YourData1", "RemoteTech");
                        dataNode.AddValue("YourDataN", "TechRemote");

                        var result = RemoteTech.API.API.QueueCommandToFlightComputer(dataNode);

                        RTLog.Verbose("API.QueueCommandToFlightComputer(ConfigNode) = {0}", this.currentLogLevel, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.HasLocalControl
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.HasLocalControl; Guid: ", GUILayout.ExpandWidth(true));
                this.HasLocalControlGuidInput = GUILayout.TextField(this.HasLocalControlGuidInput, GUILayout.Width(160));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.HasLocalControl(new Guid(this.HasLocalControlGuidInput));
                        RTLog.Verbose("API.HasLocalControl({0}) = {1}", this.currentLogLevel, this.HasLocalControlGuidInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.GetMaxRangeDistance
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.GetMaxRangeDistance; Guid: ", GUILayout.ExpandWidth(true));
                this.GetMaxRangeDistanceSatelliteGuidAInput = GUILayout.TextField(this.GetMaxRangeDistanceSatelliteGuidAInput, GUILayout.Width(70));
                GUILayout.Label("to: ", GUILayout.ExpandWidth(true));
                this.GetMaxRangeDistanceSatelliteGuidBInput = GUILayout.TextField(this.GetMaxRangeDistanceSatelliteGuidBInput, GUILayout.Width(70));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.GetMaxRangeDistance(new Guid(this.GetMaxRangeDistanceSatelliteGuidAInput), new Guid(this.GetMaxRangeDistanceSatelliteGuidBInput));
                        RTLog.Verbose("API.GetMaxRangeDistance({0},{1}) = {2}", this.currentLogLevel, this.GetMaxRangeDistanceSatelliteGuidAInput, this.GetMaxRangeDistanceSatelliteGuidBInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
            #region API.GetRangeDistance
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("API.GetRangeDistance; Guid: ", GUILayout.ExpandWidth(true));
                this.GetRangeDistanceSatelliteGuidAInput = GUILayout.TextField(this.GetRangeDistanceSatelliteGuidAInput, GUILayout.Width(70));
                GUILayout.Label("to: ", GUILayout.ExpandWidth(true));
                this.GetRangeDistanceSatelliteGuidBInput = GUILayout.TextField(this.GetRangeDistanceSatelliteGuidBInput, GUILayout.Width(70));
                if (GUILayout.Button("Run", GUILayout.Width(50)))
                {
                    try
                    {
                        var result = RemoteTech.API.API.GetRangeDistance(new Guid(this.GetRangeDistanceSatelliteGuidAInput), new Guid(this.GetRangeDistanceSatelliteGuidBInput));
                        RTLog.Verbose("API.GetRangeDistance({0},{1}) = {2}", this.currentLogLevel, this.GetRangeDistanceSatelliteGuidAInput, this.GetRangeDistanceSatelliteGuidBInput, result);
                    }
                    catch (Exception ex)
                    {
                        RTLog.Verbose("Exception {0}", this.currentLogLevel, ex);
                    }
                    // go to the end of the log
                    this.debugLogScrollPosition.y = Mathf.Infinity;
                }
            }
            GUILayout.EndHorizontal();
            #endregion
        }

        /// <summary>
        /// Draws a list with all ship guids and command stations to copy these values for the api tester
        /// </summary>
        private void drawGuidReader()
        {
            // draw the vessel list
            #region Vessel list
            foreach (var vessel in FlightGlobals.Vessels)
            {
                // skip different types
                if (vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Unknown) continue;

                GUILayout.BeginHorizontal();
                {
                    var pushFontStyle = GUI.skin.label.fontStyle;
                    // active vessel, make bold
                    if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.id == vessel.id)
                    {
                        GUI.skin.label.fontStyle = FontStyle.Bold;
                    }

                    GUILayout.Label(vessel.vesselName, GUILayout.ExpandWidth(true));
                    GUILayout.TextField(vessel.id.ToString(), GUILayout.Width(270));
                    GUI.skin.label.fontStyle = pushFontStyle;
                }
                GUILayout.EndHorizontal();
            }
            #endregion

            // draw the Ground stations
            #region Ground stations
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#RT_DEBUG_LoadedScene"), GUILayout.ExpandWidth(true));//"Ground stations are only available in the flight or tracking station."
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (var stations in RTCore.Instance.Network.GroundStations)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(stations.Value.Name, GUILayout.ExpandWidth(true));
                        GUILayout.TextField(stations.Key.ToString(), GUILayout.Width(270));
                    }
                    GUILayout.EndHorizontal();
                }
                
            }
            #endregion
        }

        /// <summary>
        /// This method is needed as a callback function for the API.QueueCommandToFlightComputer
        /// </summary>
        /// <param name="data">data passed from the flightcomputer</param>
        public static bool ReceiveDataPop(ConfigNode data)
        {
            RTLog.Verbose("Received Data via Api.ReceiveData", RTLogLevel.API);
            RTLog.Notify("Data: {0}",data);
            return true;
        }
        public static bool ReceiveDataExec(ConfigNode data)
        {
            RTLog.Verbose("Received Data via Api.ReceiveDataExec", RTLogLevel.API);
            RTLog.Notify("Data: {0}", data);
            
            return bool.Parse(data.GetValue("AbortCommand"));
        }
        public static void ReceiveDataAbort(ConfigNode data)
        {
            RTLog.Verbose("Aborting via Api.ReceiveDataAbort", RTLogLevel.API);
            RTLog.Notify("Data: {0}", data);
        }
    }
}