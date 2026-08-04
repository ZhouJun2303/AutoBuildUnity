using System;
using System.Collections.Generic;
using ET;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.AssetCore
{
    public interface IAssetLoader
    {
        AssetBackendType Backend { get; }
        bool SupportsHotUpdate { get; }

        ETTask InitializeAsync(AssetRuntimeOptions options);
        ETTask<bool> InitializePackageAsync(string packageName);
        void Tick();
        void Dispose();

        T Load<T>(string assetPath, string packageName = null) where T : Object;
        ETTask<T> LoadAsync<T>(string assetPath, string packageName = null) where T : Object;
        ETTask<IAssetHandle<T>> LoadHandleAsync<T>(string assetPath, string packageName = null) where T : Object;

        ETTask<ISceneHandle> LoadSceneAsync(string scenePath, string packageName = null,
            LoadSceneMode mode = LoadSceneMode.Single);

        void Unload(string assetPath, string packageName = null);
        void Unload(IAssetHandle handle);
        bool Exists(string assetPath, string packageName = null);

        ETTask<IUpdateInfo> CheckUpdateAsync(IReadOnlyDictionary<string, bool> packages);
        ETTask DownloadUpdateAsync(IUpdateInfo info, IProgress<UpdateProgress> progress = null);
    }
}
