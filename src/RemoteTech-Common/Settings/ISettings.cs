using System;
using System.Collections.Generic;

namespace RemoteTech.Common.Settings
{
    public interface ISettings
    {
        ISettings Load(ISettings settings);

        ISettings LoadPreset(ISettings previousSettings, string presetCfgUrl);

        void Save();
        
        List<string> PreSets { get; }

        event Action<Game.Modes> OnSettingsLoadGameMode;
    }
}
