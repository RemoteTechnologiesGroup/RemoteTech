using System;
using System.Linq;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTech.UI
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
                        return "D+ " + vs.Connections[0].Delay.ToString("F5") + "s";
                    }
                    else
                    {
                        return "Connected";
                    }                    
                }
				if (RTSettings.Instance.EnableNetworkOnlyMode) 
				{
					return "Network Only Mode";
				} 
				else 
				{
					return "No Connection";
				}
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

            // objects on this scene?
            if (mTimewarpObject == null || mTimewarpObject.timeQuadrantTab == null)
            {
                // to skip the draw calls
                mTimewarpObject = null;
                return;
            }

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
            // no drawing without timewarp object
            if (mTimewarpObject == null)
                return;


            Vector2 screenCoord = ScreenSafeUI.referenceCam.WorldToScreenPoint(mTimewarpObject.timeQuadrantTab.transform.position);
            float scale = ScreenSafeUI.VerticalRatio * 900.0f / Screen.height;

            float topLeftTotimeQuadrant = Screen.height - screenCoord.y;
            float texBackgroundHeight = mTexBackground.height * 0.7f / scale;
            float texBackgroundWidth = (mTimewarpObject.timeQuadrantTab.renderer.material.mainTexture.width * 0.8111f) / scale;


            Rect delaytextPosition = new Rect(12.0f / scale, topLeftTotimeQuadrant + texBackgroundHeight - 1, 50.0f / scale, 20.0f / scale);
                        
            // calc the position under the timewarp object
            Rect pos = new Rect(mTimewarpObject.transform.position.x,
                                topLeftTotimeQuadrant + texBackgroundHeight - 3.0f,
                                texBackgroundWidth, texBackgroundHeight);

            // draw the image
            GUI.DrawTexture(pos, mTexBackground);
            // draw the delay-text
            GUI.Label(delaytextPosition, DisplayText, mTextStyle);

            // draw the flightcomputer button to the right relativ to the delaytext position
            delaytextPosition.width = 21.0f / scale;
            delaytextPosition.x += 125 / scale;

            if (GUI.Button(delaytextPosition, "", ButtonStyle))
            {
                var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (satellite == null || satellite.SignalProcessor.FlightComputer == null) return;
                satellite.SignalProcessor.FlightComputer.Window.Show();
            }
        }
    }
}
