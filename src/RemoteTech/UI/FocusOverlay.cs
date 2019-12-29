using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RemoteTech.UI
{
    /// <summary>
    /// Class handling the "focus view" in the Tracking station scene.
    /// </summary>
    public class FocusOverlay : IFragment, IDisposable
    {
        private FocusFragment mFocus = new FocusFragment();
        private bool mShowOverlay = false;
        private Texture2D satellite;
        private bool inMapView = false;

        private KSP.UI.Screens.ApplicationLauncherButton mButton;
        private UnityEngine.UI.Image mButtonImg;
        public List<TrackingButton> mTrackButtonListener = new List<TrackingButton>();

        public struct TrackingButton
        {
            public UnityEngine.Events.UnityAction<bool> cb;
            public KSP.UI.Screens.TrackingStationWidget button;
        }
 
        private Rect PositionFrame
        {
            get
            {
                float scale = GameSettings.UI_SCALE;

                var pos = KSP.UI.UIMainCamera.Camera.WorldToScreenPoint(mButtonImg.rectTransform.position);

                var rect = new Rect(0, 0, 250, 500);
                if (HighLogic.LoadedSceneIsFlight)
                {
                    rect.y = Screen.height - pos.y;
                    rect.x = pos.x - rect.width - 10.0f;
                }
                else
                {
                    rect.y = Screen.height - pos.y - 10.0f - rect.height;
                    rect.x = pos.x + (mButtonImg.rectTransform.rect.width * scale) - rect.width;
                }

                return rect;
            }
        }

        public void RebuildTrackingListeners()
        {
            // Remove and clear just in case
            RemoveTrackingListeners();

            // Adds a click listener to all the tracking station objects
            var TSWList = UnityEngine.Object.FindObjectsOfType<KSP.UI.Screens.TrackingStationWidget>();
            foreach (var tsw in TSWList)
            {
                if (tsw)
                {
                    var tb = new TrackingButton();
                    tb.button = tsw;
                    tb.cb = (bool st) => { mFocus.setSelection(tb.button.vessel); };
                    tsw.toggle.onValueChanged.AddListener(tb.cb);
                    mTrackButtonListener.Add(tb);
                }
            }
        }

        public void RemoveTrackingListeners()
        {
            foreach (var tb in mTrackButtonListener)
            {
                if (tb.button)
                    tb.button.toggle.onValueChanged.RemoveListener(tb.cb);
            }
            mTrackButtonListener.Clear();
        }

        public FocusOverlay()
        {
            // Load texture on create, removal of the old Textures class
            satellite = RTUtil.LoadImage("texSatellite");

            // New AppLauncher Button instead of floating satellite button
            var actives = KSP.UI.Screens.ApplicationLauncher.AppScenes.TRACKSTATION | KSP.UI.Screens.ApplicationLauncher.AppScenes.MAPVIEW;
            mButton = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(OnButtonDown, OnButtonUp, null, null, null, null, actives, satellite);
            mButtonImg = mButton.GetComponent<UnityEngine.UI.Image>();

            RebuildTrackingListeners();

            MapView.OnEnterMapView += OnEnterMapView;
            MapView.OnExitMapView += OnExitMapView;
        }

        public void Dispose()
        {
            MapView.OnEnterMapView -= OnEnterMapView;
            MapView.OnExitMapView -= OnExitMapView;

            if (inMapView) 
            {
                // to let it clean up stuff, because we don't receive this event any more
                OnExitMapView();
            }

            // Remove button on destroy
            KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication(mButton);
            RemoveTrackingListeners();
        }

        // Rebuilds the tracking buttons only when a vessel is removed/terminated/recovered
        // Feels really hacky tracking button fix - find better solution
        public void Update()
        {
            RebuildTrackingListeners();
            RTCore.Instance.OnFrameUpdate -= Update;
        }
       
        public void OnVDestroy(Vessel v)
        {
            if(HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                RTCore.Instance.AddOnceOnFrameUpdate(Update);
        }
        public void OnVRecover(ProtoVessel v, bool t)
        {
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                RTCore.Instance.AddOnceOnFrameUpdate(Update);
        }
        public void OnVTerminate(ProtoVessel v)
        {
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                RTCore.Instance.AddOnceOnFrameUpdate(Update);
        }

        public void OnEnterMapView()
        {
            inMapView = true;
            RTCore.Instance.OnGuiUpdate += Draw;
            mFocus.resetSelection();

            GameEvents.onVesselRecovered.Add(OnVRecover);
            GameEvents.onVesselDestroy.Add(OnVDestroy);
            GameEvents.onVesselTerminated.Add(OnVTerminate);
        }

        public void OnExitMapView()
        {
            inMapView = false;
            RTCore.Instance.OnGuiUpdate -= Draw;

            GameEvents.onVesselRecovered.Remove(OnVRecover);
            GameEvents.onVesselDestroy.Remove(OnVDestroy);
            GameEvents.onVesselTerminated.Remove(OnVTerminate);
        }

        // Button states for applauncher
        private void OnButtonUp()
        {
            InputLockManager.RemoveControlLock("RTMapViewSatelliteList");
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
                // Refer to the idential codes in AbstractWindow.cs
                InputLockManager.RemoveControlLock("RTMapViewSatelliteList");
                if (this.PositionFrame.ContainsMouse())
                {
                    InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS | ControlTypes.MAP, "RTMapViewSatelliteList");
                }
                mFocus.Draw();
            }
            GUILayout.EndArea();
        }
    }
}