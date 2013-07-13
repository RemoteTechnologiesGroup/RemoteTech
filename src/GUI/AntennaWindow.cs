using UnityEngine;

namespace RemoteTech {
    public class AntennaWindow : AbstractWindow {
        private readonly IAntenna mAntenna;
        private readonly ISatellite mSatellite;
        private AntennaConfigFragment mAntennaFragment;

        public AntennaWindow(IAntenna antenna, ISatellite sat)
        : base("Antenna Configuration", new Rect(100, 100, 0, 0), WindowAlign.Floating) {
            mAntenna = antenna;
            mSatellite = sat;
        }

        public override void Window(int id) {
            base.Window(id);
            mAntennaFragment.Draw();
        }

        public override void Show() {
            mAntennaFragment = new AntennaConfigFragment(mAntenna, Hide, a => { });
            base.Show();
        }

        public override void Hide() {
            base.Hide();
            mAntennaFragment.Dispose();
        }
    }
}
