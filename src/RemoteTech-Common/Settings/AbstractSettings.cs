using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteTech.Common.Utils;

namespace RemoteTech.Common.Settings
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SettingsAttribute : Attribute
    {
        public string SaveFileName;
        public string DefaultSettingFileName;
        public string NodeName;
    }

    public abstract class AbstractSettings : ISettings
    {
        /// <summary>Trigger to force a reloading of the settings if a selected save is running.</summary>
        public bool SettingsLoaded;
        /// <summary>True if its the first start of RemoteTech for this save, false otherwise.</summary>
        public bool FirstStart;

        public string SaveFileName;
        public string DefaultSettingFileName;
        public string NodeName;

        /// <summary>
        /// Returns the full path of the Default_Settings of the RemoteTech mod
        /// </summary>
        private string DefaultSettingFile => KSPUtil.ApplicationRootPath + "/GameData/RemoteTech/" + DefaultSettingFileName;

        [Persistent(collectionIndex = "PRESETS")]
        public List<string> PreSets { get; }

        private readonly object _lockObject = new object();
        private event Action<Game.Modes> _onSettingsLoadGameMode;

        /// <summary>
        /// Returns the current RemoteTech_Settings of an existing save full path. The path will be empty
        /// if no save is loaded or the game is a training mission
        /// </summary>
        private string SaveSettingFile
        {
            get
            {
                if (HighLogic.CurrentGame == null || GameUtil.IsGameScenario)
                    return string.Empty;

                return KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + Path.DirectorySeparatorChar + SaveFileName;
            }
        }

        protected AbstractSettings()
        {
        }

        public virtual ISettings Load(ISettings settings)
        {
            // Create a new settings object from the stored default settings

            var defaultLoad = ConfigNode.Load(DefaultSettingFile);
            if (defaultLoad == null) // disable itself and write explanation to KSP's log
            {
                RTLog.Notify("RemoteTech is disabled because the default file '{0}' is not found", DefaultSettingFile);
                return null;
                // the main impact of returning null is the endless loop of invoking Load() in the KSP's loading screen
            }
            defaultLoad = defaultLoad.GetNode("RemoteTechSettings"); // defaultLoad has root{...} so need to traverse downwards
            var success = ConfigNode.LoadObjectFromConfig(settings, defaultLoad);
            RTLog.Notify("Load default settings into object with {0}: LOADED {1}", defaultLoad, success ? "OK" : "FAIL");

            SettingsLoaded = true;

            if (HighLogic.CurrentGame != null)
            {
                _onSettingsLoadGameMode?.Invoke(HighLogic.CurrentGame.Mode);
            }

            // stop and return default settings if we are on the KSP loading screen OR in training scenarios
            if (string.IsNullOrEmpty(SaveSettingFile))
            {
                return settings;
            }

            // try to load from the save-settings.cfg
            var load = ConfigNode.Load(SaveSettingFile);
            if (load == null)
            {
                // write the RT settings to the player's save folder
                settings.Save();
                FirstStart = true;
            }
            else
            {
                // old or new format?
                if (load.HasNode("RemoteTechSettings"))
                    load = load.GetNode("RemoteTechSettings");

                // replace the default settings with save-setting file
                success = ConfigNode.LoadObjectFromConfig(settings, load);
                RTLog.Notify("Found and load save settings into object with {0}: LOADED {1}", load, success ? "OK" : "FAIL");
            }

            // find third-party mods' RemoteTech settings
            SearchAndPreparePresets(settings);

            return settings;
        }

        public virtual ISettings LoadPreset(ISettings previousSettings, string presetCfgUrl)
        {
            throw new NotImplementedException();
        }

        public virtual void Save()
        {
            try
            {
                // only save the settings if the file name is not empty (i.e. not on loading screen or in training)
                if (string.IsNullOrEmpty(SaveSettingFile))
                    return;

                var details = new ConfigNode("RemoteTechSettings");
                ConfigNode.CreateConfigFromObject(this, 0, details);
                var save = new ConfigNode();
                save.AddNode(details);
                save.Save(SaveSettingFile);

                //RTSettings.OnSettingsSaved.Fire();
            }
            catch (Exception ex)
            {
                RTLog.Notify($"An error occurred while attempting to save: {ex}", RTLogLevel.LVL4);
            }
        }

        public event Action<Game.Modes> OnSettingsLoadGameMode
        {
            add
            {
                lock (_lockObject)
                {
                    if (_onSettingsLoadGameMode != null) _onSettingsLoadGameMode += value;
                }
            }
            remove
            {
                lock (_lockObject)
                {
                    if (_onSettingsLoadGameMode != null) _onSettingsLoadGameMode -= value;
                }
            }
        }


        private void SearchAndPreparePresets(ISettings settings)
        {
            var presetsChanged = false;

            // Exploit KSP's GameDatabase to find third-party mods' RemoteTechSetting node (from GameData/ExampleMod/RemoteTechSettings.cfg)
            var cfgs = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            var rtSettingCfGs = cfgs.Select(x => x.url).ToList();

            //check for any invalid preset in the settings of a save
            for (var i = 0; i < settings.PreSets.Count(); i++)
            {
                if (rtSettingCfGs.Contains(settings.PreSets[i]))
                    continue;

                RTLog.Notify("Remove an invalid setting preset {0}", settings.PreSets[i]);
                settings.PreSets.RemoveAt(i);
                presetsChanged = true;
            }

            //find and add new presets to the settings of a save
            for (var i = 0; i < rtSettingCfGs.Count(); i++)
            {
                if (settings.PreSets.Contains(rtSettingCfGs[i]))
                    continue;

                RTLog.Notify("Add a new setting preset {0}", rtSettingCfGs[i]);
                settings.PreSets.Add(rtSettingCfGs[i]);
                presetsChanged = true;
            }

            if (presetsChanged) // only if new RT settings are found and added to the save-setting's PreSets node
                settings.Save();
        }
    }
}
