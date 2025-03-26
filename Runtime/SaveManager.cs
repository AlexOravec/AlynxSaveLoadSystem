using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AlynxSceneSystem.Runtime;
using AlynxServicesManager.Runtime;
using AlynxSaveLoadSystem.Runtime.GameSaves;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityGameplayFramework.Runtime.Object;


namespace AlynxSaveLoadSystem.Runtime
{
    [DefaultExecutionOrder(-1)]
    public class SaveManager : Object, ISceneLoadingBlocker, IGameService
    {
        private static readonly Dictionary<ILoadableContent, bool> LoadableContents = new();
        public static string CurrentSaveName;

        private bool _isGameLoaded;
        private GameSave[] _preloadedSaves;
        private SceneLoader _sceneLoader;
        public Action OnGameLoaded;
        public Action OnGameplayLoaded;
        public Action OnGameSaved;

        public bool IsBlockingSceneLoading()
        {
            return !_isGameLoaded;
        }

        public void OnStartedBlockingSceneLoading()
        {
            _isGameLoaded = false;

            //Load all loadable content
            foreach (var loadableContent in LoadableContents.Keys)
            {
                var contentType = loadableContent.GetType();
                var loadMethod = contentType.GetMethod("Load");
                var data = SaveSystem.Load(loadableContent.GetContentType(), CurrentSaveName);

                if (data == null || loadMethod == null)
                {
                    Debug.LogWarning("SaveManager: Data is null");
                    continue;
                }

                // Invoke the Load method dynamically
                loadMethod.Invoke(loadableContent, new object[] { data });
            }

            //Start coroutine to wait for game to load
            StartCoroutine(WaitForGameToLoad());
        }

        public void PreloadAndSortSaves()
        {
            if (HasPreloadedSaves()) return;

            var saveNames = SaveSystem.GetSaveNames().ToList();

            //Sort save names by date
            saveNames.Sort((a, b) =>
            {
                var saveA = SaveSystem.Load<GameSave>(a);
                var saveB = SaveSystem.Load<GameSave>(b);
                return saveB.SaveTime.CompareTo(saveA.SaveTime);
            });

            _preloadedSaves = new GameSave[saveNames.Count];

            for (var i = 0; i < saveNames.Count; i++) _preloadedSaves[i] = SaveSystem.Load<GameSave>(saveNames[i]);
        }

        public bool HasPreloadedSaves()
        {
            return _preloadedSaves != null;
        }

        public GameSave[] GetPreloadedSaves()
        {
            return _preloadedSaves;
        }


#if UNITY_EDITOR
        //Button to open save location
        [MenuItem("Tools/Open Save Location")]
#endif
        public static void OpenSaveLocation()
        {
            //Open save location
            Process.Start(SaveSystem.SaveLocation);
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Reset Save")]
#endif
        public static void ResetSave()
        {
            //Find GameSettings
            var path = SaveSystem.SaveLocation;

            //Check if directory exists
            if (!Directory.Exists(path)) return;

            //Delete all files
            foreach (var file in Directory.GetFiles(path)) File.Delete(file);

            //Also delete directories
            foreach (var directory in Directory.GetDirectories(path)) Directory.Delete(directory, true);
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (Application.isEditor) CurrentSaveName = "EditorSave";
        }

        protected override void BeginPlay()
        {
            base.BeginPlay();

            //Register as scene loading blocker
            _sceneLoader = ServiceLocator.GetService<SceneLoader>();

            //Null check
            if (_sceneLoader != null)
            {
                _sceneLoader.OnSceneChanged += GameplayLoaded;
                _sceneLoader.AddSceneLoadingBlocker(this);
            }
        }

        protected override void Destroyed()
        {
            base.Destroyed();

            if (_sceneLoader != null) _sceneLoader.OnSceneChanged -= GameplayLoaded;
        }

        private void GameplayLoaded()
        {
            //Invoke event
            OnGameplayLoaded?.Invoke();
        }

        //Coroutine to wait for game to load
        private IEnumerator WaitForGameToLoad()
        {
            //Check if content is loaded
            foreach (var loadableContent in LoadableContents)
                yield return new WaitUntil(() => loadableContent.Key.HasFinishedLoading());

            var contentToRemove = new List<ILoadableContent>();

            foreach (var valuePair in LoadableContents)
                if (valuePair.Value)
                    contentToRemove.Add(valuePair.Key);

            //Unregister content
            foreach (var content in contentToRemove) UnregisterContentToLoad(content);

            //Invoke event
            OnGameLoaded?.Invoke();
            _isGameLoaded = true;
        }

        //Register content
        public static bool RegisterContentToLoad<T>(ILoadableContent<T> content, bool removeAfterLoad = true)
            where T : SaveData
        {
            //Check if content is null
            if (content == null)
            {
                Debug.LogWarning("SaveManager: Content is null");
                return false;
            }


            var saveDataContent = content;

            //Check if content is already registered
            if (LoadableContents.ContainsKey(saveDataContent)) return false;

            var sceneLoader = ServiceLocator.GetService<SceneLoader>();

            //Null check
            if (sceneLoader == null || sceneLoader.IsLoading == false)
            {
                content.Load(LoadContent<T>());
                return false;
            }

            //Check if game is loaded
            if (!sceneLoader.IsLoading) return false;

            //Add content to list
            LoadableContents.Add(saveDataContent, removeAfterLoad);
            return true;
        }

        //Unregister content
        public static void UnregisterContentToLoad(ILoadableContent content)
        {
            //Check if content is null
            if (content == null)
            {
                Debug.LogWarning("SaveManager: Content is null");
                return;
            }

            //Check if content is registered
            if (!LoadableContents.ContainsKey(content)) return;

            //Remove content from list
            LoadableContents.Remove(content);
        }

        public static void SaveDynamicContent<T>(IDynamicLoadableContent<T> dynamicLoadableContent) where T : class
        {
            //Check if content is null
            if (dynamicLoadableContent == null)
            {
                Debug.LogWarning("SaveManager: Content is null");
                return;
            }

            var gameSave = SaveSystem.Load<GameSave>(CurrentSaveName);

            //Check if content is already registered
            var guid = dynamicLoadableContent.GetGuid();

            if (gameSave.DynamicData.ContainsKey(guid))
                gameSave.DynamicData[guid] = dynamicLoadableContent.Save();
            else
                gameSave.DynamicData.Add(guid, dynamicLoadableContent.Save());

            SaveSystem.Save(gameSave, CurrentSaveName);
        }

        public static void LoadDynamicContent<T>(IDynamicLoadableContent<T> dynamicLoadableContent) where T : class
        {
            //Check if content is null
            if (dynamicLoadableContent == null)
            {
                Debug.LogWarning("SaveManager: Content is null");
                return;
            }

            var gameSave = SaveSystem.Load<GameSave>(CurrentSaveName);

            //Check if content is already registered
            var guid = dynamicLoadableContent.GetGuid();

            if (gameSave.DynamicData.TryGetValue(guid, out var value))
            {
                var type = typeof(T);
                //Try to deserialize data
                try
                {
                    if (value.GetType() != type)
                        //We need to deserialize data
                        value = JsonConvert.DeserializeObject(value.ToString(), type);

                    dynamicLoadableContent.Load(value as T);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public static T LoadContent<T>() where T : SaveData
        {
            var data = SaveSystem.Load(typeof(T), CurrentSaveName);

            if (data == null)
            {
                Debug.LogWarning("SaveManager: Data is null");
                return default;
            }

            return (T)data;
        }

        public static SaveData LoadContent(Type type)
        {
            return SaveSystem.Load(type, CurrentSaveName);
        }

        public static void SaveContent<T>(ILoadableContent<T> holder) where T : SaveData
        {
            var data = SaveSystem.Load(typeof(T), CurrentSaveName);
            var saveData = holder.Save(data as T);
            SaveSystem.Save(saveData, CurrentSaveName);

            var saveManager = ServiceLocator.GetService<SaveManager>();

            if (saveManager != null)
                saveManager.OnGameSaved?.Invoke();
        }

        public bool HasSaveFile()
        {
            return SaveSystem.DoesSaveExist();
        }
    }
}