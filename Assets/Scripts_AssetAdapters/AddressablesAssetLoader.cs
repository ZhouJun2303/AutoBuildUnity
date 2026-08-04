using System;
using System.Collections.Generic;
using System.Linq;
using ET;
using Game.AssetCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.AssetAdapters
{
    public sealed class AddressablesAssetLoader : IAssetLoader
    {
        private AssetRuntimeOptions _options;
        private readonly Dictionary<string, List<AddressableHandleLease>> _handles =
            new Dictionary<string, List<AddressableHandleLease>>();
        private bool _inited;

        public AssetBackendType Backend => AssetBackendType.Addressables;
        public bool SupportsHotUpdate => true;

        public async ETTask InitializeAsync(AssetRuntimeOptions options)
        {
            _options = options ?? new AssetRuntimeOptions();
            if (_inited)
            {
                await ETTask.CompletedTask;
                return;
            }

            var handle = Addressables.InitializeAsync();
            await AwaitHandle(handle);
            _inited = handle.Status == AsyncOperationStatus.Succeeded;
            if (!_inited)
            {
                var exception = handle.OperationException;
                if (handle.IsValid())
                    Addressables.Release(handle);
                throw new InvalidOperationException($"[Addressables] Initialize failed: {exception}", exception);
            }

            if (handle.IsValid())
                Addressables.Release(handle);
        }

        public async ETTask<bool> InitializePackageAsync(string packageName)
        {
            // Addressables 无分包 Initialize；确保系统已初始化
            if (!_inited)
                await InitializeAsync(_options);
            await ETTask.CompletedTask;
            return _inited;
        }

        public void Tick()
        {
        }

        public void Dispose()
        {
            foreach (var lease in _handles.Values.SelectMany(value => value).ToArray())
                Release(lease);
            _handles.Clear();
            _inited = false;
        }

        public T Load<T>(string assetPath, string packageName = null) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(assetPath);
            var asset = handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = handle.OperationException;
                if (handle.IsValid())
                    Addressables.Release(handle);
                Debug.LogError($"[Addressables] Load failed: {assetPath}, {exception}");
                return null;
            }

            Track(assetPath, packageName, handle);
            return asset;
        }

        public async ETTask<T> LoadAsync<T>(string assetPath, string packageName = null) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(assetPath);
            await AwaitHandle(handle);
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = handle.OperationException;
                if (handle.IsValid())
                    Addressables.Release(handle);
                Debug.LogError($"[Addressables] Load failed: {assetPath}, {exception}");
                return null;
            }

            Track(assetPath, packageName, handle);
            return handle.Result;
        }

        public async ETTask<IAssetHandle<T>> LoadHandleAsync<T>(string assetPath, string packageName = null)
            where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(assetPath);
            await AwaitHandle(handle);
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = handle.OperationException;
                if (handle.IsValid())
                    Addressables.Release(handle);
                Debug.LogError($"[Addressables] Load failed: {assetPath}, {exception}");
                return new AssetHandle<T>(assetPath, null);
            }

            var lease = Track(assetPath, packageName, handle);
            return new AssetHandle<T>(assetPath, handle.Result, () => Release(lease));
        }

        public async ETTask<ISceneHandle> LoadSceneAsync(string scenePath, string packageName = null,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            AsyncOperationHandle<SceneInstance> handle =
                Addressables.LoadSceneAsync(scenePath, mode);
            await AwaitHandle(handle);
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = handle.OperationException;
                string error = $"[Addressables] LoadScene failed: {scenePath}, {exception}";
                Debug.LogError(error);
                if (handle.IsValid())
                    Addressables.Release(handle);
                return new SceneHandle(scenePath, succeeded: false, error: error);
            }

            return new SceneHandle(scenePath,
                () => handle.PercentComplete,
                () => handle.IsDone,
                () =>
                {
                    if (handle.IsValid())
                        Addressables.UnloadSceneAsync(handle, true);
                });
        }

        public void Unload(string assetPath, string packageName = null)
        {
            var key = MakeKey(assetPath, packageName);
            if (_handles.TryGetValue(key, out var leases) && leases.Count > 0)
            {
                Release(leases[leases.Count - 1]);
            }
        }

        public void Unload(IAssetHandle handle)
        {
            handle?.Dispose();
        }

        public bool Exists(string assetPath, string packageName = null)
        {
            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(assetPath, typeof(Object), out var locations) && locations != null &&
                    locations.Count > 0)
                    return true;
            }

            return false;
        }

        public async ETTask<IUpdateInfo> CheckUpdateAsync(IReadOnlyDictionary<string, bool> packages)
        {
            var check = Addressables.CheckForCatalogUpdates(false);
            await AwaitHandle(check);
            if (check.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = check.OperationException;
                if (check.IsValid())
                    Addressables.Release(check);
                throw new InvalidOperationException("[Addressables] CheckForCatalogUpdates failed", exception);
            }

            var catalogs = check.Result?.ToList() ?? new List<string>();
            if (check.IsValid())
                Addressables.Release(check);
            if (catalogs.Count == 0)
            {
                return new UpdateInfo
                {
                    NeedUpdate = false,
                    TotalBytes = 0,
                    Packages = packages?.Keys.ToList() ?? new List<string>(),
                };
            }

            var update = Addressables.UpdateCatalogs(catalogs, false);
            await AwaitHandle(update);
            if (update.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = update.OperationException;
                if (update.IsValid())
                    Addressables.Release(update);
                throw new InvalidOperationException("[Addressables] UpdateCatalogs failed", exception);
            }

            var keys = update.Result?
                .Where(locator => locator != null)
                .SelectMany(locator => locator.Keys)
                .Distinct()
                .ToList() ?? new List<object>();
            if (update.IsValid())
                Addressables.Release(update);

            long totalBytes = 0;
            if (keys.Count > 0)
            {
                var size = Addressables.GetDownloadSizeAsync((System.Collections.IEnumerable)keys);
                await AwaitHandle(size);
                if (size.Status != AsyncOperationStatus.Succeeded)
                {
                    var exception = size.OperationException;
                    if (size.IsValid())
                        Addressables.Release(size);
                    throw new InvalidOperationException("[Addressables] GetDownloadSizeAsync failed", exception);
                }

                totalBytes = size.Result;
                if (size.IsValid())
                    Addressables.Release(size);
            }

            return new UpdateInfo
            {
                NeedUpdate = true,
                TotalBytes = totalBytes,
                Packages = packages?.Keys.ToList() ?? new List<string>(),
                Native = new AddressablesUpdateContext(keys),
            };
        }

        public async ETTask DownloadUpdateAsync(IUpdateInfo info, IProgress<UpdateProgress> progress = null)
        {
            if (info == null || !info.NeedUpdate)
            {
                await ETTask.CompletedTask;
                return;
            }

            var context = (info as UpdateInfo)?.Native as AddressablesUpdateContext;
            if (context == null)
                throw new InvalidOperationException("[Addressables] DownloadUpdateAsync missing update context");
            if (context.Keys.Count == 0 || info.TotalBytes <= 0)
            {
                progress?.Report(new UpdateProgress { Percent = 100f, TotalBytes = info.TotalBytes });
                await ETTask.CompletedTask;
                return;
            }

            var download = Addressables.DownloadDependenciesAsync(
                (System.Collections.IEnumerable)context.Keys, Addressables.MergeMode.Union, false);
            await AwaitHandle(download);
            if (download.Status != AsyncOperationStatus.Succeeded)
            {
                var exception = download.OperationException;
                if (download.IsValid())
                    Addressables.Release(download);
                throw new InvalidOperationException("[Addressables] DownloadDependenciesAsync failed", exception);
            }

            if (download.IsValid())
                Addressables.Release(download);
            progress?.Report(new UpdateProgress
            {
                Percent = 100f,
                CurrentBytes = info.TotalBytes,
                TotalBytes = info.TotalBytes,
            });
        }

        private AddressableHandleLease Track(string assetPath, string packageName, AsyncOperationHandle handle)
        {
            var key = MakeKey(assetPath, packageName);
            if (!_handles.TryGetValue(key, out var leases))
            {
                leases = new List<AddressableHandleLease>();
                _handles.Add(key, leases);
            }

            var lease = new AddressableHandleLease(key, handle);
            leases.Add(lease);
            return lease;
        }

        private void Release(AddressableHandleLease lease)
        {
            if (lease == null || lease.Released)
                return;

            lease.Released = true;
            if (lease.Handle.IsValid())
                Addressables.Release(lease.Handle);
            if (_handles.TryGetValue(lease.Key, out var leases))
            {
                leases.Remove(lease);
                if (leases.Count == 0)
                    _handles.Remove(lease.Key);
            }
        }

        private static string MakeKey(string path, string package) => $"{package}|{path}";

        private static ETTask AwaitHandle(AsyncOperationHandle handle)
        {
            var tcs = ETTask.Create(true);
            if (handle.IsDone)
            {
                tcs.SetResult();
                return tcs;
            }

            handle.Completed += _ => tcs.SetResult();
            return tcs;
        }

        private sealed class AddressableHandleLease
        {
            public AddressableHandleLease(string key, AsyncOperationHandle handle)
            {
                Key = key;
                Handle = handle;
            }

            public string Key { get; }
            public AsyncOperationHandle Handle { get; }
            public bool Released { get; set; }
        }

        private sealed class AddressablesUpdateContext
        {
            public AddressablesUpdateContext(List<object> keys)
            {
                Keys = keys;
            }

            public List<object> Keys { get; }
        }
    }
}
