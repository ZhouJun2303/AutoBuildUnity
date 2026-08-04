using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Game.AssetCore
{
    public sealed class AssetHandle<T> : IAssetHandle<T> where T : Object
    {
        private Action _onDispose;
        private bool _disposed;

        public AssetHandle(string assetPath, T asset, Action onDispose = null)
        {
            AssetPath = assetPath;
            Asset = asset;
            _onDispose = onDispose;
        }

        public string AssetPath { get; }
        public T Asset { get; private set; }
        public bool IsValid => !_disposed && Asset != null;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _onDispose?.Invoke();
            _onDispose = null;
            Asset = null;
        }
    }

    public sealed class SceneHandle : ISceneHandle
    {
        private Action _onDispose;
        private Func<float> _progressGetter;
        private Func<bool> _isDoneGetter;
        private bool _disposed;

        public SceneHandle(string scenePath, Func<float> progressGetter = null, Func<bool> isDoneGetter = null,
            Action onDispose = null, bool succeeded = true, string error = null)
        {
            ScenePath = scenePath;
            _progressGetter = progressGetter;
            _isDoneGetter = isDoneGetter;
            _onDispose = onDispose;
            Succeeded = succeeded;
            Error = error;
        }

        public string ScenePath { get; }
        public float Progress => _progressGetter?.Invoke() ?? (IsDone ? 1f : 0f);
        public bool IsDone => _isDoneGetter?.Invoke() ?? true;
        public bool Succeeded { get; }
        public string Error { get; }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _onDispose?.Invoke();
            _onDispose = null;
            _progressGetter = null;
            _isDoneGetter = null;
        }
    }

    public sealed class UpdateInfo : IUpdateInfo
    {
        public bool NeedUpdate { get; set; }
        public long TotalBytes { get; set; }
        public IReadOnlyList<string> Packages { get; set; } = Array.Empty<string>();
        /// <summary>适配器内部原生对象（如 BM UpdateBundleDataInfo）</summary>
        public object Native { get; set; }
    }
}
