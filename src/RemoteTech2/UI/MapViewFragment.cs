using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class MapViewConfigFragment : IFragment, IDisposable
    {
        private static class Texture
        {
            public static readonly Texture2D Background;
            public static readonly Texture2D Path;
            public static readonly Texture2D Any;
            public static readonly Texture2D Dish;
            public static readonly Texture2D Omni;
            public static readonly Texture2D OmniDish;
            public static readonly Texture2D Empty;
            public static readonly Texture2D Planet;
            public static readonly Texture2D Satellite;

            static Texture()
            {
                RTUtil.LoadImage(out Background, "texBackground.png");
                RTUtil.LoadImage(out Path, "texPath.png");
                RTUtil.LoadImage(out Any, "texAll.png");
                RTUtil.LoadImage(out Dish, "texDish.png");
                RTUtil.LoadImage(out Omni, "texOmni.png");
                RTUtil.LoadImage(out OmniDish, "texOmniDish.png");
                RTUtil.LoadImage(out Empty, "texEmpty.png");
                RTUtil.LoadImage(out Planet, "texPlanet.png");
                RTUtil.LoadImage(out Satellite, "texSatellite.png");
            }
        }

        private static class Style
        {
            public static readonly GUIStyle Standard;
            public static readonly GUIStyle StandardGray;
            public static readonly GUIStyle StandardGreen;
            public static readonly GUIStyle StandardRed;
            public static readonly GUIStyle StandardYellow;
            public static readonly GUIStyle Focus;

            static Style()
            {
                Standard = GUITextureButtonFactory.CreateFromFilename("texButton.png");
                StandardGray = GUITextureButtonFactory.CreateFromFilename("texButtonGray.png");
                StandardGreen = GUITextureButtonFactory.CreateFromFilename("texButtonGreen.png");
                StandardRed = GUITextureButtonFactory.CreateFromFilename("texButtonRed.png");
                StandardYellow = GUITextureButtonFactory.CreateFromFilename("texButtonYellow.png");
                Focus = GUITextureButtonFactory.CreateFromFilename("texKnowledgeNormal.png", "texKnowledgeHover.png", "texKnowledgeActive.png", "texKnowledgeHover.png");
            }
        }

        private SatelliteWindow mConfig = new SatelliteWindow(null);
        private FocusWindow mFocus = new FocusWindow();

        private Rect PositionFilter
        {
            get
            {
                return new Rect(Screen.width - Texture.Background.width,
                                Screen.height - Texture.Background.height,
                                Texture.Background.width,
                                Texture.Background.height);
            }
        }

        private Rect PositionFocus
        {
            get
            {
                if (!KnowledgeBase.Instance) return new Rect(0, 0, 0, 0);
                var position = KnowledgeBase.Instance.KnowledgeContainer.transform.position;
                return new Rect(Screen.width - Texture.Satellite.width + (position.x - 613.5f),
                                250 + 2 * 31,
                                Texture.Satellite.width,
                                Texture.Satellite.height);
            }
        }

        private Texture2D TextureComButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Path) == MapFilter.Path)
                    return Texture.Path;
                return Texture.Empty;
            }
        }

        private Texture2D TexturePlanetButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Planet) == MapFilter.Planet)
                    return Texture.Planet;
                return Texture.Empty;
            }
        }

        private Texture2D TextureTypeButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & (MapFilter.Omni | MapFilter.Dish)) == (MapFilter.Omni | MapFilter.Dish))
                    return Texture.OmniDish;
                if ((mask & MapFilter.Omni) == MapFilter.Omni)
                    return Texture.Omni;
                if ((mask & MapFilter.Dish) == MapFilter.Dish)
                    return Texture.Dish;
                return Texture.Empty;
            }
        }

        private GUIStyle StyleStatusButton
        {
            get
            {
                if (mConfig.Satellite == null)
                    return Style.StandardGray;
                if (RTCore.Instance.Network[mConfig.Satellite].Any())
                    return Style.StandardGreen;
                if (mConfig.Satellite.HasLocalControl)
                    return Style.StandardYellow;
                return Style.StandardRed;
            }
        }

        public MapViewConfigFragment()
        {
            GameEvents.onPlanetariumTargetChanged.Add(OnChangeTarget);
            MapView.OnExitMapView += OnExitMapView;
        }

        public void Dispose()
        {
            GameEvents.onPlanetariumTargetChanged.Remove(OnChangeTarget);
            MapView.OnExitMapView -= OnExitMapView;
        }

        public void OnExitMapView()
        {
            mConfig.Hide();
            mFocus.Hide();
        }

        public void Draw()
        {
            GUI.depth = 0;
            GUILayout.BeginArea(PositionFilter, Texture.Background);
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(TextureComButton, Style.Standard))
                        OnClickCompath();
                    if (GUILayout.Button(TexturePlanetButton, Style.Standard))
                        OnClickPlanet();
                    if (GUILayout.Button(TextureTypeButton, Style.Standard))
                        OnClickType();
                    if (GUILayout.Button("", StyleStatusButton))
                        OnClickStatus();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(PositionFocus);
            {
                if (GUILayout.Toggle(mFocus.Enabled, Texture.Satellite, Style.Focus))
                    OnClickFocus();
            }
            GUILayout.EndArea();
        }

        private void OnChangeTarget(MapObject mo)
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

        private void OnClickFocus()
        {
            if (mFocus.Enabled)
            {
                mFocus.Hide();
            }
            else
            {
                mFocus.Show();
            }
        }

        private void OnClickStatus()
        {
            if (mConfig.Enabled)
            {
                mConfig.Hide();
            }
            else if (StyleStatusButton != Style.StandardRed && StyleStatusButton != Style.StandardGray)
            {
                mConfig.Show();
            }
        }
    }
}