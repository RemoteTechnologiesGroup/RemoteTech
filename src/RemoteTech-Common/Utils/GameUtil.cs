using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RemoteTech.Common.Utils
{
    public static class GameUtil
    {
        /// <summary>Automatically finds the proper texture directory from the DLL location. Assumes the DLL is in the proper location of GameData/RemoteTech/Plugins/</summary>
        /// <returns>The texture directory string if found otherwise a null reference.</returns>
        private static string TextureDirectory
        {
            get
            {
                var location = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location))
                {
                    var parentLocation = Directory.GetParent(location).Parent;
                    if (parentLocation != null)
                        return parentLocation.Name + "/Textures/";

                    RTLog.Notify("TextureDirectory: cannot Find parent location", RTLogLevel.LVL4);
                    return null;
                }

                RTLog.Notify("TextureDirectory: cannot Find location", RTLogLevel.LVL4);
                return null;
            }
        }

        /// <summary>True if the current running game is a SCENARIO or SCENARIO_NON_RESUMABLE, otherwise false</summary>
        public static bool IsGameScenario => (HighLogic.CurrentGame != null && (HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO || HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO_NON_RESUMABLE));

        /// <summary>Load an image from the texture directory.</summary>
        /// <param name="texture">The output texture if the texture is found, otherwise a black texture.</param>
        /// <param name="fileName">The file name of the texture (in the texture directory).</param>
        /// <remarks>Replaces old manual method with unity style texture loading.</remarks>
        public static void LoadImage(out Texture2D texture, string fileName)
        {
            var str = TextureDirectory + fileName;
            if (GameDatabase.Instance.ExistsTexture(str))
                texture = GameDatabase.Instance.GetTexture(str, false);
            else
            {
                RTLog.Notify($"LoadImage: cannot Find Texture: {str}", RTLogLevel.LVL4);
                texture = Texture2D.blackTexture;
            }
        }

        /// <summary>Check if a technology is unlocked in the Research and Development center.</summary>
        /// <param name="techId">The technology Id.</param>
        /// <returns>true if the technology is unlocked, false otherwise.</returns>
        public static bool IsTechUnlocked(string techId)
        {
            if (string.IsNullOrEmpty(techId))
            {
                RTLog.Notify("IsTechUnlocked: techId is null or empty.", RTLogLevel.LVL4);
                return false;
            }

            if (techId.Equals("None"))
                return true;

            return HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX ||
                ResearchAndDevelopment.GetTechnologyState(techId) == RDTech.State.Available;
        }

        /// <summary>Load an image from the texture directory.</summary>
        /// <param name="fileName">The file name of the texture (in the texture directory).</param>
        /// <returns>The texture if the file was found, otherwise a completely black texture.</returns>
        /// <remarks>Replaces old manual method with unity style texture loading.</remarks>
        public static Texture2D LoadImage(string fileName)
        {
            var str = TextureDirectory + fileName;
            if (GameDatabase.Instance.ExistsTexture(str))
                return GameDatabase.Instance.GetTexture(str, false);

            RTLog.Notify($"LoadImage: cannot Find Texture: {str}", RTLogLevel.LVL4);
            return Texture2D.blackTexture;
        }

        /// <summary>Returns the current AssemblyFileVersion, as a string, from AssemblyInfos.cs.</summary>
        public static string Version
        {
            get
            {
                var executableAssembly = Assembly.GetExecutingAssembly();
                if (!string.IsNullOrEmpty(executableAssembly.Location))
                    return "v" + FileVersionInfo.GetVersionInfo(executableAssembly.Location).ProductVersion;

                RTLog.Notify("Executing assembly is null", RTLogLevel.LVL4);
                return "Unknown version";
            }
        }
    }
}
