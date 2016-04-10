using System;
using System.Linq;
using RemoteTech.SimpleTypes;
using UnityEngine;
using KSP.UI;

namespace RemoteTech.UI
{
    public class TimeWarpDecorator
    {
        /// <summary>
        /// Image for position access
        /// </summary>
        private UnityEngine.UI.Image mTimewarpImage;

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
                    return "N/A";
                }
                else if (vs.HasLocalControl)
                {
                    return "Local Control";
                }
                else if (vs.Connections.Any())
                {
                    if (RTSettings.Instance.EnableSignalDelay)
                    {
                        return "D+ " + vs.Connections[0].Delay.ToString("F5") + "s";
                    }
                    else
                    {
                        return "Connected";
                    }                    
                }
                return "No Connection";
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
                else if (vs.HasLocalControl)
                {
                    return mFlightButtonYellow;
                }
                else if (vs.Connections.Any())
                {
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
                mTimewarpImage = go.GetComponent<UnityEngine.UI.Image>();

            // objects on this scene?
            if (mTimewarpImage == null)
            {
                return;
            }

            if(TimeWarp.fetch != null && TimeWarp.fetch.timeQuadrantTab != null)
            {
                var text = TimeWarp.fetch.timeQuadrantTab.transform.FindChild("MET timer").GetComponent<ScreenSafeGUIText>();
                mTextStyle = new GUIStyle(text.textStyle);
                mTextStyle.fontSize = (int)(text.textSize * ScreenSafeUI.PixelRatio);
            }         
        }

        /// <summary>
        /// Draws the TimeQuadrantFcStatus.png, Delay time and the Flightcomputerbutton under the timewarp object
        /// </summary>
        public void Draw()
        {
            // no drawing without timewarp object
            if (mTimewarpImage == null)
                return;

            Vector2 screenCoord = UIMainCamera.Camera.WorldToScreenPoint(mTimewarpImage.rectTransform.position);

            float scale = GameSettings.UI_SCALE;
            float topLeftTotimeQuadrant = Screen.height - (screenCoord.y - (mTimewarpImage.preferredHeight * scale));
            float texBackgroundHeight = (mTexBackground.height * 0.7f) * scale;
            float texBackgroundWidth = (mTexBackground.width * 0.8111f) * scale;

            Rect delaytextPosition = new Rect((screenCoord.x + 12.0f) * scale, topLeftTotimeQuadrant + 2 * scale, 50.0f * scale, 20.0f * scale);

            // calc the position under the timewarp object
            Rect pos = new Rect(screenCoord.x,
                                topLeftTotimeQuadrant,
                                texBackgroundWidth, texBackgroundHeight);

            // draw the image
            GUI.DrawTexture(pos, mTexBackground);

            // draw the delay-text
            GUI.Label(delaytextPosition, DisplayText, mTextStyle);

            // draw the flightcomputer button to the right relative to the delaytext position
            Rect btnPos = new Rect((pos.x + 130.0f) * scale, topLeftTotimeQuadrant + 2 * scale, 21.0f * scale, 21.0f * scale);

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
