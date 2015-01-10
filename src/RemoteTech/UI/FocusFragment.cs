using System;
using UnityEngine;

namespace RemoteTech.UI
{
    public class FocusFragment : IFragment
    {
        private Vector2 mScrollPosition = Vector2.zero;
        private VesselSatellite mSelection = null;

        public void Draw()
        {
            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, AbstractWindow.Frame);
            {
                Color pushColor = GUI.contentColor;
                TextAnchor pushAlign = GUI.skin.button.alignment;
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                foreach (VesselSatellite sat in RTCore.Instance.Satellites)
                {
                    if ((sat.parentVessel != null && !MapViewFiltering.CheckAgainstFilter(sat.parentVessel)) || FlightGlobals.ActiveVessel == sat.parentVessel)
                    {
                        continue;
                    }

                    String text = sat.Name.Truncate(25);
                    RTUtil.StateButton(text, mSelection == sat ? 1 : 0, 1, s =>
                    {
                        mSelection = (s > 0) ? sat : null;
                        if (mSelection != null)
                        {
                            Vessel vessel = sat.SignalProcessor.Vessel;
                            ScaledMovement scaledMovement = new GameObject().AddComponent<ScaledMovement>();
                            scaledMovement.tgtRef = vessel.transform;
                            scaledMovement.name = sat.Name;
                            scaledMovement.transform.parent = ScaledSpace.Instance.transform;
                            scaledMovement.vessel = vessel;
                            scaledMovement.type = MapObject.MapObjectType.VESSEL;

                            var success = PlanetariumCamera.fetch.SetTarget(PlanetariumCamera.fetch.AddTarget(scaledMovement));
                            PlanetariumCamera.fetch.targets.Remove(scaledMovement);
                            this.resetTarget();
                        }
                        else
                        {
                            // go back to the active vessel
                            PlanetariumCamera.fetch.SetTarget(this.resetTarget());
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
