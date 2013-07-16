using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class TimeQuadrantPatcher {
        private class Backup {
            public TimeWarp TimeQuadrant { get; set; }
            public Texture Texture { get; set; }
            public Vector3 Scale { get; set; }
            public Vector3 Center { get; set; }
            public Vector3 ExpandedPosition { get; set; }
            public Vector3 CollapsedPosition { get; set; }
        }

        private static readonly GUIStyle mFlightButton;

        private String DisplayText {
            get {
                VesselSatellite vs = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (vs == null) {
                    return "N/A";
                } else if (vs.LocalControl) {
                    return "Local Control";
                } else if (vs.Connection.Exists) {
                    return "D+" + vs.Connection.Delay + "s";
                }
                return "Not Connected";
            }
        }

        private Backup mBackup;
        private GUIStyle mTextStyle;
        
        static TimeQuadrantPatcher() {
            mFlightButton = GUITextureButtonFactory.CreateFromFilename("texFlightNormal.png",
                                                                       "texFlightNormal.png",
                                                                       "texFlightActive.png",
                                                                       "texFlightNormal.png");
            RTUtil.Log("TimeQuadrantPatcher has loaded textures.");
        }

        public void Patch(TimeWarp timeQuadrant) {
            if (mBackup != null) {
                throw new InvalidOperationException("Patcher is already in use.");
            }
            ScreenSafeUISlideTab tab = timeQuadrant.timeQuadrantTab;
            mBackup = new Backup() {
                TimeQuadrant = timeQuadrant,
                Texture = tab.renderer.material.mainTexture,
                Scale = tab.transform.localScale,
                Center = ((BoxCollider) tab.collider).center,
                ExpandedPosition = tab.expandedPos,
                CollapsedPosition = tab.collapsedPos,
            };

            List<Transform> children = new List<Transform>();

            foreach (Transform child in tab.transform) {
                children.Add(child);
            }

            foreach (Transform child in children) {
                child.parent = tab.transform.parent;
            }

            // Set the new texture
            Texture2D newTexture;
            RTUtil.LoadImage(out newTexture, "texTimeQuadrant.png");
            tab.renderer.material.mainTexture = newTexture;

            // Apply new scale, positions
            tab.transform.localScale = new Vector3(tab.transform.localScale.x,
                                                   tab.transform.localScale.y,
                                                   tab.transform.localScale.z * 
                                                   1.3970588235294117647058823529412f);
            tab.collapsedPos += new Vector3(0, -0.013f, 0);
            tab.expandedPos += new Vector3(0, -0.013f, 0);
            foreach (Transform child in children) {
                child.localPosition += new Vector3(0, 0.013f, 0);
            }
            tab.Expand();

            foreach (Transform child in children) {
                child.parent = tab.transform;
            }

            ((BoxCollider) tab.collider).center += new Vector3(0, 0, -0.37f);

            ScreenSafeGUIText text = 
                tab.transform.FindChild("MET timer").GetComponent<ScreenSafeGUIText>();
            mTextStyle = text.textStyle;
            mTextStyle.fontSize = (int)(text.textSize * ScreenSafeUI.PixelRatio);

            RenderingManager.AddToPostDrawQueue(0, Draw);
        }

        public void Undo() {
            if (mBackup == null && mBackup.TimeQuadrant != null)
                return;

            ScreenSafeUISlideTab tab = mBackup.TimeQuadrant.timeQuadrantTab;

            ((BoxCollider)tab.collider).center = mBackup.Center;

            List<Transform> children = new List<Transform>();

            foreach (Transform child in tab.transform) {
                children.Add(child);
                child.parent = tab.transform.parent;
            }

            tab.transform.localScale = mBackup.Scale;
            tab.expandedPos = mBackup.ExpandedPosition;
            tab.collapsedPos = mBackup.CollapsedPosition;
            foreach (Transform child in children) {
                child.localPosition += new Vector3(0, -0.013f, 0);
            }
            tab.Collapse();
            tab.renderer.material.mainTexture = mBackup.Texture;

            foreach (Transform child in children) {
                child.parent = tab.transform;
            }

            mBackup = null;

            RenderingManager.RemoveFromPostDrawQueue(0, Draw);
        }

        public void Draw() {
            if(mBackup != null) {
                GUI.depth = 0;
                Vector2 screenCoord = ScreenSafeUI.referenceCam.WorldToScreenPoint(mBackup.TimeQuadrant.timeQuadrantTab.transform.position);
                screenCoord += new Vector2(0, -10f);
                Rect screenPos = new Rect(5.0f, Screen.height - screenCoord.y, 50, 20);
                GUI.Label(screenPos, DisplayText, mTextStyle);
                screenPos.x += 80f;
                if (GUI.Button(screenPos, "", mFlightButton)) {
                    RTCore.Instance.Gui.OpenFlightComputer();
                }
            }
        }
    }
}
