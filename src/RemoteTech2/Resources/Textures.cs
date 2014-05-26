using System;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace RemoteTech {
    public class Textures {
        public static readonly Texture2D FilterBackground;
        public static readonly Texture2D FilterButton;
        public static readonly Texture2D FilterButtonDish;
        public static readonly Texture2D FilterButtonEmpty;
        public static readonly Texture2D FilterButtonNetwork;
        public static readonly Texture2D FilterButtonOmni;
        public static readonly Texture2D FilterButtonOmniDish;
        public static readonly Texture2D FilterButtonPath;
        public static readonly Texture2D FilterButtonPlanet;
        public static readonly Texture2D FilterButtonSatelliteGray;
        public static readonly Texture2D FilterButtonSatelliteGreen;
        public static readonly Texture2D FilterButtonSatelliteRed;
        public static readonly Texture2D FilterButtonSatelliteYellow;
        public static readonly Texture2D FlightComputerGreen;
        public static readonly Texture2D FlightComputerGreenDown;
        public static readonly Texture2D FlightComputerGreenOver;
        public static readonly Texture2D FlightComputerRed;
        public static readonly Texture2D FlightComputerRedDown;
        public static readonly Texture2D FlightComputerRedOver;
        public static readonly Texture2D FlightComputerYellow;
        public static readonly Texture2D FlightComputerYellowDown;
        public static readonly Texture2D FlightComputerYellowOver;
        public static readonly Texture2D KnowledgeButton;
        public static readonly Texture2D KnowledgeButtonActive;
        public static readonly Texture2D KnowledgeButtonHover;
        public static readonly Texture2D Mark;
        public static readonly Texture2D SatelliteIcon;
        public static readonly Texture2D TimeQuadrant;

        private static ResourceManager resourceManager = new ResourceManager("RemoteTech.Textures", typeof(Textures).Assembly);
        static Textures()
        {
            var resourceManager = new ResourceManager("RemoteTech.Textures", typeof(Textures).Assembly);

            foreach (var textureField in typeof(Textures).GetFields())
            {
                if (textureField.FieldType != typeof(Texture2D)) continue;
                textureField.SetValue(null, LoadImage(textureField.Name));
            }
        }

        private static Texture2D LoadImage(String fileName)
        {
            RTLog.Notify("LoadImage({0})", fileName);
            
            var fileStream = resourceManager.GetStream(fileName);
            var newTexture = new Texture2D(2, 2);
            var rawBitmap = new byte[fileStream.Length];
            fileStream.Read(rawBitmap, 0, (int)fileStream.Length);
            fileStream.Close();
            if (!newTexture.LoadImage(rawBitmap))
            {
                newTexture.Resize(32, 32);
                newTexture.SetPixels32(Enumerable.Repeat((Color32)Color.magenta, 32 * 32).ToArray());
                newTexture.Apply();
            }
            return newTexture;
        }
    }
}
