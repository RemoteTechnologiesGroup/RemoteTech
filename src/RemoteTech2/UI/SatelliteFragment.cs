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

        public void Draw()
        {
            bool draw_antenna = mAntennaFragment.Antenna != null;
            GUILayout.BeginHorizontal(GUILayout.Height(350));
            {
                if (draw_antenna)
                {
                    GUILayout.BeginVertical();
                    {
                        mAntennaFragment.Draw();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.BeginVertical();
                {
                    DrawSelf();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void Refresh(ISatellite sat)
        {
            if (sat == Satellite) Satellite = null;
        }

        private void DrawSelf()
        {
            if (Satellite == null) return;
            GUILayout.BeginHorizontal();
            {
                GUILayout.TextField(Satellite.Name.Truncate(25), GUILayout.ExpandWidth(true));
                RTUtil.Button("Name", () =>
                {
                    var vessel = FlightGlobals.Vessels.First(v => v.id == Satellite.Guid);
                    if (vessel) vessel.RenameVessel();
                }, GUILayout.ExpandWidth(false), GUILayout.Height(24));
            }
            GUILayout.EndHorizontal();

            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.ExpandHeight(true), GUILayout.Width(250));
            {
                Color pushColor = GUI.contentColor;
                TextAnchor pushAlign = GUI.skin.button.alignment;
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                int i = 0;
                foreach (var a in Satellite.Antennas.Where(a => a.CanTarget))
                {
                    i++;
                    GUI.contentColor = (a.Powered) ? XKCDColors.ElectricLime : XKCDColors.Scarlet;
                    String text = a.Name.Truncate(25) + Environment.NewLine + "Target: " + RTUtil.TargetName(a.Target).Truncate(18);
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
    }
}
