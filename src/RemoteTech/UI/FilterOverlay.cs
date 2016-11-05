using System;
using System.Linq;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTech.UI
{
    /// <summary>
    /// Class used for the buttons overlay in Tracking Station or Flight map scenes.
    /// Draws and handles buttons on the bottom right of the scene.
    /// </summary>
    public class FilterOverlay : IFragment, IDisposable
    {
        private class Texture
        {
            public Texture2D Background;
            public Texture2D BackgroundLeft;
            public Texture2D NoPath;
            public Texture2D Path;
            public Texture2D NoOmniDish;
            public Texture2D Dish;
            public Texture2D Omni;
            public Texture2D OmniDish;
            public Texture2D NoCone;
            public Texture2D Cone;
            public Texture2D SatButton;

            public void CreateTextures()
            {
                RTUtil.LoadImage(out Background, "texBackground");
                RTUtil.LoadImage(out BackgroundLeft, "texBackground_left");
                RTUtil.LoadImage(out NoPath, "texNoPath");
                RTUtil.LoadImage(out Path, "texPath");
                RTUtil.LoadImage(out NoOmniDish, "texNoOmniDish");
                RTUtil.LoadImage(out Dish, "texDish");
                RTUtil.LoadImage(out Omni, "texOmni");
                RTUtil.LoadImage(out OmniDish, "texOmniDish");
                RTUtil.LoadImage(out NoCone, "texNoCone");
                RTUtil.LoadImage(out Cone, "texCone");
                RTUtil.LoadImage(out SatButton, "texButtonGray");
            }
        }

        private SatelliteFragment mSatelliteFragment = new SatelliteFragment(null);
        private AntennaFragment mAntennaFragment = new AntennaFragment(null);
        private Texture mTextures = new Texture();

        private TargetInfoWindow mTargetInfos;
        private bool mEnabled;
        private bool mShowOverlay = true;
        private bool onTrackingStation { get { return (HighLogic.LoadedScene == GameScenes.TRACKSTATION); } }

        public static GUIStyle Button;
        public static GUIStyle ButtonGray;
        public static GUIStyle ButtonGreen;
        public static GUIStyle ButtonRed;
        public static GUIStyle ButtonYellow;

        private static UnityEngine.UI.Image mImg = null;

        private Rect Position
        {
            get
            {
                float posX = Screen.width - mTextures.Background.width * GameSettings.UI_SCALE;
                float posY = Screen.height - mTextures.Background.height * GameSettings.UI_SCALE;

                // mirror to the left side on the tracking station
                if (this.onTrackingStation)
                {
                    // New side bar location checking... if someone finds a better method for this please fix
                    if (mImg == null)
                        mImg = GameObject.Find("Side Bar").GetChild("bg (stretch)").GetComponent<UnityEngine.UI.Image>();

                    posX = mImg.rectTransform.rect.width * GameSettings.UI_SCALE;
                }

                return new Rect(posX, posY, mTextures.Background.width * GameSettings.UI_SCALE, mTextures.Background.height * GameSettings.UI_SCALE);
            }
        }

        private Rect PositionSatellite
        {
            get
            {
                float width = 350;
                float height = 350;
                float posX = Screen.width - width;

                // mirror to the left side on the tracking station
                if (this.onTrackingStation)
                {

                    // Same new side bar checking... if someone finds a better method for this please fix
                    if (mImg == null)
                        mImg = GameObject.Find("Side Bar").GetChild("bg (stretch)").GetComponent<UnityEngine.UI.Image>();
                    posX = mImg.rectTransform.rect.width * GameSettings.UI_SCALE;
                }

                return new Rect(posX, Screen.height - height, width, height);
            }

        }

        private Rect PositionAntenna
        {
            get
            {
                var width = 350;
                var height = 350;
                var posX = PositionSatellite.x - width;

                // mirror to the left side on the tracking station
                if (this.onTrackingStation)
                {
                    posX = PositionSatellite.x + PositionSatellite.width;
                }

                return new Rect(posX, Screen.height - height, width, height);
            }

        }

        private Texture2D TextureComButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Path) == MapFilter.Path)
                    return mTextures.Path;
                else
                    return mTextures.NoPath;
            }
        }

        private Texture2D TextureReachButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & MapFilter.Cone) == MapFilter.Cone)
                    return mTextures.Cone;
                else
                    return mTextures.NoCone;
            }
        }

        private Texture2D TextureTypeButton
        {
            get
            {
                MapFilter mask = RTCore.Instance.Renderer.Filter;
                if ((mask & (MapFilter.Omni | MapFilter.Dish)) == (MapFilter.Omni | MapFilter.Dish))
                    return mTextures.OmniDish;
                else if ((mask & MapFilter.Omni) == MapFilter.Omni)
                    return mTextures.Omni;
                else if ((mask & MapFilter.Dish) == MapFilter.Dish)
                    return mTextures.Dish;
                else
                    return mTextures.NoOmniDish;
            }
        }

        private GUIStyle StyleStatusButton
        {
            get
            {
                var sat = mSatelliteFragment.Satellite;
                if (sat == null)
                    return ButtonGray;
                if (RTCore.Instance.Network[sat].Any())
                    return ButtonGreen;
                if (sat.HasLocalControl)
                    return ButtonYellow;
                return ButtonRed;
            }
        }

        private void OnHideUI()
        {
            mShowOverlay = false;
        }

        private void OnShowUI()
        {
            mShowOverlay = true;
        }

        public FilterOverlay()
        {
            // loading styles
            mTextures.CreateTextures();
            Button = GUITextureButtonFactory.CreateFromFilename("texButton");
            ButtonGray = GUITextureButtonFactory.CreateFromFilename("texButtonGray");
            ButtonGreen = GUITextureButtonFactory.CreateFromFilename("texButtonGreen");
            ButtonRed = GUITextureButtonFactory.CreateFromFilename("texButtonRed");
            ButtonYellow = GUITextureButtonFactory.CreateFromFilename("texButtonYellow");

            GameEvents.onPlanetariumTargetChanged.Add(OnChangeTarget);
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            MapView.OnEnterMapView += OnEnterMapView;
            MapView.OnExitMapView += OnExitMapView;
            // Add the on mouse over event
            mAntennaFragment.onMouseOverListEntry += showTargetInfo;

            WindowAlign targetInfoAlign = WindowAlign.TopLeft;
            if (this.onTrackingStation)
            {
                // switch to the other side if we are at the trackingStation
                targetInfoAlign = WindowAlign.TopRight;
            }

            // Create a new Targetinfo window with a fixed position to the antenna fragment
            mTargetInfos = new TargetInfoWindow(PositionAntenna, targetInfoAlign);

        }

        public void Dispose()
        {
            // Remove the on mouse over event
            mAntennaFragment.onMouseOverListEntry -= showTargetInfo;

            GameEvents.onPlanetariumTargetChanged.Remove(OnChangeTarget);
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
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
                mTargetInfos.SetTarget(mAntennaFragment.mouseOverEntry, mAntennaFragment.Antenna);
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
            if (!mShowOverlay) return;
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
            mAntennaFragment.triggerMouseOverListEntry = PositionAntenna.Contains(Event.current.mousePosition) && mEnabled;

            if (mEnabled && mSatelliteFragment.Satellite != null && mAntennaFragment.Antenna != null)
            {
                GUILayout.BeginArea(PositionAntenna, AbstractWindow.Frame);
                {
                    mAntennaFragment.Draw();
                }
                GUILayout.EndArea();
            }

            
            // Switch the background from map view to tracking station
            Texture2D backgroundImage = mTextures.Background;
            if(this.onTrackingStation)
            {
                backgroundImage = mTextures.BackgroundLeft;
            }


            // TODO: Fix textures
            // Draw Toolbar
            GUI.DrawTexture(Position, backgroundImage);
            GUILayout.BeginArea(Position);
            {
                GUILayout.BeginHorizontal();
                {
                    if (this.onTrackingStation)
                    {
                        if (GUILayout.Button("", StyleStatusButton, GUILayout.Width(mTextures.SatButton.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.SatButton.height * GameSettings.UI_SCALE)))
                            OnClickStatus();
                        if (GUILayout.Button(TextureTypeButton, Button, GUILayout.Width(mTextures.OmniDish.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.OmniDish.height * GameSettings.UI_SCALE)))
                            OnClickType();
                        if (GUILayout.Button(TextureReachButton, Button, GUILayout.Width(mTextures.Cone.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.Cone.height * GameSettings.UI_SCALE)))
                            OnClickReach();
                        if (GUILayout.Button(TextureComButton, Button, GUILayout.Width(mTextures.Path.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.Path.height * GameSettings.UI_SCALE)))
                            OnClickCompath();
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(TextureComButton, Button, GUILayout.Width(mTextures.Path.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.Path.height * GameSettings.UI_SCALE)))
                            OnClickCompath();
                        if (GUILayout.Button(TextureReachButton, Button, GUILayout.Width(mTextures.Cone.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.Cone.height * GameSettings.UI_SCALE)))
                            OnClickReach();
                        if (GUILayout.Button(TextureTypeButton, Button, GUILayout.Width(mTextures.OmniDish.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.OmniDish.height * GameSettings.UI_SCALE)))
                            OnClickType();
                        if (GUILayout.Button("", StyleStatusButton, GUILayout.Width(mTextures.SatButton.width * GameSettings.UI_SCALE), GUILayout.Height(mTextures.SatButton.height * GameSettings.UI_SCALE)))
                            OnClickStatus();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void OnChangeTarget(MapObject mo)
        {
            if (mo != null && mo.type == MapObject.ObjectType.Vessel)
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
                mTargetInfos.Hide();
            }
            else if (StyleStatusButton != ButtonRed && StyleStatusButton != ButtonGray)
            {
                mEnabled = true;
            }
        }
    }
}