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
            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Height(Screen.height - 350), GUILayout.Width(300));
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
                            var newTarget = PlanetariumCamera.fetch.targets.FirstOrDefault(t => t.gameObject.name == sat.Name);
                            if (newTarget == null)
                            {
                                var vessel = sat.SignalProcessor.Vessel;
                                var scaledMovement = new GameObject().AddComponent<ScaledMovement>();
                                scaledMovement.tgtRef = vessel.transform;
                                scaledMovement.name = sat.Name;
                                scaledMovement.transform.parent = ScaledSpace.Instance.transform;
                                newTarget = scaledMovement;
                            }
                            sat.SignalProcessor.Vessel.orbitDriver.Renderer.isFocused = true;
                            PlanetariumCamera.fetch.SetTarget(newTarget);
                        }
                    });
                }
                GUI.skin.button.alignment = pushAlign;
                GUI.contentColor = pushColor;
            }
            GUILayout.EndScrollView();
        }
    }

    public class FocusWindow : AbstractWindow
    {
        public static Guid Guid = new Guid("f7ba0240-d2a5-4733-8460-e5d98d3494c3");
        public FocusFragment mFocusFragment = new FocusFragment();

        public FocusWindow() : base(Guid, null, new Rect(0, 0, 300, 600), WindowAlign.TopRight) { }

        public override void Window(int uid)
        {
            GUI.skin = HighLogic.Skin;
            mFocusFragment.Draw();
            base.Window(uid);
        }
    }
}
