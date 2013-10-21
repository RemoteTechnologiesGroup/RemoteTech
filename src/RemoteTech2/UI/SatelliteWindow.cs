using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class SatelliteWindow : AbstractWindow, IDisposable
    {
        public static Guid Guid = new Guid("d7e902e7-2bb6-4130-83a4-311b3cb5953c");
        public ISatellite Satellite { get { return mSatelliteFragment.Satellite; } set { mSatelliteFragment.Satellite = value; } }
        public SatelliteFragment mSatelliteFragment = new SatelliteFragment(null);

        public SatelliteWindow() : base(Guid, null, new Rect(0, 0, 600, 600), WindowAlign.BottomRight) { }

        public override void Show()
        {
            if (mSatelliteFragment.Satellite == null) return;
            base.Show();
        }

        public override void Window(int uid)
        {
            mSatelliteFragment.Draw();
            base.Window(uid);
        }

        public void Dispose() 
        {
            mSatelliteFragment.Dispose();
        }

    }
}
