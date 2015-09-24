using System;
using RemoteTech.SimpleTypes;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.UI
{
    public class ManeuverNodeOverlay : IFragment, IDisposable
    {
        private readonly GUIStyle mManeuverNodeButtonAdd;
        private readonly GUIStyle mManeuverNodeButtonDelete;
        
        private bool mShowOverlay = true;

        private MapView mMap { get { return MapView.fetch; } }

        public ManeuverNodeOverlay()
        {
            mManeuverNodeButtonAdd = GUITextureButtonFactory.CreateFromFilename("maneuverAddBtn.png", "maneuverAddBtnHover.png", "maneuverAddBtn.png", "maneuverAddBtn.png");
            mManeuverNodeButtonAdd.fixedHeight = mManeuverNodeButtonAdd.fixedWidth = 0;
            mManeuverNodeButtonAdd.stretchHeight = mManeuverNodeButtonAdd.stretchWidth = true;

            mManeuverNodeButtonDelete = GUITextureButtonFactory.CreateFromFilename("maneuverDeleteBtn.png", "maneuverDeleteBtnHover.png", "maneuverDeleteBtn.png", "maneuverDeleteBtn.png");
            mManeuverNodeButtonDelete.fixedHeight = mManeuverNodeButtonDelete.fixedWidth = 0;
            mManeuverNodeButtonDelete.stretchHeight = mManeuverNodeButtonDelete.stretchWidth = true;

            MapView.OnEnterMapView += OnEnterMapView;
            MapView.OnExitMapView += OnExitMapView;

            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
        }

        public void Dispose()
        {
            MapView.OnEnterMapView -= OnEnterMapView;
            MapView.OnExitMapView -= OnExitMapView;

            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
        }

        public void OnEnterMapView()
        {
            RTCore.Instance.OnGuiUpdate += Draw;
        }

        public void OnExitMapView()
        {
            RTCore.Instance.OnGuiUpdate -= Draw;
        }

        private void OnHideUI()
        {
            mShowOverlay = false;
        }
        
        private void OnShowUI()
        {
            mShowOverlay = true;
        }

        /// <summary>
        /// Draws the RT add node to queue on the maneuver gizmo
        /// </summary>
        public void Draw()
        {
            if (!this.mShowOverlay) return;

            if (this.mMap != null && FlightGlobals.ActiveVessel != null)
            {
                // if we r on local control, skip these part
                if (FlightGlobals.ActiveVessel.HasLocalControl()) return;

                // if we've no flightcomputer, go out
                var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (satellite == null || satellite.SignalProcessor.FlightComputer == null) return;
                var flightComputer = satellite.SignalProcessor.FlightComputer;

                PatchedConicSolver pCS = FlightGlobals.ActiveVessel.patchedConicSolver;

                // PatchedConicSolver instantiated? and more than one maneuver node?
                if ( pCS != null && pCS.maneuverNodes.Count > 0)
                {
                    // Loop maneuvers
                    for (var i = 0; i < pCS.maneuverNodes.Count; i++)
                    {
                        ManeuverNode node = pCS.maneuverNodes[i];
                        
                        // node has an attached gizmo?
                        if (node.attachedGizmo == null || node.UT < RTUtil.GameTime) continue;

                        ManeuverGizmo gizmo = node.attachedGizmo;
                        ScreenSafeUIButton gizmoDeleteBtn = gizmo.deleteBtn;

                        // We are on the right gizmo but no buttons are visible so skip the rest
                        if (!gizmoDeleteBtn.renderer.isVisible)
                            continue;

                        Vector3 screenCoord = gizmo.camera.WorldToScreenPoint(gizmo.transform.position);
                        Vector3 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                        double dist = Math.Sqrt(Math.Pow(screenCenter.x - screenCoord.x, 2.0) + Math.Pow(screenCenter.y - screenCoord.y, 2.0));

                        double btnDim = 18.0f + (8.0f * ((1.2f / screenCenter.magnitude) * Math.Abs(dist)));

                        //btnDim = btnDim * lossyScale;
                        Rect screenPos = new Rect(screenCoord.x - (float)btnDim - 7.0f,
                                                  Screen.height - screenCoord.y - (float)btnDim - 3.0f,
                                                  (float)btnDim, (float)btnDim);

                        GUIStyle maneuverCtrl = mManeuverNodeButtonAdd;
                        bool nodeAlreadyQueued = flightComputer.hasManeuverCommandByNode(node);

                        // switch the button style
                        if (nodeAlreadyQueued)
                        {
                            maneuverCtrl = mManeuverNodeButtonDelete;
                        }

                        if (GUI.Button(screenPos, "", maneuverCtrl))
                        {
                            if (!nodeAlreadyQueued)
                            {
                                flightComputer.Enqueue(ManeuverCommand.WithNode(i, flightComputer), false, false, true);
                            }
                            else
                            {
                                flightComputer.removeManeuverCommandByNode(node);
                            }
                        }
                    }
                }
            }
        }
    }
}
