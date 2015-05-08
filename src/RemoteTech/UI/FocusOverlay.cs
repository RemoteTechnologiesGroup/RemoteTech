﻿using System;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTech.UI
{
    public class FocusOverlay : IFragment, IDisposable
    {
        private static class Texture
        {
            public static readonly Texture2D Satellite;

            static Texture()
            {
                RTUtil.LoadImage(out Satellite, "texSatellite.png");
            }
        }
        
        private FocusFragment mFocus = new FocusFragment();
        private bool mEnabled;
        private bool mShowOverlay = true;
        public static GUIStyle Button;

        private Rect PositionButton
        {
            get
            {
                if (!KnowledgeBase.Instance) return new Rect(0, 0, 0, 0);
                var position = KnowledgeBase.Instance.KnowledgeContainer.transform.position;
                var position2 = UIManager.instance.rayCamera.WorldToScreenPoint(position);
                var rect = new Rect(position2.x + 154,
                                250 - 1 * 31,
                                Texture.Satellite.width,
                                Texture.Satellite.height);
                return rect;
            }
        }

        private Rect PositionFrame
        {
            get
            {
                var rect = new Rect(0, 0, 250, 500);
                rect.y = PositionButton.y;
                rect.x = PositionButton.x - 5 - rect.width;
                return rect;
            }
        }

        public FocusOverlay()
        {
            Button = GUITextureButtonFactory.CreateFromFilename("texKnowledgeNormal.png", "texKnowledgeHover.png", "texKnowledgeActive.png", "texKnowledgeHover.png");

            MapView.OnEnterMapView += OnEnterMapView;
            MapView.OnExitMapView += OnExitMapView;
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
        }

        public void Dispose()
        {
            MapView.OnEnterMapView -= OnEnterMapView;
            MapView.OnExitMapView -= OnExitMapView;
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
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

        private void OnHideUI()
        {
            mShowOverlay = false;
        }

        private void OnShowUI()
        {
            mShowOverlay = true;
        }

        public void Draw()
        {
            if (!mShowOverlay) return;
            GUI.depth = 0;
            GUI.skin = HighLogic.Skin;

            GUILayout.BeginArea(PositionButton);
            {
                mEnabled = GUILayout.Toggle(mEnabled, Texture.Satellite, Button);
            }
            GUILayout.EndArea();

            if (mEnabled)
            {
                GUILayout.BeginArea(PositionFrame);
                {
                    mFocus.Draw();
                }
                GUILayout.EndArea();
            }
        }
    }
}