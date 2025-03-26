using System;
using System.Collections.Generic;

namespace AlynxSaveLoadSystem.Runtime.UserSettings
{
    [Serializable]
    public class UserSettings : SaveData
    {
        //Selected language
        public string SelectedLanguage;

        //Volume settings for all mixers
        public Dictionary<string, float> AudioVolume = new()
        {
            { "MasterVolume", 0.5f },
            { "Music", 1f },
            { "SFX", 1f },
            { "UI", 1f }
        };

        public override bool IsSingleSave()
        {
            return true;
        }
    }
}