using System;
using System.Linq;
using RemoteTech.Modules;
using UnityEngine;

namespace RemoteTech.UI
{
    public class SatelliteFragment : IFragment, IDisposable
    {
        public ISatellite Satellite
        {
            get { return mSatellite; }
            set { if (mSatellite != value) { mSatellite = value; Antenna = null; } }
        }

        public IAntenna Antenna { get; private set; }

        private ISatellite mSatellite;
        private Vector2 mScrollPosition = Vector2.zero;

        public SatelliteFragment(ISatellite sat)
        {
            Satellite = sat;
            RTCore.Instance.Satellites.OnUnregister += Refresh;
        }

        public void Dispose()
        {
            if (RTCore.Instance != null)
            {
                RTCore.Instance.Satellites.OnUnregister -= Refresh;
            }
        }

        public void Draw()
        {
            if (Satellite == null) return;

            GUILayout.BeginHorizontal();
            {
                GUILayout.TextField(Satellite.Name.Truncate(25), GUILayout.ExpandWidth(true));
                RTUtil.Button("Name", () =>
                {
                    var vessel = RTUtil.GetVesselById(Satellite.Guid);
                    if (vessel) vessel.RenameVessel();
                }, GUILayout.ExpandWidth(false), GUILayout.Height(24));
            }
            GUILayout.EndHorizontal();

            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.ExpandHeight(true));
            {
                Color pushColor = GUI.contentColor;
                TextAnchor pushAlign = GUI.skin.button.alignment;
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                foreach (var a in Satellite.Antennas.Where(a => a.CanTarget))
                {
                    GUI.contentColor = (a.Powered) ? XKCDColors.ElectricLime : XKCDColors.Scarlet;
                    String text = a.Name.Truncate(25) + Environment.NewLine + "Target: " + ModuleRTAntenna.TargetName(a.Target).Truncate(18);
                    RTUtil.StateButton(text, Antenna, a, s =>
                    {
                        Antenna = (s > 0) ? a : null;
                    });
                }
                GUI.skin.button.alignment = pushAlign;
                GUI.contentColor = pushColor;
            }
            GUILayout.EndScrollView();
        }

        private void Refresh(ISatellite sat)
        {
            if (sat == Satellite) Satellite = null;
        }
    }
}
