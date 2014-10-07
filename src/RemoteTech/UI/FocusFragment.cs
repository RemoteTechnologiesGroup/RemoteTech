using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
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
                            MapObject newTarget = PlanetariumCamera.fetch.targets.FirstOrDefault(t => t != null && t.gameObject.name == sat.Name);
                            Vessel vessel = sat.SignalProcessor.Vessel;
                            ScaledMovement scaledMovement = new GameObject().AddComponent<ScaledMovement>();
                            scaledMovement.tgtRef = vessel.transform;
                            scaledMovement.name = sat.Name;
                            scaledMovement.transform.parent = ScaledSpace.Instance.transform;
                            scaledMovement.vessel = vessel;
                            scaledMovement.type = MapObject.MapObjectType.VESSEL;
                            newTarget = scaledMovement;

                            var success = PlanetariumCamera.fetch.SetTarget(PlanetariumCamera.fetch.AddTarget(newTarget));
                            PlanetariumCamera.fetch.targets.Remove(newTarget);
                            PlanetariumCamera.fetch.target = PlanetariumCamera.fetch.initialTarget;
                        }
                        else
                        {
                            // go back to the active vessel
                            PlanetariumCamera.fetch.SetTarget("ActiveVesselScaled");
                        }
                    });
                }
                GUI.skin.button.alignment = pushAlign;
                GUI.contentColor = pushColor;
            }
            GUILayout.EndScrollView();
        }

        public void resetSelection()
        {
            // reset the selection set before
            mSelection = null;
        }
    }
}
