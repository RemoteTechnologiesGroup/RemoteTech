using System;
using RemoteTech.SimpleTypes;
using UnityEngine;
using RemoteTech.FlightComputer.Commands;

namespace RemoteTech.UI
{
    /// <summary>
    /// Class adding and handling a new button to maneuver nodes.
    /// </summary>
    public class ManeuverNodeOverlay : IFragment, IDisposable
    {
        private readonly GUIStyle mManeuverNodeButtonAdd;
        private readonly GUIStyle mManeuverNodeButtonDelete;
        
        private bool mShowOverlay = true;

        private MapView mMap { get { return MapView.fetch; } }

        public ManeuverNodeOverlay()
        {
            mManeuverNodeButtonAdd = GUITextureButtonFactory.CreateFromFilename("buttons_fc_add", "buttons_fc_add_hover", "buttons_fc_add_hover", "buttons_fc_add_hover");
            mManeuverNodeButtonAdd.fixedHeight = mManeuverNodeButtonAdd.fixedWidth = 0;
            mManeuverNodeButtonAdd.stretchHeight = mManeuverNodeButtonAdd.stretchWidth = true;

            mManeuverNodeButtonDelete = GUITextureButtonFactory.CreateFromFilename("buttons_fc_del", "buttons_fc_del_hover", "buttons_fc_del", "buttons_fc_del");
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
                        float btnWidth = 23.0f;
                        ManeuverNode node = pCS.maneuverNodes[i];
                        
                        // node has an attached gizmo?
                        if (node.attachedGizmo == null || node.UT < RTUtil.GameTime) continue;

                        ManeuverGizmo gizmo = node.attachedGizmo;
                        UnityEngine.UI.Button gizmoDeleteBtn = gizmo.deleteBtn;

                        // We are on the right gizmo but no buttons are visible so skip the rest
                        if (!gizmoDeleteBtn.isActiveAndEnabled)
                        {
                            continue;
                        }

                        Vector3 screenCoord = gizmo.camera.WorldToScreenPoint(gizmo.transform.position);
                        //Vector3 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
                        
                        //double dist = Math.Sqrt(Math.Pow(screenCenter.x - screenCoord.x, 2.0) + Math.Pow(screenCenter.y - screenCoord.y, 2.0));
                        //double btnDim = 20.0f + (8.0f * ((1.2f / screenCenter.magnitude) * Math.Abs(dist)));
                        //btnDim = 1.0f * gizmoDeleteBtn.transform.lossyScale.x;

                        Rect screenPos = new Rect(screenCoord.x - btnWidth - 5.0f, Screen.height - screenCoord.y - btnWidth, btnWidth, btnWidth);
                        
                        GUIStyle maneuverCtrl = mManeuverNodeButtonAdd;
                        bool nodeAlreadyQueued = flightComputer.HasManeuverCommandByNode(node);

                        // switch the button style
                        if (nodeAlreadyQueued)
                        {
                            maneuverCtrl = mManeuverNodeButtonDelete;
                        }
                        GUILayout.BeginArea(screenPos);

                        if (GUILayout.Button("", maneuverCtrl))
                        {
                            if (!nodeAlreadyQueued)
                            {
                                flightComputer.Enqueue(ManeuverCommand.WithNode(i, flightComputer), false, false, true);
                            }
                            else
                            {
                                flightComputer.RemoveManeuverCommandByNode(node);
                            }
                        }
                        GUILayout.EndArea();

                    }
                }
            }
        }
    }
}
