using System;

namespace AlynxSaveLoadSystem.Runtime
{
    public interface IDynamicLoadableContent<T> where T : class
    {
        public string GetGuid();
        public void Load(T data);
        public T Save();

        public Type GetContentType()
        {
            return typeof(T);
        }
    }
}