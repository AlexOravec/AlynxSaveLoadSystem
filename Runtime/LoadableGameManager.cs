using System;
using AlynxServicesManager.Runtime;
using UnityEngine;

namespace AlynxSaveLoadSystem.Runtime
{
    public abstract class LoadableGameManager<T> : GameManager, ILoadableContent<T> where T : SaveData
    {
        private bool _isLoaded;

        public T Save(T data)
        {
            try
            {
                return HandleSave(data);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save game save: " + e + " on " + GetType());
                return null;
            }
        }

        public void Load(T data)
        {
            _isLoaded = false;

            try
            {
                if (HandleLoad(data)) _isLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load game save: " + e + " on " + GetType());
            }

            finally
            {
                _isLoaded = true;
            }
        }

        public bool HasFinishedLoading()
        {
            return _isLoaded;
        }

        protected void ForceLoad()
        {
            var gameSave = SaveManager.LoadContent<T>();

            if (gameSave == null) return;

            Load(gameSave);
        }

        public void ForceSave()
        {
            SaveManager.SaveContent(this);
        }

        protected abstract T HandleSave(T data);

        protected abstract bool HandleLoad(T data);
    }
}