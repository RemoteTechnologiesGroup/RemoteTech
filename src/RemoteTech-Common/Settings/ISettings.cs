using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

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
