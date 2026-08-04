using System;
using System.Collections.Generic;
using System.Linq;
using ET;
using Game.AssetCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.AssetAdapters
{
    public sealed class ResourcesAssetLoader : IAssetLoader
    {
        private readonly Dictionary<string, List<ResourceLease>> _leases =
            new Dictionary<string, List<ResourceLease>>();

        public AssetBackendType Backend => AssetBackendType.Resources;
        public bool SupportsHotUpdate => false;

        public async ETTask InitializeAsync(AssetRuntimeOptions options)
        {
            await ETTask.CompletedTask;
        }

        public async ETTask<bool> InitializePackageAsync(string packageName)
        {
            await ETTask.CompletedTask;
            return true;
        }

        public void Tick()
        {
        }

        public void Dispose()
        {
            foreach (var lease in _leases.Values.SelectMany(value => value).ToArray())
                Release(lease);
            _leases.Clear();
        }

        public T Load<T>(string assetPath, string packageName = null) where T : Object
        {
            var lease = LoadLease<T>(assetPath);
            return lease?.Asset as T;
        }

        public async ETTask<T> LoadAsync<T>(string assetPath, string packageName = null) where T : Object
        {
            var lease = await LoadLeaseAsync<T>(assetPath);
            return lease?.Asset as T;
        }

        public async ETTask<IAssetHandle<T>> LoadHandleAsync<T>(string assetPath, string packageName = null)
            where T : Object
        {
            var lease = await LoadLeaseAsync<T>(assetPath);
            var asset = lease?.Asset as T;
            return new AssetHandle<T>(assetPath, asset, () => Release(lease));
        }

        public async ETTask<ISceneHandle> LoadSceneAsync(string scenePath, string packageName = null,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            string normalizedScenePath = scenePath?.Replace('\\', '/');
            int buildIndex = string.IsNullOrEmpty(normalizedScenePath)
                ? -1
                : SceneUtility.GetBuildIndexByScenePath(normalizedScenePath);
            if (buildIndex < 0)
            {
                string error = $"[Resources] scene not in Build Settings: {scenePath}";
                Debug.LogError(error);
                await ETTask.CompletedTask;
                return new SceneHandle(scenePath, succeeded: false, error: error);
            }

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(buildIndex, mode);
            }
            catch (Exception e)
            {
                string error = $"[Resources] LoadSceneAsync failed: {scenePath}, {e.Message}";
                Debug.LogError(error);
                return new SceneHandle(scenePath, succeeded: false, error: error);
            }

            if (op == null)
            {
                string error = $"[Resources] LoadSceneAsync returned null: {scenePath}";
                Debug.LogError(error);
                return new SceneHandle(scenePath, succeeded: false, error: error);
            }

            var tcs = ETTask.Create(true);
            op.completed += _ => tcs.SetResult();
            await tcs;
            return new SceneHandle(scenePath, () => op.progress, () => op.isDone,
                () => UnloadSceneIfLoaded(scenePath, mode));
        }

        public void Unload(string assetPath, string packageName = null)
        {
            var key = MakeKey(assetPath);
            if (_leases.TryGetValue(key, out var leases) && leases.Count > 0)
                Release(leases[leases.Count - 1]);
        }

        public void Unload(IAssetHandle handle)
        {
            handle?.Dispose();
        }

        public bool Exists(string assetPath, string packageName = null)
        {
            if (!AssetPathUtility.TryToResourcesPath(assetPath, out var resPath))
                return false;
            return Resources.Load(resPath) != null;
        }

        public async ETTask<IUpdateInfo> CheckUpdateAsync(IReadOnlyDictionary<string, bool> packages)
        {
            await ETTask.CompletedTask;
            return new UpdateInfo
            {
                NeedUpdate = false,
                TotalBytes = 0,
                Packages = packages != null ? new List<string>(packages.Keys) : new List<string>(),
            };
        }

        public async ETTask DownloadUpdateAsync(IUpdateInfo info, IProgress<UpdateProgress> progress = null)
        {
            progress?.Report(new UpdateProgress { Percent = 100f });
            await ETTask.CompletedTask;
        }

        private ResourceLease LoadLease<T>(string assetPath) where T : Object
        {
            if (!AssetPathUtility.TryToResourcesPath(assetPath, out var resPath))
            {
                Debug.LogError($"[Resources] path not under Assets/Resources/: {assetPath}");
                return null;
            }

            var asset = Resources.Load<T>(resPath);
            if (asset == null)
            {
                Debug.LogError($"[Resources] Load failed: {assetPath}");
                return null;
            }

            return Track(assetPath, asset);
        }

        private async ETTask<ResourceLease> LoadLeaseAsync<T>(string assetPath) where T : Object
        {
            if (!AssetPathUtility.TryToResourcesPath(assetPath, out var resPath))
            {
                Debug.LogError($"[Resources] path not under Assets/Resources/: {assetPath}");
                await ETTask.CompletedTask;
                return null;
            }

            var tcs = ETTask<ResourceLease>.Create(true);
            var req = Resources.LoadAsync<T>(resPath);
            req.completed += _ =>
            {
                var asset = req.asset as T;
                if (asset == null)
                {
                    Debug.LogError($"[Resources] LoadAsync failed: {assetPath}");
                    tcs.SetResult(null);
                    return;
                }

                tcs.SetResult(Track(assetPath, asset));
            };
            return await tcs;
        }

        private ResourceLease Track(string assetPath, Object asset)
        {
            var key = MakeKey(assetPath);
            if (!_leases.TryGetValue(key, out var leases))
            {
                leases = new List<ResourceLease>();
                _leases.Add(key, leases);
            }

            var lease = new ResourceLease(key, asset);
            leases.Add(lease);
            return lease;
        }

        private void Release(ResourceLease lease)
        {
            if (lease == null || lease.Released)
                return;

            lease.Released = true;
            if (_leases.TryGetValue(lease.Key, out var leases))
            {
                leases.Remove(lease);
                if (leases.Count == 0)
                    _leases.Remove(lease.Key);
            }

            bool stillReferenced = _leases.Values
                .SelectMany(value => value)
                .Any(other => ReferenceEquals(other.Asset, lease.Asset));
            if (!stillReferenced && lease.Asset != null)
                Resources.UnloadAsset(lease.Asset);
        }

        private static void UnloadSceneIfLoaded(string scenePath, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && SceneManager.sceneCount <= 1)
                return;

            var scene = SceneManager.GetSceneByPath(scenePath.Replace('\\', '/'));
            if (!scene.IsValid())
                scene = SceneManager.GetSceneByName(AssetPathUtility.GetSceneName(scenePath));
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
        }

        private static string MakeKey(string assetPath)
        {
            return assetPath?.Replace('\\', '/');
        }

        private sealed class ResourceLease
        {
            public ResourceLease(string key, Object asset)
            {
                Key = key;
                Asset = asset;
            }

            public string Key { get; }
            public Object Asset { get; }
            public bool Released { get; set; }
        }
    }
}
