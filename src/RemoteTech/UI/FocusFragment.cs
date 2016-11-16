using System;
using RemoteTech.Common.Extensions;
using RemoteTech.Common.UI;
using RemoteTech.Common.Utils;
using UnityEngine;

namespace RemoteTech.UI
{
    public class FocusFragment : IFragment
    {
        private Vector2 mScrollPosition = Vector2.zero;
        private Vessel mSelection = null;
        private Vessel mLastVessel = null;

        public void Draw()
        {
            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, AbstractWindow.Frame);
            {
               
                Color pushColor = GUI.contentColor;
                TextAnchor pushAlign = GUI.skin.button.alignment;
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;

                var TSWList = UnityEngine.Object.FindObjectsOfType<KSP.UI.Screens.TrackingStationWidget>();

                foreach (VesselSatellite sat in RTCore.Instance.Satellites)
                {
                    if ((sat.parentVessel != null && !MapViewFiltering.CheckAgainstFilter(sat.parentVessel)) || FlightGlobals.ActiveVessel == sat.parentVessel)
                    {
                        continue;
                    }

                    String text = sat.Name.Truncate(25);
                    GuiUtil.StateButton(text, mSelection == sat.parentVessel ? 1 : 0, 1, s =>
                    {
                        mSelection = (s > 0) ? sat.parentVessel : null;
                        if (mSelection != null)
                        {
                            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                            {
                                foreach (var tsw in TSWList)
                                {
                                    if (tsw.vessel == sat.parentVessel)
                                    {
                                        tsw.toggle.isOn = true;
                                    }
                                }
                            }
                            else
                            {
                                Vessel vessel = sat.parentVessel;
                                ScaledMovement scaledMovement = new GameObject().AddComponent<ScaledMovement>();
                                scaledMovement.tgtRef = vessel.transform;
                                scaledMovement.name = sat.Name;
                                scaledMovement.transform.parent = ScaledSpace.Instance.transform;
                                scaledMovement.vessel = vessel;
                                scaledMovement.type = MapObject.ObjectType.Vessel;

                                var success = PlanetariumCamera.fetch.SetTarget(PlanetariumCamera.fetch.AddTarget(scaledMovement));
                                PlanetariumCamera.fetch.targets.Remove(scaledMovement);
                                this.resetTarget();

                                if(mLastVessel != vessel)
                                {
                                    if(mLastVessel)
                                    {
                                        mLastVessel.DetachPatchedConicsSolver();
                                        mLastVessel.orbitRenderer.isFocused = false;
                                    }                                    

                                    vessel.AttachPatchedConicsSolver();
                                    vessel.orbitRenderer.isFocused = true;
                                    vessel.orbitRenderer.drawIcons = OrbitRenderer.DrawIcons.OBJ;
                                    mLastVessel = vessel;
                                }
                            }
                        }
                        else
                        {
                            if (HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                            {
                                // go back to the active vessel
                                PlanetariumCamera.fetch.SetTarget(this.resetTarget());
                            }                               
                        }
                    });
                }
                GUI.skin.button.alignment = pushAlign;
                GUI.contentColor = pushColor;
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// This method resets the current selected state button on the FocusFragment.
        /// Should be called by enter map view.
        /// </summary>
        public void resetSelection()
        {
            // reset the selection set before
            mSelection = null;
        }

        /// <summary>
        /// This method sets the selection of the focus overlay.
        /// Should be called when a tracking station button is clicked
        /// </summary>
        public void setSelection(Vessel v)
        {
            mSelection = v;
        }

        /// <summary>
        /// Reset the current target object on the PlanetariumCamera to the Active vessel
        /// or if there is no active vessel (tracking station), back to Kerbin
        /// </summary>
        /// <returns>Found target as MapObject. Can be the active vessel or kerbin</returns>
        public MapObject resetTarget()
        {
            // try to get the active vessel
            int target_id = PlanetariumCamera.fetch.GetTargetIndex("ActiveVesselScaled");
            if (target_id == -1)
            {
                // there is no active vessel, go to target kerbin
                target_id = PlanetariumCamera.fetch.GetTargetIndex("Kerbin");
            }

            MapObject activeVesselObj = PlanetariumCamera.fetch.GetTarget(target_id);
            PlanetariumCamera.fetch.target = activeVesselObj;
            return activeVesselObj;
        }
    }
}
