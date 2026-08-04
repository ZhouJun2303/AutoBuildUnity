using System;
using System.Collections.Generic;
using ET;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.AssetCore
{
    /// <summary>
    /// 资源门面。启动时 Bootstrap 一次，业务侧只调本类。
    /// </summary>
    public static class AssetService
    {
        public static IAssetLoader Loader { get; private set; }
        public static AssetRuntimeOptions Options { get; private set; }
        public static AssetBackendType Backend => Loader != null ? Loader.Backend : AssetBackendType.BundleMaster;
        public static bool SupportsHotUpdate => Loader != null && Loader.SupportsHotUpdate;
        public static bool IsBootstrapped => Loader != null;

        public static void Bootstrap(IAssetLoader loader, AssetRuntimeOptions options = null)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            if (Loader != null)
                throw new InvalidOperationException("AssetService already bootstrapped. Restart app to switch backend.");

            Loader = loader;
            Options = options ?? new AssetRuntimeOptions();
            Debug.Log($"[AssetService] Bootstrap backend={Loader.Backend}, defaultPackage={Options.DefaultPackageName}");
        }

        public static void ResetForTests()
        {
            Loader?.Dispose();
            Loader = null;
            Options = null;
        }

        private static void EnsureReady()
        {
            if (Loader == null)
                throw new InvalidOperationException("AssetService not bootstrapped. Call Bootstrap in LaunchAOT first.");
        }

        public static string ResolvePackage(string packageName)
        {
            EnsureReady();
            return string.IsNullOrEmpty(packageName) ? Options.DefaultPackageName : packageName;
        }

        public static ETTask InitializeAsync()
        {
            EnsureReady();
            return Loader.InitializeAsync(Options);
        }

        public static ETTask<bool> InitializePackageAsync(string packageName = null)
        {
            EnsureReady();
            return Loader.InitializePackageAsync(ResolvePackage(packageName));
        }

        public static void Tick()
        {
            Loader?.Tick();
        }

        public static T Load<T>(string assetPath, string packageName = null) where T : Object
        {
            EnsureReady();
            return Loader.Load<T>(assetPath, ResolvePackage(packageName));
        }

        public static ETTask<T> LoadAsync<T>(string assetPath, string packageName = null) where T : Object
        {
            EnsureReady();
            return Loader.LoadAsync<T>(assetPath, ResolvePackage(packageName));
        }

        public static ETTask<IAssetHandle<T>> LoadHandleAsync<T>(string assetPath, string packageName = null)
            where T : Object
        {
            EnsureReady();
            return Loader.LoadHandleAsync<T>(assetPath, ResolvePackage(packageName));
        }

        public static ETTask<ISceneHandle> LoadSceneAsync(string scenePath, string packageName = null,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            EnsureReady();
            return Loader.LoadSceneAsync(scenePath, ResolvePackage(packageName), mode);
        }

        public static void Unload(string assetPath, string packageName = null)
        {
            EnsureReady();
            Loader.Unload(assetPath, ResolvePackage(packageName));
        }

        public static void Unload(IAssetHandle handle)
        {
            EnsureReady();
            Loader.Unload(handle);
        }

        public static bool Exists(string assetPath, string packageName = null)
        {
            EnsureReady();
            return Loader.Exists(assetPath, ResolvePackage(packageName));
        }

        public static ETTask<IUpdateInfo> CheckUpdateAsync(IReadOnlyDictionary<string, bool> packages)
        {
            EnsureReady();
            return Loader.CheckUpdateAsync(packages);
        }

        public static ETTask DownloadUpdateAsync(IUpdateInfo info, IProgress<UpdateProgress> progress = null)
        {
            EnsureReady();
            return Loader.DownloadUpdateAsync(info, progress);
        }
    }
}
