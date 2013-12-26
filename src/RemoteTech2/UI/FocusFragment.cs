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
                foreach (var sat in RTCore.Instance.Satellites)
                {
                    String text = sat.Name.Truncate(25);
                    RTUtil.StateButton(text, mSelection == sat ? 1 : 0, 1, s =>
                    {
                        mSelection = (s > 0) ? sat : null;
                        if (mSelection != null)
                        {
                            var newTarget = PlanetariumCamera.fetch.targets.FirstOrDefault(t => t != null && t.gameObject.name == sat.Name);
                            if (newTarget == null)
                            {
                                var vessel = sat.SignalProcessor.Vessel;
                                var scaledMovement = new GameObject().AddComponent<ScaledMovement>();
                                scaledMovement.tgtRef = vessel.transform;
                                scaledMovement.name = sat.Name;
                                scaledMovement.transform.parent = ScaledSpace.Instance.transform;
                                scaledMovement.vessel = vessel;
                                scaledMovement.type = MapObject.MapObjectType.VESSEL;
                                newTarget = scaledMovement;
                                PlanetariumCamera.fetch.SetTarget(PlanetariumCamera.fetch.AddTarget(newTarget));
                                PlanetariumCamera.fetch.targets.Remove(newTarget);
                            }
                            else
                            {
                                PlanetariumCamera.fetch.SetTarget(PlanetariumCamera.fetch.AddTarget(newTarget));
                            }
                            
                        }
                    });
                }
                GUI.skin.button.alignment = pushAlign;
                GUI.contentColor = pushColor;
            }
            GUILayout.EndScrollView();
        }
    }
}
