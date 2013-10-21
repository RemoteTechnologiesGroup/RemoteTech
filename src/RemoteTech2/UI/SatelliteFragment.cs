using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class SatelliteFragment : IFragment, IDisposable
    {
        public ISatellite Satellite { get; set; }
        private readonly AntennaFragment mAntennaFragment = new AntennaFragment(null, () => { });
        private Vector2 mScrollPosition = Vector2.zero;
        private int mSelection = 0;

        public SatelliteFragment(ISatellite sat)
        {
            Satellite = sat;
            RTCore.Instance.Satellites.OnUnregister += Refresh;
        }

        public void Dispose()
        {
            RTCore.Instance.Satellites.OnUnregister -= Refresh;
            mAntennaFragment.Dispose();
        }

        void IFragment.Draw()
        {
            bool draw_antenna = mAntennaFragment.Antenna != null;
            GUILayout.BeginHorizontal(GUILayout.Width(draw_antenna ? 600 : 300), GUILayout.Height(300));
            {
                if (draw_antenna)
                {
                    mAntennaFragment.Draw();
                }
                Draw();
            }
            GUILayout.EndHorizontal();
        }

        private void Refresh(ISatellite sat)
        {
            if (sat == Satellite) Satellite = null;
        }

        public void Draw()
        {
            if (Satellite == null) return;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Box(Satellite.Name);
                RTUtil.Button("Name", () => { }, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(300));
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition);
                {
                    Color pushColor = GUI.contentColor;
                    TextAnchor pushAlign = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    int i = 0;
                    foreach (var a in Satellite.Antennas.Where(a => a.CanTarget))
                    {
                        i++;
                        GUI.contentColor = (a.Powered) ? Color.green : Color.red;
                        String text = a.Name + Environment.NewLine + "Target: " + RTUtil.TargetName(a.Target);
                        RTUtil.StateButton(text, mSelection, i, s =>
                        {
                            mSelection = (s > 0) ? s : 0;
                            mAntennaFragment.Antenna = (s > 0) ? a : null;
                        });
                    }
                    GUI.skin.button.alignment = pushAlign;
                    GUI.contentColor = pushColor;
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }
    }
}
