using System;
using UnityEngine;

namespace RemoteTech.UI
{
    public class AntennaWindow : AbstractWindow
    {
        public static Guid Guid = new Guid("39fe8878-d894-4ded-befb-d6e070ddc2c4");
        public IAntenna Antenna { get { return mSetAntenna; } set { mSetAntenna = value; if (mAntennaFragment != null) mAntennaFragment.Antenna = value; } }
        public AntennaFragment mAntennaFragment;
        public TargetInfoWindow mTargetInfos;
        private IAntenna mSetAntenna;

        public AntennaWindow(IAntenna antenna)
            : base(Guid, "Antenna Configuration", new Rect(100, 100, 300, 500), WindowAlign.Floating)
        {
            mSavePosition = true;
            mSetAntenna = antenna;
            mTargetInfos = new TargetInfoWindow(this, WindowAlign.Floating);
        }

        /// <summary>
        /// Mouse over callback forced by the mAntennaFragment
        /// </summary>
        public void showTargetInfo()
        {
            if (mAntennaFragment != null)
            {
                if (mAntennaFragment.mouseOverEntry != null)
                {
                    // set the current selected target to the targetwindow
                    mTargetInfos.SetTarget(mAntennaFragment.mouseOverEntry, mSetAntenna);
                    mTargetInfos.Show();
                }
                else
                {
                    // hide if we do not have any selection
                    mTargetInfos.Hide();
                }
            }
        }

        /// <summary>
        /// Mouse out callback forced by the mAntennaFragment
        /// </summary>
        public void hideTargetInfo()
        {
            mTargetInfos.Hide();
        }

        public override void Show()
        {
            mAntennaFragment = mAntennaFragment ?? new AntennaFragment(mSetAntenna);
            /// Add callbacks for the onPositionChanged on the AbstractWindow
            onPositionChanged += mTargetInfos.CalculatePosition;
           
            if (HighLogic.LoadedScene != GameScenes.EDITOR)
            { 
                /// Add the showTargetInfo callback to the on mouse over/out event
                mAntennaFragment.onMouseOverListEntry += showTargetInfo;
            }
            mAntennaFragment.onMouseOutListEntry += hideTargetInfo;
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequested);
            base.Show();
        }

        public override void Hide()
        {
            // also hide the target info popup
            hideTargetInfo();

            if (mAntennaFragment != null)
            {
                /// Remove callbacks from the onPositionChanged on the AbstractWindow
                onPositionChanged -= mTargetInfos.CalculatePosition;

                /// Remove the showTargetInfo callback from the on mouse over/out event
                mAntennaFragment.onMouseOverListEntry -= showTargetInfo;
                mAntennaFragment.onMouseOutListEntry -= hideTargetInfo;

                mAntennaFragment.Dispose(); mAntennaFragment = null;
            }

            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequested);
            base.Hide();
        }

        public override void Window(int uid)
        {
            if (mAntennaFragment.Antenna == null) { Hide(); return; }
            GUI.skin = HighLogic.Skin;

            // check the mouse position on every draw call
            mouseOverAntennaWindow();

            GUILayout.BeginVertical(GUILayout.Width(300), GUILayout.Height(500));
            {
                mAntennaFragment.Draw();
            }
            GUILayout.EndVertical();

            base.Window(uid);
        }

        /// <summary>
        /// Checks whether the mouse is over this window to set
        /// the triggerMouseOverListEntry on the AntennaFragment
        /// </summary>
        public void mouseOverAntennaWindow()
        {
            mAntennaFragment.triggerMouseOverListEntry = backupPosition.ContainsMouse();
        }

        public void OnVesselChange(Vessel v)
        {
            Hide();
        }

        public void OnGameSceneLoadRequested(GameScenes gs)
        {
            Hide();
        }
    }
}
