using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace AlynxSaveLoadSystem.Runtime
{
    public static class SaveSystem
    {
        //Extension for file
        private const string FileExtension = ".cute";
        public static readonly string SaveLocation = Application.persistentDataPath + "/Saves/";


        private static DateTime _lastSaveTime;

        //Static Event when data is saved
        public static event Action<SaveData> OnDataSaved;

        //Method to save data by given type, must be serializable
        public static void Save<T>(T data, string saveName = null) where T : SaveData
        {
            Save(data as SaveData, saveName);
        }

        //Save data but with Type parameter
        public static void Save(SaveData data, string saveName = null)
        {
            //Check if data is null
            if (data == null)
            {
                Debug.LogWarning("SaveSystem: Data is null");
                return;
            }

            if (!data.IsSingleSave() && string.IsNullOrEmpty(saveName))
            {
                Debug.LogWarning("SaveSystem: Save name is null or empty");
                return;
            }

            var saveLocation = SaveLocation + (data.IsSingleSave() ? "" : saveName + "/");

            data.SaveTime = DateTime.Now;

            //Create directory if it does not exist
            if (!Directory.Exists(saveLocation)) Directory.CreateDirectory(saveLocation);

            //If file does not exist, create it
            if (!File.Exists(saveLocation + data.GetType().Name + FileExtension))
            {
                var file = File.Create(saveLocation + data.GetType().Name + FileExtension);
                file.Close();
            }

            var dataStream = new FileStream(saveLocation + data.GetType().Name + FileExtension, FileMode.Create);

            var converter = new BinaryFormatter();
            converter.Serialize(dataStream, data);

            dataStream.Close();

            //Invoke On Data Saved event
            OnDataSaved?.Invoke(data);
        }

        //Method to load data by given type, must be serializable
        public static T Load<T>(string saveName = null) where T : SaveData
        {
            var save = Activator.CreateInstance<T>();

            if (string.IsNullOrEmpty(saveName) && !save.IsSingleSave())
            {
                Debug.LogWarning("SaveSystem: Save name is null or empty");
                return default;
            }


            var saveLocation = SaveLocation + (save.IsSingleSave() ? "" : saveName + "/");

            //Check if file exists
            if (!File.Exists(saveLocation + typeof(T).Name + FileExtension))
            {
                //Save new data
                Save(save, saveName);

                return save;
            }

            // File exists 
            var dataStream = new FileStream(saveLocation + typeof(T).Name + FileExtension, FileMode.Open);

            var converter = new BinaryFormatter();
            var saveData = converter.Deserialize(dataStream) as T;

            dataStream.Close();
            return saveData;
        }

        //Load data but with Type parameter
        public static SaveData Load(Type type, string saveName = null)
        {
            var save = Activator.CreateInstance(type) as SaveData;

            if (save == null)
            {
                Debug.LogWarning("SaveSystem: Data is null");
                return null;
            }

            if (string.IsNullOrEmpty(saveName) && !save.IsSingleSave())
            {
                Debug.LogWarning("SaveSystem: Save name is null or empty");
                return null;
            }

            var saveLocation = SaveLocation + (save.IsSingleSave() ? "" : saveName + "/");

            //Check if file exists
            if (!File.Exists(saveLocation + type.Name + FileExtension))
            {
                //Save new data
                Save(save, saveName);
                return save;
            }

            // File exists 
            var dataStream = new FileStream(saveLocation + type.Name + FileExtension, FileMode.Open);

            var converter = new BinaryFormatter();
            var saveData = converter.Deserialize(dataStream) as SaveData;

            dataStream.Close();
            return saveData;
        }

        public static string[] GetSaveNames()
        {
            var saveNames = Directory.GetDirectories(SaveLocation).ToList();

            //Remove path
            for (var i = 0; i < saveNames.Count; i++) saveNames[i] = saveNames[i].Replace(SaveLocation, "");

            //Remove editorSave
            if (saveNames.Contains("EditorSave")) saveNames.Remove("EditorSave");

            return saveNames.ToArray();
        }

        public static void DeleteSave(string gameSaveSaveName)
        {
            if (string.IsNullOrEmpty(gameSaveSaveName))
            {
                Debug.LogWarning("SaveSystem: Save name is null or empty");
                return;
            }

            var saveLocation = SaveLocation + gameSaveSaveName + "/";

            if (Directory.Exists(saveLocation)) Directory.Delete(saveLocation, true);
        }

        public static void RenameSave(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
            {
                Debug.LogWarning("SaveSystem: Save name is null or empty");
                return;
            }

            var oldSaveLocation = SaveLocation + oldName + "/";
            var newSaveLocation = SaveLocation + newName + "/";

            if (Directory.Exists(oldSaveLocation)) Directory.Move(oldSaveLocation, newSaveLocation);
        }

        public static bool SaveExists(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogWarning("SaveSystem: Save name is null or empty");
                return false;
            }

            var saveLocation = SaveLocation + saveName + "/";

            return Directory.Exists(saveLocation);
        }

        public static bool DoesSaveExist()
        {
            if (!Directory.Exists(SaveLocation)) return false;

            //Check if there is more than one file in the save location
            return Directory.GetFiles(SaveLocation).Length > 0;
        }
    }
}