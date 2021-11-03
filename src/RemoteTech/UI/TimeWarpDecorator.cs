using System;
using System.Linq;
using RemoteTech.SimpleTypes;
using UnityEngine;
using KSP.UI;
using KSP.Localization;

namespace RemoteTech.UI
{
    /// <summary>
    /// Class used to display and handle the status quadrant (time delay) and the flight computer button.
    /// This is located right below the time warp UI, hence its name.
    /// </summary>
    public class TimeWarpDecorator
    {
        /// <summary>
        /// Image for position access
        /// </summary>
        private UnityEngine.RectTransform mTimewarpTransform;
        /// <summary>
        /// Delay-Text style
        /// </summary>
        private GUIStyle mTextStyle;
        /// <summary>
        /// Green Flightcomputer button
        /// </summary>
        private readonly GUIStyle mFlightButtonGreen;
        /// <summary>
        /// Red Flightcomputer button
        /// </summary>
        private readonly GUIStyle mFlightButtonRed;
        /// <summary>
        /// Yellow Flightcomputer button
        /// </summary>
        private readonly GUIStyle mFlightButtonYellow;
        /// <summary>
        /// Delay-Timer Background
        /// </summary>
        private readonly Texture2D mTexBackground;
        /// <summary>
        /// Activ Vessel
        /// </summary>
        private VesselSatellite mVessel { get{  return RTCore.Instance.Satellites[FlightGlobals.ActiveVessel]; }  }

        private String DisplayText
        {
            get
            {
                var vs = this.mVessel;
                if (vs == null)
                {
                    return Localizer.Format("#RT_ConnectionStatus1");//"N/A"
                }
                else if (vs.HasLocalControl)
                {
                    return Localizer.Format("#RT_ConnectionStatus2");//"Local Control"
                }
                else if (vs.Connections.Any())
                {
                    if (RTSettings.Instance.EnableSignalDelay)
                    {
                        return Localizer.Format("#RT_ConnectionStatus3",vs.Connections[0].Delay.ToString("F5"));//"D+ " +  + "s"
                    }
                    else
                    {
                        return Localizer.Format("#RT_ConnectionStatus4");//"Connected"
                    }                    
                }
                return Localizer.Format("#RT_ConnectionStatus5");//"No Connection"
            }
        }

        /// <summary>
        /// Returns the Style of the Flightcomputer button by the connected status
        /// </summary>
        private GUIStyle ButtonStyle
        {
            get
            {
                var vs = this.mVessel;
                if (vs == null) 
                {
                    return mFlightButtonRed;
                }

                else if (vs.Connections.Any())
                {
                    if (vs.HasLocalControl)
                        return mFlightButtonYellow;

                    return mFlightButtonGreen;
                }
                return mFlightButtonRed;
            }
        }

        public TimeWarpDecorator()
        {
            mFlightButtonGreen = GUITextureButtonFactory.CreateFromFilename("texFlightGreen", "texFlightGreenOver", "texFlightGreenDown", "texFlightGreenOver");
            mFlightButtonYellow = GUITextureButtonFactory.CreateFromFilename("texFlightYellow", "texFlightYellowOver", "texFlightYellowDown", "texFlightYellowOver");
            mFlightButtonRed = GUITextureButtonFactory.CreateFromFilename("texFlightRed", "texFlightRed", "texFlightRed", "texFlightRed");

            mFlightButtonGreen.fixedHeight = mFlightButtonGreen.fixedWidth = 0;
            mFlightButtonYellow.fixedHeight = mFlightButtonYellow.fixedWidth = 0;
            mFlightButtonRed.fixedHeight = mFlightButtonRed.fixedWidth = 0;
            mFlightButtonGreen.stretchHeight = mFlightButtonGreen.stretchWidth = true;
            mFlightButtonYellow.stretchHeight = mFlightButtonYellow.stretchWidth = true;
            mFlightButtonRed.stretchHeight = mFlightButtonRed.stretchWidth = true;

            // create the Background
            RTUtil.LoadImage(out mTexBackground, "TimeQuadrantFcStatus");

            // Get the image for positioning the decorator
            GameObject go = GameObject.Find("TimeQuadrant");
            if (go)
            {
                mTimewarpTransform = go.GetComponent<UnityEngine.RectTransform>();
            }

            // objects on this scene?
            if (mTimewarpTransform == null || TimeWarp.fetch == null)
            {
                return;
            }

            // create a style (for the connection / delay text) from the high logic skin label style.
            var skin = UnityEngine.Object.Instantiate(HighLogic.Skin);
            mTextStyle = new GUIStyle(skin.label);
            mTextStyle.alignment = TextAnchor.MiddleLeft;
            mTextStyle.wordWrap = false;
            mTextStyle.font = skin.font;
        }

        /// <summary>
        /// Draws the TimeQuadrantFcStatus.png, Delay time and the Flightcomputerbutton under the timewarp object
        /// </summary>
        public void Draw()
        {
            // no drawing without timewarp object
            if (mTimewarpTransform == null)
                return;

            // no drawing if in-flight action group panel is opened
            if(ActionGroupsFlightController.Instance != null && ActionGroupsFlightController.Instance.IsOpen)
                return;

            // no drawing if in-flight construction panel is opened
            if (EVAConstructionModeController.Instance != null && EVAConstructionModeController.Instance.IsOpen)
                return;

            Vector2 timeWarpImageScreenCoord = UIMainCamera.Camera.WorldToScreenPoint(mTimewarpTransform.position);

            float scale = GameSettings.UI_SCALE_TIME * GameSettings.UI_SCALE;
            float YToTimeQuadrant = Screen.height - (timeWarpImageScreenCoord.y - (mTimewarpTransform.rect.height * scale));
            float XToTimeQuadrant = 0; //compensated for KSP 1.12 alarm button
            float texBackgroundHeight = (mTexBackground.height * 0.7f) * scale;
            float texBackgroundWidth = (mTexBackground.width * 0.8111f) * scale;

            Rect delaytextPosition = new Rect((XToTimeQuadrant + 12.0f) * scale, YToTimeQuadrant + 2 * scale, 50.0f * scale, 20.0f * scale);

            // calc the position under the timewarp object
            Rect pos = new Rect(XToTimeQuadrant, YToTimeQuadrant,
                                texBackgroundWidth, texBackgroundHeight);

            // draw the image
            GUI.DrawTexture(pos, mTexBackground);

            // get color for the delay-text
            mTextStyle.normal.textColor = new Color(0.56078f, 0.10196f, 0.07450f);
            if (this.mVessel != null && this.mVessel.Connections.Any())
            {
                mTextStyle.normal.textColor = XKCDColors.GreenApple;
            }

            // draw connection / delay text
            mTextStyle.fontSize = (int)(mTextStyle.font.fontSize * scale);
            GUI.Label(delaytextPosition, DisplayText, mTextStyle);

            // draw the flightcomputer button to the right relative to the delaytext position
            Rect btnPos = new Rect((pos.x + 130.0f) * scale, YToTimeQuadrant + 2 * scale, 21.0f * scale, 21.0f * scale);

            GUILayout.BeginArea(btnPos);
            if (GUILayout.Button("", ButtonStyle))
            {
                var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (satellite == null || satellite.SignalProcessor.FlightComputer == null) return;
                satellite.SignalProcessor.FlightComputer.Window.Show();
            }
            GUILayout.EndArea();
        }
    }
}
