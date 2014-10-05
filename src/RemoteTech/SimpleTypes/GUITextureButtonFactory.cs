using System;
using UnityEngine;

namespace RemoteTech
{
    public static class GUITextureButtonFactory
    {

        public static GUIStyle CreateFromFilename(String normal)
        {
            Texture2D[] tex = new Texture2D[4];
            RTUtil.LoadImage(out tex[0], normal);
            RTUtil.LoadImage(out tex[1], normal);
            RTUtil.LoadImage(out tex[2], normal);
            RTUtil.LoadImage(out tex[3], normal);

            return CreateFromTextures(tex[0], tex[1], tex[2], tex[3]);
        }

        public static GUIStyle CreateFromFilename(String normal, String hover, String active,
                                                                               String focus)
        {
            Texture2D[] tex = new Texture2D[4];
            RTUtil.LoadImage(out tex[0], normal);
            RTUtil.LoadImage(out tex[1], hover);
            RTUtil.LoadImage(out tex[2], active);
            RTUtil.LoadImage(out tex[3], focus);

            return CreateFromTextures(tex[0], tex[1], tex[2], tex[3]);
        }

        private static GUIStyle CreateFromTextures(Texture2D texNormal, Texture2D texHover,
                                                   Texture2D texActive, Texture2D texFocus)
        {
            return new GUIStyle()
            {
                name = texNormal.name,
                normal = new GUIStyleState() { background = texNormal, textColor = Color.white },
                hover = new GUIStyleState() { background = texHover, textColor = Color.white },
                active = new GUIStyleState() { background = texActive, textColor = Color.white },
                onNormal = new GUIStyleState() { background = texActive, textColor = Color.white },
                onHover = new GUIStyleState() { background = texActive, textColor = Color.white },
                onActive = new GUIStyleState() { background = texActive, textColor = Color.white },
                focused = new GUIStyleState() { background = texFocus, textColor = Color.white },
                onFocused = new GUIStyleState() { background = texActive, textColor = Color.white },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                imagePosition = ImagePosition.ImageAbove,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip,
                contentOffset = new Vector2(0, 0),
                fixedWidth = texNormal.width,
                fixedHeight = texNormal.height,
                stretchWidth = false,
                stretchHeight = false,
                font = null,
                fontSize = 0,
                fontStyle = FontStyle.Normal,
                richText = false,
            };
        }
    }
}