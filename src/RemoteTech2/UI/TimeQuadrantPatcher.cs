using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class TimeQuadrantPatcher
    {
        private class Backup
        {
            public TimeWarp TimeQuadrant { get; set; }
            public Texture Texture { get; set; }
            public Vector3 Scale { get; set; }
            public Vector3 Center { get; set; }
            public Vector3 ExpandedPosition { get; set; }
            public Vector3 CollapsedPosition { get; set; }
        }

        private static readonly GUIStyle mFlightButtonGreen, mFlightButtonRed, mFlightButtonYellow;

        private String DisplayText
        {
            get
            {
                var vs = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (vs == null)
                {
                    return "N/A";
                }
                else if (vs.HasLocalControl)
                {
                    return "Local Control";
                }
                else if (vs.Connections().Any())
                {
                    if (RTSettings.Instance.EnableSignalDelay)
                    {
                        return "D+ " + vs.Connections().ShortestDelay().SignalDelay.ToString("F6") + "s";
                    }
                    else
                    {
                        return "Connected";
                    }                    
                }
                return "No Connection";
            }
        }

        private GUIStyle ButtonStyle
        {
            get
            {
                var vs = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (vs == null) 
                {
                    return mFlightButtonRed;
                }
                else if (vs.HasLocalControl)
                {
                    return mFlightButtonYellow;
                }
                else if (vs.Connections().Any())
                {
                    return mFlightButtonGreen;
                }
                return mFlightButtonRed;
            }
        }

        private Backup mBackup;
        private GUIStyle mTextStyle;

        static TimeQuadrantPatcher()
        {
            mFlightButtonGreen = GUITextureButtonFactory.CreateFromTextures(Textures.FlightComputerGreen,
                                                                       Textures.FlightComputerGreenOver,
                                                                       Textures.FlightComputerGreenDown,
                                                                       Textures.FlightComputerGreenOver);
            mFlightButtonYellow = GUITextureButtonFactory.CreateFromTextures(Textures.FlightComputerYellow,
                                                                       Textures.FlightComputerYellowOver,
                                                                       Textures.FlightComputerYellowDown,
                                                                       Textures.FlightComputerYellowOver);
            mFlightButtonRed = GUITextureButtonFactory.CreateFromTexture(Textures.FlightComputerRed);

            mFlightButtonGreen.fixedHeight = mFlightButtonGreen.fixedWidth = 0;
            mFlightButtonYellow.fixedHeight = mFlightButtonYellow.fixedWidth = 0;
            mFlightButtonRed.fixedHeight = mFlightButtonRed.fixedWidth = 0;
            mFlightButtonGreen.stretchHeight = mFlightButtonGreen.stretchWidth = true;
            mFlightButtonYellow.stretchHeight = mFlightButtonYellow.stretchWidth = true;
            mFlightButtonRed.stretchHeight = mFlightButtonRed.stretchWidth = true;
        }

        public void Patch()
        {
            var timeQuadrant = TimeWarp.fetch;
            if (timeQuadrant == null) return;
            if (mBackup != null)
            {
                throw new InvalidOperationException("Patcher is already in use.");
            }
            var tab = timeQuadrant.timeQuadrantTab;
            mBackup = new Backup()
            {
                TimeQuadrant = timeQuadrant,
                Texture = tab.renderer.material.mainTexture,
                Scale = tab.transform.localScale,
                Center = ((BoxCollider)tab.collider).center,
                ExpandedPosition = tab.expandedPos,
                CollapsedPosition = tab.collapsedPos,
            };

            List<Transform> children = new List<Transform>();

            foreach (Transform child in tab.transform)
            {
                children.Add(child);
            }

            foreach (Transform child in children)
            {
                child.parent = tab.transform.parent;
            }

            // Set the new texture
            float old_height = tab.renderer.material.mainTexture.height;
            Texture2D newTexture = Textures.TimeQuadrant;
            newTexture.filterMode = FilterMode.Trilinear;
            newTexture.wrapMode = TextureWrapMode.Clamp;
            tab.renderer.material.mainTexture = newTexture;

            // Apply new scale, positions
            float scale = Screen.height / (float)GameSettings.UI_SIZE;
            tab.transform.localScale = new Vector3(tab.transform.localScale.x,
                                                   tab.transform.localScale.y,
                                                   tab.transform.localScale.z * (tab.renderer.material.mainTexture.height / old_height));
            tab.collapsedPos += new Vector3(0, -0.013f, 0);
            tab.expandedPos += new Vector3(0, -0.013f, 0);
            foreach (Transform child in children)
            {
                child.localPosition += new Vector3(0, 0.013f, 0);
            }
            tab.Expand();

            foreach (Transform child in children)
            {
                child.parent = tab.transform;
            }

            ((BoxCollider)tab.collider).center += new Vector3(0, 0, -0.37f);

            var text = tab.transform.FindChild("MET timer").GetComponent<ScreenSafeGUIText>();
            mTextStyle = new GUIStyle(text.textStyle);
            mTextStyle.fontSize = (int)(text.textSize * ScreenSafeUI.PixelRatio);

            RenderingManager.AddToPostDrawQueue(0, Draw);
        }

        public void Undo()
        {
            try
            {
                RenderingManager.RemoveFromPostDrawQueue(0, Draw);

                if (mBackup == null)
                    return;

                var tab = mBackup.TimeQuadrant.timeQuadrantTab;

                if (tab.collider != null)
                {
                    ((BoxCollider)tab.collider).center = mBackup.Center;
                }

                List<Transform> children = new List<Transform>();

                foreach (Transform child in tab.transform)
                {
                    children.Add(child);
                    child.parent = tab.transform.parent;
                }

                tab.transform.localScale = mBackup.Scale;
                tab.expandedPos = mBackup.ExpandedPosition;
                tab.collapsedPos = mBackup.CollapsedPosition;
                foreach (Transform child in children)
                {
                    child.localPosition += new Vector3(0, -0.013f, 0);
                }
                tab.Collapse();
                tab.renderer.material.mainTexture = mBackup.Texture;

                foreach (Transform child in children)
                {
                    child.parent = tab.transform;
                }

                mBackup = null;
            }
            catch (Exception) { }
        }

        public void Draw()
        {
            if (mBackup != null)
            {
                float scale = ScreenSafeUI.VerticalRatio * 900.0f / Screen.height;
                Vector2 screenCoord = ScreenSafeUI.referenceCam.WorldToScreenPoint(mBackup.TimeQuadrant.timeQuadrantTab.transform.position);
                Rect screenPos = new Rect(5.0f / scale, Screen.height - screenCoord.y + 14.0f / scale, 50.0f / scale, 20.0f / scale);

                GUI.Label(screenPos, DisplayText, mTextStyle);

                screenPos.width = 21.0f / scale;
                screenPos.x += 101 / scale;

                if (GUI.Button(screenPos, "", ButtonStyle))
                {
                    var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                    if (satellite == null || satellite.SignalProcessor.FlightComputer == null) return;
                    satellite.SignalProcessor.FlightComputer.Window.Show();
                    //ScreenMessages.PostScreenMessage(new ScreenMessage("[FlightComputer]: Not yet implemented!", 4.0f, ScreenMessageStyle.UPPER_LEFT));
                }
            }
        }
    }
}