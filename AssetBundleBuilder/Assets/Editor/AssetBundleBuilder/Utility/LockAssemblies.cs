using UnityEditor;
using System;

namespace AssetBundleBuilder
{
    public class LockAssemblies : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public LockAssemblies()
        {
            EditorApplication.LockReloadAssemblies();
            IsDisposed = false;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                EditorApplication.UnlockReloadAssemblies();
                IsDisposed = true;
            }
        }
    }
}