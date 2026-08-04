using System;
using System.Collections.Generic;
using System.Linq;
using BM;
using ET;
using Game.AssetCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.AssetAdapters
{
    public sealed class BundleMasterAssetLoader : IAssetLoader
    {
        private AssetRuntimeOptions _options;
        private readonly Dictionary<string, List<HandlerLease>> _pathHandlers =
            new Dictionary<string, List<HandlerLease>>();

        public AssetBackendType Backend => AssetBackendType.BundleMaster;
        public bool SupportsHotUpdate => AssetComponentConfig.AssetLoadMode == AssetLoadMode.Build;

        public async ETTask InitializeAsync(AssetRuntimeOptions options)
        {
            _options = options ?? new AssetRuntimeOptions();
            if (!string.IsNullOrEmpty(_options.DefaultPackageName))
                AssetComponentConfig.DefaultBundlePackageName = _options.DefaultPackageName;
            if (!string.IsNullOrEmpty(_options.BundleServerUrl))
                AssetComponentConfig.BundleServerUrl = _options.BundleServerUrl;
            await ETTask.CompletedTask;
        }

        public async ETTask<bool> InitializePackageAsync(string packageName)
        {
            return await AssetComponent.Initialize(packageName);
        }

        public void Tick()
        {
            AssetComponent.Update();
        }

        public void Dispose()
        {
            foreach (var lease in _pathHandlers.Values.SelectMany(value => value).ToArray())
                Release(lease);
            _pathHandlers.Clear();
        }

        public T Load<T>(string assetPath, string packageName = null) where T : Object
        {
            var asset = AssetComponent.Load<T>(out LoadHandler handler, assetPath, false, packageName);
            if (asset == null)
            {
                if (handler != null)
                    AssetComponent.UnLoad(handler);
                Debug.LogError($"[BundleMaster] Load failed: {assetPath}, package={packageName}");
                return null;
            }

            Track(assetPath, packageName, handler);
            return asset;
        }

        public async ETTask<T> LoadAsync<T>(string assetPath, string packageName = null) where T : Object
        {
            var task = AssetComponent.LoadAsync<T>(out LoadHandler handler, assetPath, false, packageName);
            var asset = await task;
            if (asset == null)
            {
                if (handler != null)
                    AssetComponent.UnLoad(handler);
                Debug.LogError($"[BundleMaster] LoadAsync failed: {assetPath}, package={packageName}");
                return null;
            }

            Track(assetPath, packageName, handler);
            return asset;
        }

        public async ETTask<IAssetHandle<T>> LoadHandleAsync<T>(string assetPath, string packageName = null)
            where T : Object
        {
            var task = AssetComponent.LoadAsync<T>(out LoadHandler handler, assetPath, false, packageName);
            var asset = await task;
            if (asset == null)
            {
                if (handler != null)
                    AssetComponent.UnLoad(handler);
                Debug.LogError($"[BundleMaster] LoadHandleAsync failed: {assetPath}, package={packageName}");
                return new AssetHandle<T>(assetPath, null);
            }

            var lease = Track(assetPath, packageName, handler);
            return new AssetHandle<T>(assetPath, asset, () => Release(lease));
        }

        public async ETTask<ISceneHandle> LoadSceneAsync(string scenePath, string packageName = null,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            LoadSceneHandler bmHandler = await AssetComponent.LoadSceneAsync(scenePath, packageName);
            if (bmHandler == null)
            {
                string error = $"[BundleMaster] scene bundle load failed: {scenePath}, package={packageName}";
                Debug.LogError(error);
                return new SceneHandle(scenePath, succeeded: false, error: error);
            }

            AsyncOperation op;
            try
            {
                op = SceneManager.LoadSceneAsync(scenePath, mode);
            }
            catch (Exception e)
            {
                bmHandler.UnLoad();
                string error = $"[BundleMaster] LoadSceneAsync failed: {scenePath}, {e.Message}";
                Debug.LogError(error);
                return new SceneHandle(scenePath, succeeded: false, error: error);
            }

            var tcs = ETTask.Create(true);
            if (op == null)
            {
                bmHandler.UnLoad();
                string error = $"[BundleMaster] LoadSceneAsync returned null: {scenePath}";
                Debug.LogError(error);
                return new SceneHandle(scenePath, succeeded: false, error: error);
            }

            op.completed += _ => tcs.SetResult();
            await tcs;

            return new SceneHandle(scenePath,
                () => op?.progress ?? 1f,
                () => op == null || op.isDone,
                () =>
                {
                    UnloadSceneIfLoaded(scenePath, mode);
                    bmHandler.UnLoad();
                });
        }

        public void Unload(string assetPath, string packageName = null)
        {
            var key = MakeKey(assetPath, packageName);
            if (_pathHandlers.TryGetValue(key, out var leases) && leases.Count > 0)
            {
                Release(leases[leases.Count - 1]);
            }
            else
            {
                AssetComponent.UnLoadByPath(assetPath, packageName);
            }
        }

        public void Unload(IAssetHandle handle)
        {
            handle?.Dispose();
        }

        public bool Exists(string assetPath, string packageName = null)
        {
            return AssetComponent.CheckAssetExist(assetPath, packageName);
        }

        public async ETTask<IUpdateInfo> CheckUpdateAsync(IReadOnlyDictionary<string, bool> packages)
        {
            if (!string.IsNullOrEmpty(_options?.BundleServerUrl))
                AssetComponentConfig.BundleServerUrl = _options.BundleServerUrl;
            if (!string.IsNullOrEmpty(_options?.DefaultPackageName))
                AssetComponentConfig.DefaultBundlePackageName = _options.DefaultPackageName;

            var dict = packages?.ToDictionary(kv => kv.Key, kv => kv.Value)
                       ?? new Dictionary<string, bool>();
            UpdateBundleDataInfo data = await AssetComponent.CheckAllBundlePackageUpdate(dict);
            if (data == null)
                throw new InvalidOperationException("[BundleMaster] CheckAllBundlePackageUpdate returned null");
            return new UpdateInfo
            {
                NeedUpdate = data.NeedUpdate,
                TotalBytes = data.NeedUpdateSize,
                Packages = dict.Keys.ToList(),
                Native = data,
            };
        }

        public async ETTask DownloadUpdateAsync(IUpdateInfo info, IProgress<UpdateProgress> progress = null)
        {
            if (info == null || !info.NeedUpdate)
            {
                await ETTask.CompletedTask;
                return;
            }

            var data = info is UpdateInfo ui ? ui.Native as UpdateBundleDataInfo : null;
            if (data == null)
                throw new InvalidOperationException("[BundleMaster] DownloadUpdateAsync missing UpdateBundleDataInfo");
            if (AssetComponentConfig.AssetLoadMode != AssetLoadMode.Build)
                throw new InvalidOperationException("[BundleMaster] Hot update requires AssetLoadMode.Build");

            bool failed = false;
            Action onError = () => failed = true;
            Action<float> onProgress = null;
            data.ErrorCancelCallback += onError;
            if (progress != null)
            {
                onProgress = p =>
                {
                    progress.Report(new UpdateProgress
                    {
                        Percent = p,
                        CurrentBytes = data.FinishUpdateSize,
                        TotalBytes = data.NeedUpdateSize,
                        SpeedBytesPerSec = data.DownLoadSpeed,
                    });
                };
                data.ProgressCallback += onProgress;
            }

            try
            {
                await AssetComponent.DownLoadUpdate(data);
                if (failed)
                    throw new InvalidOperationException("[BundleMaster] Asset update was cancelled after a download error");
                progress?.Report(new UpdateProgress
                {
                    Percent = 100f,
                    CurrentBytes = data.NeedUpdateSize,
                    TotalBytes = data.NeedUpdateSize,
                });
            }
            finally
            {
                data.ErrorCancelCallback -= onError;
                if (onProgress != null)
                    data.ProgressCallback -= onProgress;
            }
        }

        private static string MakeKey(string path, string package) => $"{package}|{path}";

        private HandlerLease Track(string assetPath, string packageName, LoadHandler handler)
        {
            if (handler == null)
                return null;

            var key = MakeKey(assetPath, packageName);
            if (!_pathHandlers.TryGetValue(key, out var leases))
            {
                leases = new List<HandlerLease>();
                _pathHandlers.Add(key, leases);
            }

            var lease = new HandlerLease(key, handler);
            leases.Add(lease);
            return lease;
        }

        private void Release(HandlerLease lease)
        {
            if (lease == null || lease.Released)
                return;

            lease.Released = true;
            AssetComponent.UnLoad(lease.Handler);
            if (_pathHandlers.TryGetValue(lease.Key, out var leases))
            {
                leases.Remove(lease);
                if (leases.Count == 0)
                    _pathHandlers.Remove(lease.Key);
            }
        }

        private static void UnloadSceneIfLoaded(string scenePath, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && SceneManager.sceneCount <= 1)
                return;

            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid())
                scene = SceneManager.GetSceneByName(AssetPathUtility.GetSceneName(scenePath));
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
        }

        private sealed class HandlerLease
        {
            public HandlerLease(string key, LoadHandler handler)
            {
                Key = key;
                Handler = handler;
            }

            public string Key { get; }
            public LoadHandler Handler { get; }
            public bool Released { get; set; }
        }
    }
}
