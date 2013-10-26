using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class AntennaWindow : AbstractWindow
    {
        public static Guid Guid = new Guid("39fe8878-d894-4ded-befb-d6e070ddc2c4");
        public IAntenna Antenna { get { return mSetAntenna; } set { mSetAntenna = value; if (mAntennaFragment != null) mAntennaFragment.Antenna = value; } }
        public AntennaFragment mAntennaFragment;

        private IAntenna mSetAntenna;

        public AntennaWindow(IAntenna antenna)
            : base(Guid, "Antenna Configuration", new Rect(100, 100, 300, 500), WindowAlign.Floating)
        {
            mSetAntenna = antenna;
        }

        public override void Show()
        {
            mAntennaFragment = mAntennaFragment ?? new AntennaFragment(mSetAntenna, () => Hide());
            GameEvents.onVesselChange.Add(OnVesselChange);
            base.Show();
        }

        public override void Hide()
        {
            if (mAntennaFragment != null)
            {
                mAntennaFragment.Dispose(); mAntennaFragment = null;
            }
            GameEvents.onVesselChange.Remove(OnVesselChange);
            base.Hide();
        }

        public override void Window(int uid)
        {
            GUI.skin = HighLogic.Skin;
            GUILayout.BeginVertical(GUILayout.Width(300), GUILayout.Height(500));
            {
                mAntennaFragment.Draw();
            }
            GUILayout.EndVertical();
            base.Window(uid);
        }

        public void OnVesselChange(Vessel v)
        {
            Hide();
        }
    }
}
