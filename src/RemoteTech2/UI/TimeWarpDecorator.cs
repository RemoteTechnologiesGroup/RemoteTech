using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class TimeWarpDecorator
    {
        /// <summary>
        /// Craped Timewarpobject from stock
        /// </summary>
        private TimeWarp mTimewarpObject;
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
                        return "D+ " + vs.Connections[0].Delay.ToString("F6") + "s";
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
            mFlightButtonGreen = GUITextureButtonFactory.CreateFromFilename("texFlightGreen.png","texFlightGreenOver.png","texFlightGreenDown.png","texFlightGreenOver.png");
            mFlightButtonYellow = GUITextureButtonFactory.CreateFromFilename("texFlightYellow.png","texFlightYellowOver.png","texFlightYellowDown.png","texFlightYellowOver.png");
            mFlightButtonRed = GUITextureButtonFactory.CreateFromFilename("texFlightRed.png","texFlightRed.png","texFlightRed.png","texFlightRed.png");

            mFlightButtonGreen.fixedHeight = mFlightButtonGreen.fixedWidth = 0;
            mFlightButtonYellow.fixedHeight = mFlightButtonYellow.fixedWidth = 0;
            mFlightButtonRed.fixedHeight = mFlightButtonRed.fixedWidth = 0;
            mFlightButtonGreen.stretchHeight = mFlightButtonGreen.stretchWidth = true;
            mFlightButtonYellow.stretchHeight = mFlightButtonYellow.stretchWidth = true;
            mFlightButtonRed.stretchHeight = mFlightButtonRed.stretchWidth = true;

            // Crap timewarp object
            mTimewarpObject = TimeWarp.fetch;

            var text = mTimewarpObject.timeQuadrantTab.transform.FindChild("MET timer").GetComponent<ScreenSafeGUIText>();
            mTextStyle = new GUIStyle(text.textStyle);
            mTextStyle.fontSize = (int)(text.textSize * ScreenSafeUI.PixelRatio);

            // Put the draw function to the DrawQueue
            RenderingManager.AddToPostDrawQueue(0, Draw);

            // create the Background
            RTUtil.LoadImage(out mTexBackground, "TimeQuadrantFcStatus.png");
        }

        /// <summary>
        /// Draws the TimeQuadrantFcStatus.png, Delay time and the Flightcomputerbutton under the timewarp object
        /// </summary>
        public void Draw()
        {
            float scale = ScreenSafeUI.VerticalRatio * 900.0f / Screen.height;
            Vector2 screenCoord = ScreenSafeUI.referenceCam.WorldToScreenPoint(mTimewarpObject.timeQuadrantTab.transform.position);
            Rect screenPos = new Rect(9.0f / scale, Screen.height - screenCoord.y + 26.0f / scale, 50.0f / scale, 20.0f / scale);
            
            // calc the position under the timewarp object
            Rect pos = new Rect(mTimewarpObject.transform.position.x,
                    Screen.height - screenCoord.y + 18.5f,
                    (mTimewarpObject.timeQuadrantTab.renderer.material.mainTexture.width - 39.3f) / scale, (mTexBackground.height * 0.7f) / scale);

            // draw the image
            GUI.DrawTexture(pos, mTexBackground);
            // draw the delay-text
            GUI.Label(screenPos, DisplayText, mTextStyle);

            // draw the flightcomputer button to the right
            screenPos.width = 21.0f / scale;
            screenPos.x += 128 / scale;

            if (GUI.Button(screenPos, "", ButtonStyle))
            {
                var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (satellite == null || satellite.SignalProcessor.FlightComputer == null) return;
                satellite.SignalProcessor.FlightComputer.Window.Show();
            }
        }
    }
}
