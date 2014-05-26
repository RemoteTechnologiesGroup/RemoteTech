﻿using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class FocusOverlay : IFragment, IDisposable
    {
        private static class Style
        {
            public static readonly GUIStyle Button;

            static Style()
            {
                Button = GUITextureButtonFactory.CreateFromTextures(Textures.KnowledgeButton,
                                                                    Textures.KnowledgeButtonHover,
                                                                    Textures.KnowledgeButtonActive,
                                                                    Textures.KnowledgeButtonHover);
            }
        }

        private FocusFragment mFocus = new FocusFragment();
        private bool mEnabled;

        private Rect PositionButton
        {
            get
            {
                if (!KnowledgeBase.Instance) return new Rect(0, 0, 0, 0);
                var position = KnowledgeBase.Instance.KnowledgeContainer.transform.position;
                var position2 = UIManager.instance.rayCamera.WorldToScreenPoint(position);
                var rect = new Rect(position2.x + 154,
                                250 + 2 * 31,
                                Textures.SatelliteIcon.width,
                                Textures.SatelliteIcon.height);
                return rect;
            }
        }

        private Rect PositionFrame
        {
            get
            {
                var rect = new Rect(0, 0, 250, 500);
                rect.y = PositionButton.y + PositionButton.height / 2 - rect.height / 2;
                rect.x = PositionButton.x - 5 - rect.width;
                return rect;
            }
        }

        public FocusOverlay()
        {
            MapView.OnEnterMapView += OnEnterMapView;
            MapView.OnExitMapView += OnExitMapView;
        }

        public void Dispose()
        {
            MapView.OnEnterMapView -= OnEnterMapView;
            MapView.OnExitMapView -= OnExitMapView;
        }

        public void OnEnterMapView()
        {
            RTCore.Instance.OnGuiUpdate += Draw;
        }

        public void OnExitMapView()
        {
            RTCore.Instance.OnGuiUpdate -= Draw;
        }

        public void Draw()
        {
            GUI.depth = 0;
            GUI.skin = HighLogic.Skin;

            GUILayout.BeginArea(PositionButton);
            {
                mEnabled = GUILayout.Toggle(mEnabled, Textures.SatelliteIcon, Style.Button);
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