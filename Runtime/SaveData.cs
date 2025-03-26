using System;

namespace AlynxSaveLoadSystem.Runtime
{
    [Serializable]
    public abstract class SaveData
    {
        public DateTime SaveTime { get; set; }
        public abstract bool IsSingleSave();
    }
}