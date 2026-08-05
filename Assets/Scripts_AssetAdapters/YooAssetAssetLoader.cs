using System;
using System.Collections.Generic;
using System.Linq;
using ET;
using Game.AssetCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Object = UnityEngine.Object;
using YooAssetHandle = YooAsset.AssetHandle;
using YooSceneHandle = YooAsset.SceneHandle;
using CoreSceneHandle = Game.AssetCore.SceneHandle;

namespace Game.AssetAdapters
{
    public sealed class YooAssetAssetLoader : IAssetLoader
    {
        private AssetRuntimeOptions _options;
        private readonly Dictionary<string, List<YooHandleLease>> _handles =
            new Dictionary<string, List<YooHandleLease>>();
        private readonly HashSet<string> _initedPackages = new HashSet<string>();
        private HostRemoteService _remoteService;

        public AssetBackendType Backend => AssetBackendType.YooAsset;
        public bool SupportsHotUpdate => _options != null && _options.YooPlayMode == YooPlayModeKind.Host;

        public async ETTask InitializeAsync(AssetRuntimeOptions options)
        {
            _options = options ?? new AssetRuntimeOptions();
            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();
            await ETTask.CompletedTask;
        }

        public async ETTask<bool> InitializePackageAsync(string packageName)
        {
            if (_initedPackages.Contains(packageName))
                return true;

            if (!YooAssets.TryGetPackage(packageName, out var package) || package == null)
                package = YooAssets.CreatePackage(packageName);

            if (package.InitializeStatus != EOperationStatus.Succeeded)
            {
                InitializePackageOptions initOptions = BuildInitOptions();
                var op = package.InitializePackageAsync(initOptions);
                await AwaitOp(op);
                if (op.Status != EOperationStatus.Succeeded)
                {
                    Debug.LogError($"[YooAsset] InitializePackage failed: {packageName}, {op.Error}");
                    return false;
                }
            }

            // YooAsset 3.x：Initialize 成功后仍没有 Active Manifest。
            // Offline / Host / EditorSimulate 都需要：
            //   RequestPackageVersion → LoadPackageManifest
            // 否则加载资源会抛 YooPackageInvalidException: Active package manifest not found.
            // （与官方 Sample SpaceShooter FsmRequestPackageVersion / FsmUpdatePackageManifest 一致）
            if (!await LoadActiveManifestAsync(package, packageName))
                return false;

            _initedPackages.Add(packageName);
            return true;
        }

        /// <summary>
        /// 请求版本并加载当前激活清单（所有 PlayMode 共用）。
        /// Offline：从 StreamingAssets/yoo 读版本与清单；
        /// Host：优先远端，失败逻辑由 Yoo 内部处理；
        /// EditorSimulate：从模拟文件系统读。
        /// </summary>
        private async ETTask<bool> LoadActiveManifestAsync(ResourcePackage package, string packageName)
        {
            var versionOp = package.RequestPackageVersionAsync();
            await AwaitOp(versionOp);
            if (versionOp.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError(
                    $"[YooAsset] RequestPackageVersion failed: {packageName}, {versionOp.Error}\n" +
                    "Offline 请确认 StreamingAssets/yoo/{Package} 下有 .version 与 BuiltinCatalog；" +
                    "Host 请确认 BundleServerUrl 与远端版本文件可访问。");
                return false;
            }

            var manifestOp = package.LoadPackageManifestAsync(
                new LoadPackageManifestOptions(versionOp.PackageVersion, 60));
            await AwaitOp(manifestOp);
            if (manifestOp.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError(
                    $"[YooAsset] LoadPackageManifest failed: {packageName}, version={versionOp.PackageVersion}, {manifestOp.Error}");
                return false;
            }

            Debug.Log($"[YooAsset] Package ready: {packageName}, version={versionOp.PackageVersion}, mode={_options.YooPlayMode}");
            return true;
        }

        public void Tick()
        {
        }

        public void Dispose()
        {
            foreach (var lease in _handles.Values.SelectMany(value => value).ToArray())
                Release(lease);
            _handles.Clear();
            _initedPackages.Clear();
        }

        public T Load<T>(string assetPath, string packageName = null) where T : Object
        {
            var package = GetPackage(packageName);
            var handle = package.LoadAssetSync<T>(assetPath);
            if (handle.Status != EOperationStatus.Succeeded)
            {
                string error = handle.Error;
                handle.Release();
                Debug.LogError($"[YooAsset] Load failed: {assetPath}, {error}");
                return null;
            }

            Track(assetPath, packageName, handle);
            return handle.GetAssetObject<T>();
        }

        public async ETTask<T> LoadAsync<T>(string assetPath, string packageName = null) where T : Object
        {
            var handle = await LoadYooHandleAsync<T>(assetPath, packageName);
            if (handle == null)
                return null;
            Track(assetPath, packageName, handle);
            return handle.GetAssetObject<T>();
        }

        public async ETTask<IAssetHandle<T>> LoadHandleAsync<T>(string assetPath, string packageName = null)
            where T : Object
        {
            var yoo = await LoadYooHandleAsync<T>(assetPath, packageName);
            var asset = yoo != null ? yoo.GetAssetObject<T>() : null;
            var lease = Track(assetPath, packageName, yoo);
            return new Game.AssetCore.AssetHandle<T>(assetPath, asset, () => Release(lease));
        }

        public async ETTask<ISceneHandle> LoadSceneAsync(string scenePath, string packageName = null,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            var package = GetPackage(packageName);
            YooSceneHandle sceneHandle = package.LoadSceneAsync(scenePath, mode);
            await AwaitSceneHandle(sceneHandle);
            if (sceneHandle.Status != EOperationStatus.Succeeded)
            {
                string error = $"[YooAsset] LoadScene failed: {scenePath}, {sceneHandle.Error}";
                Debug.LogError(error);
                sceneHandle.Release();
                return new CoreSceneHandle(scenePath, succeeded: false, error: error);
            }

            return new CoreSceneHandle(scenePath,
                () => sceneHandle.Progress,
                () => sceneHandle.IsDone,
                () => sceneHandle.UnloadSceneAsync());
        }

        public void Unload(string assetPath, string packageName = null)
        {
            var key = MakeKey(assetPath, packageName);
            if (_handles.TryGetValue(key, out var leases) && leases.Count > 0)
            {
                Release(leases[leases.Count - 1]);
            }
            else
            {
                GetPackage(packageName).TryUnloadUnusedAsset(assetPath);
            }
        }

        public void Unload(IAssetHandle handle)
        {
            handle?.Dispose();
        }

        public bool Exists(string assetPath, string packageName = null)
        {
            try
            {
                return GetPackage(packageName).IsLocationValid(assetPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[YooAsset] Exists check failed: {e.Message}");
                return false;
            }
        }

        public async ETTask<IUpdateInfo> CheckUpdateAsync(IReadOnlyDictionary<string, bool> packages)
        {
            if (_options.YooPlayMode != YooPlayModeKind.Host)
            {
                await ETTask.CompletedTask;
                return new UpdateInfo
                {
                    NeedUpdate = false,
                    Packages = packages?.Keys.ToList() ?? new List<string>(),
                };
            }

            long total = 0;
            var packageNames = packages?.Keys.ToList() ?? new List<string>();
            var downloaders = new List<ResourceDownloaderOperation>();

            foreach (var name in packageNames)
            {
                if (!_initedPackages.Contains(name))
                {
                    bool initialized = await InitializePackageAsync(name);
                    if (!initialized)
                        throw new InvalidOperationException($"[YooAsset] package initialization failed: {name}");
                }

                var package = GetPackage(name);
                var versionOp = package.RequestPackageVersionAsync();
                await AwaitOp(versionOp);
                if (versionOp.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException(
                        $"[YooAsset] CheckUpdate version failed: {name}, {versionOp.Error}");

                var manifestOp = package.LoadPackageManifestAsync(
                    new LoadPackageManifestOptions(versionOp.PackageVersion, 60));
                await AwaitOp(manifestOp);
                if (manifestOp.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException(
                        $"[YooAsset] CheckUpdate manifest failed: {name}, {manifestOp.Error}");

                var downloader = package.CreateResourceDownloader(new ResourceDownloaderOptions(10, 3));
                if (downloader.TotalDownloadBytes > 0)
                {
                    total += downloader.TotalDownloadBytes;
                    downloaders.Add(downloader);
                }
            }

            return new UpdateInfo
            {
                NeedUpdate = total > 0,
                TotalBytes = total,
                Packages = packageNames,
                Native = downloaders,
            };
        }

        public async ETTask DownloadUpdateAsync(IUpdateInfo info, IProgress<UpdateProgress> progress = null)
        {
            if (info == null || !info.NeedUpdate)
            {
                await ETTask.CompletedTask;
                return;
            }

            var downloaders = (info as UpdateInfo)?.Native as List<ResourceDownloaderOperation>;
            if (downloaders == null || downloaders.Count == 0)
                throw new InvalidOperationException("[YooAsset] DownloadUpdateAsync missing downloader context");

            long total = info.TotalBytes;
            long done = 0;
            foreach (var downloader in downloaders)
            {
                downloader.DownloadProgressChanged += args =>
                {
                    progress?.Report(new UpdateProgress
                    {
                        Percent = total > 0 ? (done + args.CurrentDownloadBytes) * 100f / total : 100f,
                        CurrentBytes = done + args.CurrentDownloadBytes,
                        TotalBytes = total,
                    });
                };
                downloader.StartDownload();
                await AwaitOp(downloader);
                if (downloader.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException(
                        $"[YooAsset] Download failed: {downloader.Error}");
                done += downloader.TotalDownloadBytes;
            }

            progress?.Report(new UpdateProgress
            {
                Percent = 100f,
                CurrentBytes = total,
                TotalBytes = total,
            });
        }

        private InitializePackageOptions BuildInitOptions()
        {
            switch (_options.YooPlayMode)
            {
                case YooPlayModeKind.EditorSimulate:
#if UNITY_EDITOR
                    if (!string.IsNullOrEmpty(_options.YooEditorSimulateRoot))
                    {
                        return new EditorSimulateModeOptions
                        {
                            EditorFileSystemParameters =
                                FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                                    _options.YooEditorSimulateRoot),
                        };
                    }
#endif
                    Debug.LogWarning("[YooAsset] EditorSimulate root empty, fallback Offline");
                    return new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                    };
                case YooPlayModeKind.Host:
                    _remoteService = new HostRemoteService(_options.BundleServerUrl);
                    return new HostPlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                        CacheFileSystemParameters =
                            FileSystemParameters.CreateDefaultSandboxFileSystemParameters(_remoteService),
                    };
                case YooPlayModeKind.Offline:
                default:
                    return new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                    };
            }
        }

        private async ETTask<YooAssetHandle> LoadYooHandleAsync<T>(string assetPath, string packageName)
            where T : Object
        {
            var package = GetPackage(packageName);
            var handle = package.LoadAssetAsync<T>(assetPath);
            await AwaitAssetHandle(handle);
            if (!handle.IsValid || handle.Status != EOperationStatus.Succeeded)
            {
                string error = handle.IsValid ? handle.Error : "invalid handle";
                Debug.LogError($"[YooAsset] Load failed: {assetPath}, {error}");
                if (handle.IsValid)
                    handle.Release();
                return null;
            }

            return handle;
        }

        private ResourcePackage GetPackage(string packageName)
        {
            var name = string.IsNullOrEmpty(packageName) ? _options.DefaultPackageName : packageName;
            if (!YooAssets.TryGetPackage(name, out var package) || package == null)
                throw new InvalidOperationException($"[YooAsset] package not initialized: {name}");
            if (!_initedPackages.Contains(name))
                throw new InvalidOperationException($"[YooAsset] package manifest not ready: {name}");
            return package;
        }

        private YooHandleLease Track(string assetPath, string packageName, YooAssetHandle handle)
        {
            if (handle == null)
                return null;

            var key = MakeKey(assetPath, packageName);
            if (!_handles.TryGetValue(key, out var leases))
            {
                leases = new List<YooHandleLease>();
                _handles.Add(key, leases);
            }

            var lease = new YooHandleLease(key, handle);
            leases.Add(lease);
            return lease;
        }

        private void Release(YooHandleLease lease)
        {
            if (lease == null || lease.Released)
                return;

            lease.Released = true;
            if (lease.Handle.IsValid)
                lease.Handle.Release();
            if (_handles.TryGetValue(lease.Key, out var leases))
            {
                leases.Remove(lease);
                if (leases.Count == 0)
                    _handles.Remove(lease.Key);
            }
        }

        private static string MakeKey(string path, string package) => $"{package}|{path}";

        private static ETTask AwaitOp(AsyncOperationBase op)
        {
            var tcs = ETTask.Create(true);
            if (op.IsDone)
            {
                tcs.SetResult();
                return tcs;
            }

            op.Completed += _ => tcs.SetResult();
            return tcs;
        }

        private static ETTask AwaitAssetHandle(YooAssetHandle handle)
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

        private static ETTask AwaitSceneHandle(YooSceneHandle handle)
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

        private sealed class HostRemoteService : IRemoteService
        {
            private readonly string _host;

            public HostRemoteService(string host)
            {
                _host = host?.TrimEnd('/', '\\') ?? string.Empty;
            }

            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                return new[] { $"{_host}/{fileName}", $"{_host}/{fileName}" };
            }
        }

        private sealed class YooHandleLease
        {
            public YooHandleLease(string key, YooAssetHandle handle)
            {
                Key = key;
                Handle = handle;
            }

            public string Key { get; }
            public YooAssetHandle Handle { get; }
            public bool Released { get; set; }
        }
    }
}
