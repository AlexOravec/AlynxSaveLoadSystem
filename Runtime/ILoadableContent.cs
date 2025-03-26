using System;

namespace AlynxSaveLoadSystem.Runtime
{
    public interface ILoadableContent<T> : ILoadableContent where T : SaveData
    {
        Type ILoadableContent.GetContentType()
        {
            return typeof(T);
        }

        public T Save(T data);
        public void Load(T data);
    }

    public interface ILoadableContent
    {
        bool HasFinishedLoading();

        Type GetContentType();
    }
}