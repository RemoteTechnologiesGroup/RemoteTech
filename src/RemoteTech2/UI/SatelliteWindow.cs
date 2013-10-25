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

        public SatelliteWindow()
            : base(Guid, null, new Rect(0, 0, 600, 600), WindowAlign.BottomRight)
        {
            RTCore.Instance.Satellites.OnUnregister += Refresh;
        }

        public void Refresh(ISatellite sat)
        {
            if (sat == Satellite) Hide();
        }

        public override void Show()
        {
            if (mSatelliteFragment.Satellite == null) return;
            base.Show();
        }

        public override void Window(int uid)
        {
            GameSettings.AXIS_MOUSEWHEEL.switchState = UIModeBindingLinRotState.None;
            GUI.skin = HighLogic.Skin;
            mSatelliteFragment.Draw();
            base.Window(uid);
            GameSettings.AXIS_MOUSEWHEEL.switchState = UIModeBindingLinRotState.Any;
        }

        public void Dispose() 
        {
            mSatelliteFragment.Dispose();
            RTCore.Instance.Satellites.OnUnregister -= Refresh;
        }

    }
}
