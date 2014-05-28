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
                    RTGui.StateButton(text, mSelection == sat ? 1 : 0, 1, s =>
                    {
                        mSelection = (s > 0) ? sat : null;
                        if (mSelection != null)
                        {
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
