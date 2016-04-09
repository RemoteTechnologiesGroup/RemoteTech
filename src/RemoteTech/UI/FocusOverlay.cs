using System;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTech.UI
{
    public class FocusOverlay : IFragment, IDisposable
    {     
        private FocusFragment mFocus = new FocusFragment();
        private bool mShowOverlay = false;
		private Texture2D satellite;

		private KSP.UI.Screens.ApplicationLauncherButton mButton;
		private UnityEngine.UI.Image mButtonImg;

		private Rect PositionFrame
        {
            get
            {
				float scale = GameSettings.UI_SCALE;

				var pos = KSP.UI.UIMainCamera.Camera.WorldToScreenPoint(mButtonImg.rectTransform.position);

				var rect = new Rect(0, 0, 250, 500);
                if(HighLogic.LoadedSceneIsFlight)
                {
                    rect.y = Screen.height - (pos.y * scale);
                    rect.x = pos.x - rect.width - 10.0f;
                }
                else
                {
                    rect.y = Screen.height - (pos.y * scale) - 10.0f - rect.height;
                    rect.x = pos.x + (mButtonImg.rectTransform.rect.width * scale) - rect.width;
                }				

				return rect;
			}
        }

        public FocusOverlay()
        {
			// Load texture on create, removal of the old Textures class
			satellite = RTUtil.LoadImage("texSatellite");

			// New AppLauncher Button instead of floating satellite button
			var actives = KSP.UI.Screens.ApplicationLauncher.AppScenes.TRACKSTATION | KSP.UI.Screens.ApplicationLauncher.AppScenes.MAPVIEW;
			mButton = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(OnButtonDown, OnButtonUp, null, null, null, null, actives, satellite);
			mButtonImg = mButton.GetComponent<UnityEngine.UI.Image>();

			MapView.OnEnterMapView += OnEnterMapView;
			MapView.OnExitMapView += OnExitMapView;
		}

        public void Dispose()
        {
            MapView.OnEnterMapView -= OnEnterMapView;
            MapView.OnExitMapView -= OnExitMapView;

			// Remove button on destroy
			KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication(mButton);
		}

        public void OnEnterMapView()
        {
            RTCore.Instance.OnGuiUpdate += Draw;
            mFocus.resetSelection();
        }

        public void OnExitMapView()
        {
            RTCore.Instance.OnGuiUpdate -= Draw;
        }

		// Button states for applauncher
        private void OnButtonUp()
        {
            mShowOverlay = false;
        }

        private void OnButtonDown()
        {
            mShowOverlay = true;
        }

		// Fixed drawing mechanics
        public void Draw()
        {
			if (!mShowOverlay) return;

			GUILayout.BeginArea(PositionFrame);
			{
				mFocus.Draw();
			}
			GUILayout.EndArea();
		}
    }
}