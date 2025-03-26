using System;
using System.Collections.Generic;

namespace AlynxSaveLoadSystem.Runtime.GameSaves
{
    [Serializable]
    public class GameSave : SaveData
    {
        public string lastRoomSpawnDataID;
        public bool hasWon = false;

        public Dictionary<string, object> DynamicData = new();
        public Dictionary<string, int> Inventory = new();

        public override bool IsSingleSave()
        {
            return true;
        }
    }
}