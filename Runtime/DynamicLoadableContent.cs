using System;
using UnityEditor;
using UnityEngine;
using Object = UnityGameplayFramework.Runtime.Object;

namespace AlynxSaveLoadSystem.Runtime
{
    public abstract class DynamicLoadableContent<T> : Object, IDynamicLoadableContent<T> where T : class
    {
        [SerializeField] private string guid;

# if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(guid) && Application.isEditor)
            {
                guid = Guid.NewGuid().ToString();

                EditorUtility.SetDirty(this);
            }
        }
#endif

        public string GetGuid()
        {
            return guid;
        }

        public abstract void Load(T data);

        public abstract T Save();

        protected override void BeginPlay()
        {
            base.BeginPlay();

            SaveManager.LoadDynamicContent(this);
        }

        protected void OnShouldSave()
        {
            SaveManager.SaveDynamicContent(this);
        }
    }
}