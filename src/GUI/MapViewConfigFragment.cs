using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class MapViewConfigFragment : IFragment, IDisposable {
        private static class Texture {
            public const int Background = 0;
            public const int Path       = 1;
            public const int Any        = 2;
            public const int Dish       = 3;
            public const int Omni       = 4;
            public const int OmniDish   = 5;
            public const int Empty      = 6;
        }

        private static readonly Texture2D[] mTextures;
        private static readonly GUIStyle mStyleButton;
        private static readonly GUIStyle mStyleButtonGray;
        private static readonly GUIStyle mStyleButtonGreen;
        private static readonly GUIStyle mStyleButtonRed;
        private static readonly GUIStyle mStyleButtonYellow;

        private VesselSatellite mSatellite;
        private SatelliteConfigWindow mConfig;

        static MapViewConfigFragment() {
            mTextures = new Texture2D[7];
            RTUtil.LoadImage(out mTextures[0], "texBackground.png");
            RTUtil.LoadImage(out mTextures[1], "texPath.png");
            RTUtil.LoadImage(out mTextures[2], "texAll.png");
            RTUtil.LoadImage(out mTextures[3], "texDish.png");
            RTUtil.LoadImage(out mTextures[4], "texOmni.png");
            RTUtil.LoadImage(out mTextures[5], "texOmniDish.png");
            RTUtil.LoadImage(out mTextures[6], "texEmpty.png");

            mStyleButton = GUITextureButtonFactory.CreateFromFilename("texButton.png");
            mStyleButtonGray = GUITextureButtonFactory.CreateFromFilename("texButtonGray.png");
            mStyleButtonGreen = GUITextureButtonFactory.CreateFromFilename("texButtonGreen.png");
            mStyleButtonRed = GUITextureButtonFactory.CreateFromFilename("texButtonRed.png");
            mStyleButtonYellow = GUITextureButtonFactory.CreateFromFilename("texButtonYellow.png");

            RTUtil.Log("MapViewConfigFragment has loaded textures.");
        }

        private Rect Position {
            get {
                return new Rect(Screen.width - mTextures[Texture.Background].width,
                                Screen.height - mTextures[Texture.Background].height,
                                mTextures[Texture.Background].width,
                                mTextures[Texture.Background].height);
            }
        }

        private Texture2D TextureComButton {
            get {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Any) == MapFilter.Any)
                    return mTextures[Texture.Any];
                if ((mask & MapFilter.OnlyPath) == MapFilter.OnlyPath)
                    return mTextures[Texture.Path];
                return mTextures[Texture.Empty];
            }
        }

        private Texture2D TextureTypeButton {
            get {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.OmniDish) == MapFilter.OmniDish)
                    return mTextures[Texture.OmniDish];
                if ((mask & MapFilter.Omni) == MapFilter.Omni)
                    return mTextures[Texture.Omni];
                if ((mask & MapFilter.Dish) == MapFilter.Dish)
                    return mTextures[Texture.Dish];
                return mTextures[Texture.Empty];
            }
        }

        private GUIStyle StyleStatusButton {
            get {
                if (mSatellite == null)
                    return mStyleButtonGray;
                if (mSatellite.Connection.Exists)
                    return mStyleButtonGreen;
                if (mSatellite.LocalControl) 
                    return mStyleButtonYellow;
                return mStyleButtonRed;
            }
        }

        public MapViewConfigFragment() {
            GameEvents.onPlanetariumTargetChanged.Add(ChangeTarget);
            MapView.OnExitMapView += OnExitMapView;
            EZGUIPointerDisablePatcher.Register(GetArea);
        }

        public void Dispose() {
            GameEvents.onPlanetariumTargetChanged.Remove(ChangeTarget);
            MapView.OnExitMapView -= OnExitMapView;
            EZGUIPointerDisablePatcher.Unregister(GetArea);
        }

        public void OnExitMapView() {
            if(mConfig != null) {
                mConfig.Hide();
            }
        }

        public void Draw() {
            GUI.depth = 0;
            GUILayout.BeginArea(Position, mTextures[Texture.Background]);
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(TextureComButton, mStyleButton))
                        OnClickCompath();
                    if (GUILayout.Button(TextureTypeButton, mStyleButton))
                        OnClickType();
                    if (GUILayout.Button("", StyleStatusButton))
                        OnClickStatus();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void ChangeTarget(MapObject mo) {
            if (mo != null && mo.type == MapObject.MapObjectType.VESSEL) {
                mSatellite = RTCore.Instance.Satellites[mo.vessel];
            } else {
                mSatellite = null;
            }
            if (mConfig != null) {
                mConfig.Hide();
            }
            if (mSatellite != null) {
                mConfig = new SatelliteConfigWindow(null, new Rect(0,0,0,0), 
                    WindowAlign.BottomRight, mSatellite);
            }
        }

        private void OnClickCompath() {
            MapFilter mask = RTCore.Instance.Renderer.Filter;
            if ((mask & MapFilter.OnlyPath) == MapFilter.OnlyPath) {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.OnlyPath;
                RTCore.Instance.Renderer.Filter |= MapFilter.Any;
                return;
            }
            if ((mask & MapFilter.Any) == MapFilter.Any) {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.Any;
                RTCore.Instance.Renderer.Filter |= MapFilter.None;
                return;
            }
            if ((mask & MapFilter.None) == MapFilter.None) {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.None;
                RTCore.Instance.Renderer.Filter |= MapFilter.OnlyPath;
                return;
            }
            RTCore.Instance.Renderer.Filter |= MapFilter.OnlyPath;
        }

        private void OnClickType() {
            MapFilter mask = RTCore.Instance.Renderer.Filter;
            if ((mask & MapFilter.OmniDish) == MapFilter.OmniDish) {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.OmniDish;
                RTCore.Instance.Renderer.Filter |= MapFilter.Omni;
                return;
            }
            if ((mask & MapFilter.Omni) == MapFilter.Omni) {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.Omni;
                RTCore.Instance.Renderer.Filter |= MapFilter.Dish;
                return;
            }
            if ((mask & MapFilter.Dish) == MapFilter.Dish) {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.Dish;
                RTCore.Instance.Renderer.Filter |= MapFilter.OmniDish;
                return;
            }
            RTCore.Instance.Renderer.Filter |= MapFilter.OmniDish;
        }

        private void OnClickStatus() {
            if (mConfig.Enabled) {
                mConfig.Hide();
            } else if (StyleStatusButton != mStyleButtonRed && StyleStatusButton != mStyleButtonGray) {
                mConfig.Show();
            }
        }

        private Rect GetArea() {
            return Position;
        }
    }
}
