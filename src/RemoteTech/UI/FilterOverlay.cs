using System;
using System.Linq;
using UnityEngine;

namespace RemoteTech
{
    public class FilterOverlay : IFragment, IDisposable
    {
        private static class Texture
        {
            public static readonly Texture2D Background;
            public static readonly Texture2D BackgroundLeft;
            public static readonly Texture2D NoPath;
            public static readonly Texture2D Path;
            public static readonly Texture2D NoOmniDish;
            public static readonly Texture2D Dish;
            public static readonly Texture2D Omni;
            public static readonly Texture2D OmniDish;
            public static readonly Texture2D NoCone;
            public static readonly Texture2D Cone;

            static Texture()
            {
                RTUtil.LoadImage(out Background, "texBackground.png");
                RTUtil.LoadImage(out BackgroundLeft, "texBackground_left.png");
                RTUtil.LoadImage(out NoPath, "texNoPath.png");
                RTUtil.LoadImage(out Path, "texPath.png");
                RTUtil.LoadImage(out NoOmniDish, "texNoOmniDish.png");
                RTUtil.LoadImage(out Dish, "texDish.png");
                RTUtil.LoadImage(out Omni, "texOmni.png");
                RTUtil.LoadImage(out OmniDish, "texOmniDish.png");
                RTUtil.LoadImage(out NoCone, "texNoCone.png");
                RTUtil.LoadImage(out Cone, "texCone.png");
            }
        }

        private static class Style
        {
            public static readonly GUIStyle Button;
            public static readonly GUIStyle ButtonGray;
            public static readonly GUIStyle ButtonGreen;
            public static readonly GUIStyle ButtonRed;
            public static readonly GUIStyle ButtonYellow;

            static Style()
            {
                Button = GUITextureButtonFactory.CreateFromFilename("texButton.png");
                ButtonGray = GUITextureButtonFactory.CreateFromFilename("texButtonGray.png");
                ButtonGreen = GUITextureButtonFactory.CreateFromFilename("texButtonGreen.png");
                ButtonRed = GUITextureButtonFactory.CreateFromFilename("texButtonRed.png");
                ButtonYellow = GUITextureButtonFactory.CreateFromFilename("texButtonYellow.png");
            }
        }

        private SatelliteFragment mSatelliteFragment = new SatelliteFragment(null);
        private AntennaFragment mAntennaFragment = new AntennaFragment(null);
        private TargetInfoWindow mTargetInfos;
        private bool mEnabled;
        private bool onTrackingStation { get { return (HighLogic.LoadedScene == GameScenes.TRACKSTATION); } }

        private Rect Position
        {
            get
            {
                int posX = Screen.width - Texture.Background.width;

                // mirror to the left side on the tracking station
                if (this.onTrackingStation)
                {
                    posX = 200;
                }

                return new Rect(posX,
                                Screen.height - Texture.Background.height,
                                Texture.Background.width,
                                Texture.Background.height);
            }
        }

        private Rect PositionSatellite
        {
            get
            {
                var width = 350;
                var height = 350;
                var posX = Screen.width - width;

                // mirror to the left side on the tracking station
                if (this.onTrackingStation)
                {
                    posX = 200;
                }

                return new Rect(posX,
                                Screen.height - height,
                                width,
                                height);
            }
        }

        private Rect PositionAntenna
        {
            get
            {
                var positionSatellite = PositionSatellite;
                var width = 350;
                var height = 350;
                var posX = PositionSatellite.x - width;

                // mirror to the left side on the tracking station
                if (this.onTrackingStation)
                {
                    posX = PositionSatellite.x + PositionSatellite.width;
                }

                return new Rect(posX,
                                Screen.height - height,
                                width,
                                height);
            }
        }

        private Texture2D TextureComButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Path) == MapFilter.Path)
                    return Texture.Path;
                else
                    return Texture.NoPath;
            }
        }

        private Texture2D TextureReachButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Cone) == MapFilter.Cone)
                    return Texture.Cone;
                else
                    return Texture.NoCone;
            }
        }

        private Texture2D TextureTypeButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & (MapFilter.Omni | MapFilter.Dish)) == (MapFilter.Omni | MapFilter.Dish))
                    return Texture.OmniDish;
                else if ((mask & MapFilter.Omni) == MapFilter.Omni)
                    return Texture.Omni;
                else if ((mask & MapFilter.Dish) == MapFilter.Dish)
                    return Texture.Dish;
                else
                    return Texture.NoOmniDish;
            }
        }

        private GUIStyle StyleStatusButton
        {
            get
            {
                var sat = mSatelliteFragment.Satellite;
                if (sat == null)
                    return Style.ButtonGray;
                if (RTCore.Instance.Network[sat].Any())
                    return Style.ButtonGreen;
                if (sat.HasLocalControl)
                    return Style.ButtonYellow;
                return Style.ButtonRed;
            }
        }

        public FilterOverlay()
        {
            GameEvents.onPlanetariumTargetChanged.Add(OnChangeTarget);
            MapView.OnEnterMapView += OnEnterMapView;
            MapView.OnExitMapView += OnExitMapView;
            /// Add the on mouse over event
            mAntennaFragment.onMouseOverListEntry += showTargetInfo;
            
            WindowAlign targetInfoAlign = WindowAlign.TopLeft;
            if (this.onTrackingStation)
            {
                // switch to the other side if we are at the trackingStation
                targetInfoAlign = WindowAlign.TopRight;
            }

            /// Create a new Targetinfo window with a fixed position to the antenna fragment
            mTargetInfos = new TargetInfoWindow(PositionAntenna, targetInfoAlign);
            
        }

        public void Dispose()
        {
            /// Remove the on mouse over event
            mAntennaFragment.onMouseOverListEntry -= showTargetInfo;

            GameEvents.onPlanetariumTargetChanged.Remove(OnChangeTarget);
            MapView.OnEnterMapView -= OnEnterMapView;
            MapView.OnExitMapView -= OnExitMapView;
            mSatelliteFragment.Dispose();
            mAntennaFragment.Dispose();
        }

        public void OnEnterMapView()
        {
            RTCore.Instance.OnGuiUpdate += Draw;
            RTCore.Instance.OnFrameUpdate += Update;
        }

        public void OnExitMapView()
        {
            RTCore.Instance.OnGuiUpdate -= Draw;
            RTCore.Instance.OnFrameUpdate -= Update;
        }

        public void Update()
        {
            mAntennaFragment.Antenna = mSatelliteFragment.Antenna;
            var sat = mSatelliteFragment.Satellite;
            if (sat == null) return;
            if (!RTCore.Instance.Network[sat].Any() && !sat.HasLocalControl)
            { 
                mSatelliteFragment.Satellite = null;
                mAntennaFragment.Antenna = null;
            }
        }

        /// <summary>
        /// Mouse over callback forced by the mAntennaFragment
        /// </summary>
        public void showTargetInfo()
        {
            if (mAntennaFragment.mouseOverEntry != null)
            {
                // set the current selected target to the targetwindow
                mTargetInfos.setTarget(mAntennaFragment.mouseOverEntry, mAntennaFragment.Antenna);
                mTargetInfos.Show();
            }
            else
            {
                // hide if we do not have any selection
                mTargetInfos.Hide();
            }
        }

        public void Draw()
        {
            GUI.depth = 0;
            GUI.skin = HighLogic.Skin;

            // Draw Satellite Selector
            if (mEnabled && mSatelliteFragment.Satellite != null)
            {
                GUILayout.BeginArea(PositionSatellite, AbstractWindow.Frame);
                {
                    mSatelliteFragment.Draw();
                }
                GUILayout.EndArea();
            }

            // Hide the targetInfoWindow if we don't have a selected antenna
            if (mAntennaFragment.Antenna == null)
            {
                mTargetInfos.Hide();
            }

            // Draw Antenna Selector
            if (mEnabled && mSatelliteFragment.Satellite != null && mAntennaFragment.Antenna != null)
            {
                mAntennaFragment.triggerMouseOverListEntry = PositionAntenna.Contains(Event.current.mousePosition);
                GUILayout.BeginArea(PositionAntenna, AbstractWindow.Frame);
                {
                    mAntennaFragment.Draw();
                }
                GUILayout.EndArea();
            }

            
            // Switch the background from map view to tracking station
            Texture2D backgroundImage = Texture.Background;
            if(this.onTrackingStation)
            {
                backgroundImage = Texture.BackgroundLeft;
            }

            // Draw Toolbar
            GUILayout.BeginArea(Position, backgroundImage);
            {
                GUILayout.BeginHorizontal();
                {
                    if (this.onTrackingStation)
                    {
                        if (GUILayout.Button("", StyleStatusButton))
                            OnClickStatus();
                        if (GUILayout.Button(TextureTypeButton, Style.Button))
                            OnClickType();
                        if (GUILayout.Button(TextureReachButton, Style.Button))
                            OnClickReach();
                        if (GUILayout.Button(TextureComButton, Style.Button))
                            OnClickCompath();
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(TextureComButton, Style.Button))
                            OnClickCompath();
                        if (GUILayout.Button(TextureReachButton, Style.Button))
                            OnClickReach();
                        if (GUILayout.Button(TextureTypeButton, Style.Button))
                            OnClickType();
                        if (GUILayout.Button("", StyleStatusButton))
                            OnClickStatus();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void OnChangeTarget(MapObject mo)
        {
            if (mo != null && mo.type == MapObject.MapObjectType.VESSEL)
            {
                mSatelliteFragment.Satellite = RTCore.Instance.Satellites[mo.vessel];
            }
            else if (FlightGlobals.ActiveVessel != null)
            {
                mSatelliteFragment.Satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel.id];
            }
            else
            {
                mSatelliteFragment.Satellite = null;
            }
            mAntennaFragment.Antenna = null;
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

        private void OnClickReach()
        {
            MapFilter mask = RTCore.Instance.Renderer.Filter;
            if ((mask & MapFilter.Cone) == MapFilter.Cone)
            {
                RTCore.Instance.Renderer.Filter &= ~MapFilter.Cone;
                return;
            }
            RTCore.Instance.Renderer.Filter |= MapFilter.Cone;
        }

        private void OnClickStatus()
        {
            if (mEnabled)
            {
                mEnabled = false;
            }
            else if (StyleStatusButton != Style.ButtonRed && StyleStatusButton != Style.ButtonGray)
            {
                mEnabled = true;
            }
        }
    }
}