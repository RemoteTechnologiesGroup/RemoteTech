using System;
using System.Linq;
using RemoteTech.SimpleTypes;
using UnityEngine;
using KSP.UI;
using KSP.UI.Screens.Flight;

namespace RemoteTech.UI
{
    public class TimeWarpDecorator
    {
        /// <summary>
        /// Image for position access
        /// </summary>
        private UnityEngine.UI.Image mTimewarpImage;

        private TMPro.TextMeshProUGUI mText;

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
                mTimewarpImage = go.GetComponent<UnityEngine.UI.Image>();
            }

            // objects on this scene?
            if (mTimewarpImage == null || TimeWarp.fetch == null)
            {
                return;
            }

            // find the MET display
            var metDisplay = GameObject.FindObjectOfType<METDisplay>();
            if (metDisplay == null)
            {
                RTLog.Notify("No MET Display");
                return;
            }

            // instantiate a new game object and set it to the UI layer.
            GameObject gameObjText = new GameObject("TextTimeWarpDecorator");
            gameObjText.layer = LayerMask.NameToLayer("UI");

            // add a rect transform to the game object
            RectTransform rectTrans = gameObjText.AddComponent<RectTransform>();
            rectTrans.localScale = new Vector3(1, 1, 1);
            rectTrans.localPosition.Set(0, 0, 0);

            // add text mesh to game object and set its properties
            mText = gameObjText.AddComponent<TMPro.TextMeshProUGUI>();
            mText.fontSize = metDisplay.text.fontSize;
            mText.font = metDisplay.text.font;
            mText.fontSharedMaterial = metDisplay.text.fontMaterial;
            mText.color = metDisplay.text.color;
            mText.autoSizeTextContainer = true;
            mText.enableWordWrapping = false;
            mText.isOverlay = true;            

            // set the MET display as parent of the game object
            gameObjText.transform.SetParent(metDisplay.transform, false);
        }

        /// <summary>
        /// Draws the TimeQuadrantFcStatus.png, Delay time and the Flightcomputerbutton under the timewarp object
        /// </summary>
        public void Draw()
        {
            // no drawing without timewarp object
            if (mTimewarpImage == null)
                return;

            //RTLog.Notify("mVessel: " + (mVessel == null).ToString());
            //RTLog.Notify("stats: " + RTCore.Instance.Satellites.ToArray().Length); 


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
            mText.color = new Color(0.56078f, 0.10196f, 0.07450f);
            if (this.mVessel != null && this.mVessel.Connections.Any())
            {
                mText.color = XKCDColors.GreenApple;
            }
            
            //GUI.Label(delaytextPosition, DisplayText, mTextStyle);
            mText.text = DisplayText;


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
