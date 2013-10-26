using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class SatelliteWindow : AbstractWindow
    {
        public static Guid Guid = new Guid("d7e902e7-2bb6-4130-83a4-311b3cb5953c");
        public ISatellite Satellite { get { return mSetSatellite; } set { mSetSatellite = value; if (mSatelliteFragment != null) mSatelliteFragment.Satellite = value; } }
        public SatelliteFragment mSatelliteFragment;

        private ISatellite mSetSatellite;

        public SatelliteWindow(ISatellite satellite)
            : base(Guid, null, new Rect(0, 0, 600, 600), WindowAlign.BottomRight)
        {
            mSetSatellite = satellite;
        }

        public void Refresh(ISatellite sat)
        {
            if (sat == Satellite) Hide();
        }

        public override void Show()
        {
            if (mSatelliteFragment.Satellite == null) return;
            RTCore.Instance.Satellites.OnUnregister += Refresh;
            mSatelliteFragment = mSatelliteFragment ?? new SatelliteFragment(mSetSatellite);
            base.Show();
        }

        public override void Hide()
        {
            if (mSatelliteFragment != null)
            {
                mSatelliteFragment.Dispose(); mSatelliteFragment = null;
            }
            RTCore.Instance.Satellites.OnUnregister -= Refresh;
            base.Hide();
        }

        public override void Window(int uid)
        {
            if (!RTCore.Instance.Network[mSatelliteFragment.Satellite].Any()) Hide();
            GUI.skin = HighLogic.Skin;
            mSatelliteFragment.Draw();
            base.Window(uid);
        }
    }
}
