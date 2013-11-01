using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class MapViewConfigFragment : IFragment, IDisposable
    {
        private static class Texture
        {
            public const int Background = 0;
            public const int Path = 1;
            public const int Any = 2;
            public const int Dish = 3;
            public const int Omni = 4;
            public const int OmniDish = 5;
            public const int Empty = 6;
            public const int Planet = 7;
        }

        private static readonly Texture2D[] mTextures;
        private static readonly GUIStyle mStyleButton;
        private static readonly GUIStyle mStyleButtonGray;
        private static readonly GUIStyle mStyleButtonGreen;
        private static readonly GUIStyle mStyleButtonRed;
        private static readonly GUIStyle mStyleButtonYellow;

        private SatelliteWindow mConfig = new SatelliteWindow(null);

        static MapViewConfigFragment()
        {
            mTextures = new Texture2D[8];
            RTUtil.LoadImage(out mTextures[0], "texBackground.png");
            RTUtil.LoadImage(out mTextures[1], "texPath.png");
            RTUtil.LoadImage(out mTextures[2], "texAll.png");
            RTUtil.LoadImage(out mTextures[3], "texDish.png");
            RTUtil.LoadImage(out mTextures[4], "texOmni.png");
            RTUtil.LoadImage(out mTextures[5], "texOmniDish.png");
            RTUtil.LoadImage(out mTextures[6], "texEmpty.png");
            RTUtil.LoadImage(out mTextures[7], "texPlanet.png");

            mStyleButton = GUITextureButtonFactory.CreateFromFilename("texButton.png");
            mStyleButtonGray = GUITextureButtonFactory.CreateFromFilename("texButtonGray.png");
            mStyleButtonGreen = GUITextureButtonFactory.CreateFromFilename("texButtonGreen.png");
            mStyleButtonRed = GUITextureButtonFactory.CreateFromFilename("texButtonRed.png");
            mStyleButtonYellow = GUITextureButtonFactory.CreateFromFilename("texButtonYellow.png");
        }

        private Rect Position
        {
            get
            {
                return new Rect(Screen.width - mTextures[Texture.Background].width,
                                Screen.height - mTextures[Texture.Background].height,
                                mTextures[Texture.Background].width,
                                mTextures[Texture.Background].height);
            }
        }

        private Texture2D TextureComButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Path) == MapFilter.Path)
                    return mTextures[Texture.Path];
                return mTextures[Texture.Empty];
            }
        }

        private Texture2D TexturePlanetButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Planet) == MapFilter.Planet)
                    return mTextures[Texture.Planet];
                return mTextures[Texture.Empty];
            }
        }

        private Texture2D TextureTypeButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & (MapFilter.Omni | MapFilter.Dish)) == (MapFilter.Omni | MapFilter.Dish))
                    return mTextures[Texture.OmniDish];
                if ((mask & MapFilter.Omni) == MapFilter.Omni)
                    return mTextures[Texture.Omni];
                if ((mask & MapFilter.Dish) == MapFilter.Dish)
                    return mTextures[Texture.Dish];
                return mTextures[Texture.Empty];
            }
        }

        private GUIStyle StyleStatusButton
        {
            get
            {
                if (mConfig.Satellite == null)
                    return mStyleButtonGray;
                if (RTCore.Instance.Network[mConfig.Satellite].Any())
                    return mStyleButtonGreen;
                if (mConfig.Satellite.HasLocalControl)
                    return mStyleButtonYellow;
                return mStyleButtonRed;
            }
        }

        public MapViewConfigFragment()
        {
            GameEvents.onPlanetariumTargetChanged.Add(ChangeTarget);
            MapView.OnExitMapView += OnExitMapView;
        }

        public void Dispose()
        {
            GameEvents.onPlanetariumTargetChanged.Remove(ChangeTarget);
            MapView.OnExitMapView -= OnExitMapView;
        }

        public void OnExitMapView()
        {
            mConfig.Hide();
        }

        public void Draw()
        {
            GUI.depth = 0;
            GUILayout.BeginArea(Position, mTextures[Texture.Background]);
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(TextureComButton, mStyleButton))
                        OnClickCompath();
                    if (GUILayout.Button(TexturePlanetButton, mStyleButton))
                        OnClickPlanet();
                    if (GUILayout.Button(TextureTypeButton, mStyleButton))
                        OnClickType();
                    if (GUILayout.Button("", StyleStatusButton))
                        OnClickStatus();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void ChangeTarget(MapObject mo)
        {
            if (mo != null && mo.type == MapObject.MapObjectType.VESSEL)
            {
                mConfig.Satellite = RTCore.Instance.Satellites[mo.vessel];
            }
            else if (FlightGlobals.ActiveVessel != null)
            {
                mConfig.Satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel.id];
            }
            else
            {
                mConfig.Satellite = null;
            }
            mConfig.Hide();
        }

        private void OnClickCompath()
        {
            MapFilter mask = RTCore.Instance.Renderer.Filter;
            if ((mask & MapFilter.Path) == MapFilter.Path)
            {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.Path;
                return;
            }
            RTCore.Instance.Renderer.Filter |= MapFilter.Path;
        }

        private void OnClickType()
        {
            MapFilter mask = RTCore.Instance.Renderer.Filter;
            if ((mask & (MapFilter.Omni | MapFilter.Dish)) == (MapFilter.Omni | MapFilter.Dish))
            {
                RTCore.Instance.Renderer.Filter &= ~((MapFilter.Omni | MapFilter.Dish));
                return;
            }
            if ((mask & MapFilter.Omni) == MapFilter.Omni)
            {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.Omni;
                RTCore.Instance.Renderer.Filter |= MapFilter.Dish;
                return;
            }
            if ((mask & MapFilter.Dish) == MapFilter.Dish)
            {
                RTCore.Instance.Renderer.Filter |= (MapFilter.Omni | MapFilter.Dish);
                return;
            }
            RTCore.Instance.Renderer.Filter |= MapFilter.Omni;
        }

        private void OnClickPlanet()
        {
            MapFilter mask = RTCore.Instance.Renderer.Filter;
            if ((mask & MapFilter.Planet) == MapFilter.Planet)
            {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.Planet;
                return;
            }
            RTCore.Instance.Renderer.Filter |= MapFilter.Planet;
        }

        private void OnClickStatus()
        {
            if (mConfig.Enabled)
            {
                mConfig.Hide();
            }
            else if (StyleStatusButton != mStyleButtonRed && StyleStatusButton != mStyleButtonGray)
            {
                mConfig.Show();
            }
        }
    }
}