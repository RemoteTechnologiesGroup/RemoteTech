using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class AntennaFragment : IGUIFragment, IDisposable {

        IAntenna mFocus;
        OnClose mOnClose;

        public AntennaFragment(IAntenna sat, OnClose onClose) {
            mFocus = sat;
            mOnClose = onClose;

            RTCore.Instance.Antennas.Unregistered += OnAntenna;

            OnAntenna(mFocus);
        }

        public void Draw() {
            GUILayout.BeginVertical();
            RTUtil.Label(250, 30, "Module: " + mFocus.Name);
            RTUtil.Label(250, 30, "Target: " + mFocus.Target);
            RTUtil.Label(250, 30, "Dish Range: " + mFocus.DishRange);
            RTUtil.Label(250, 30, "Omni Range: " + mFocus.OmniRange);
            RTUtil.Button(250, 30, "Close", () => mOnClose.Invoke());
            GUILayout.EndVertical();
        }

        public void Dispose() {
            RTCore.Instance.Antennas.Unregistered -= OnAntenna;
        }

        void OnAntenna(IAntenna antenna) {
            if (antenna == mFocus) {
                mOnClose.Invoke();
            }
        }
    }

    public class AntennaGUIWindow : AbstractGUIWindow {

        IAntenna mAntenna;
        AntennaFragment mFragment;
        Rect mWindowPosition;

        public AntennaGUIWindow(IAntenna sat) {
            mAntenna = sat;
            mWindowPosition = new Rect(0, 0, 250, 400);
        }

        void Window(int id) {
            mFragment.Draw();
            GUI.DragWindow();
        }

        public override void Show() {
            mFragment = new AntennaFragment(mAntenna, () => Hide());
            base.Show();
        }

        public override void Hide() {
            base.Hide();
            if(mFragment != null) {
                mFragment.Dispose();
            }
        }

        protected override void Draw() {
            mWindowPosition = GUILayout.Window(46, mWindowPosition, Window, "Antenna Configuration");
        }
    }
}
