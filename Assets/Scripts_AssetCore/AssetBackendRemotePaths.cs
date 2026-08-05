using System.IO;

namespace Game.AssetCore
{
    /// <summary>
    /// 各资源后端的「远端请求根」与「IIS 磁盘根」映射。
    ///
    /// 约定（学习/本机热更）：
    ///   URL  根: http://192.168.18.62:8866/
    ///   磁盘根: C:\IIS_ServerData\
    ///
    /// 不同 AB 使用不同子目录，请求地址与拷贝路径必须一一对应：
    ///   BundleMaster  → .../BundleMaster/
    ///   YooAsset      → .../YooAsset/
    ///   Addressables  → .../Addressables/
    ///
    /// 运行时 version.txt 示例：
    ///   http://192.168.18.62:8866/BundleMaster/version.txt
    ///   磁盘 C:\IIS_ServerData\BundleMaster\version.txt
    ///
    /// 热更资源根（Launch 拼 BundleServerUrl）：
    ///   {RemoteUrl}/{version}/AssetBundles
    ///   例: http://192.168.18.62:8866/BundleMaster/4/AssetBundles
    /// </summary>
    public static class AssetBackendRemotePaths
    {
        /// <summary>本机 IIS HTTP 根。</summary>
        public const string RemoteBaseUrl = "http://192.168.18.62:8866";

        /// <summary>本机 IIS 物理根目录。</summary>
        public const string IisDiskRoot = @"C:\IIS_ServerData";

        public const string FolderBundleMaster = "BundleMaster";
        public const string FolderYooAsset = "YooAsset";
        public const string FolderAddressables = "Addressables";

        /// <summary>版本号文件名（与 GameConfig.VersionFileName 一致）。</summary>
        public const string VersionFileName = "version.txt";

        /// <summary>热更资源在版本目录下的子文件夹名（BM/Yoo 共用约定）。</summary>
        public const string AssetBundlesFolder = "AssetBundles";

        /// <summary>
        /// 后端在 IIS/URL 上的一级目录名。
        /// Resources 无远端，返回空。
        /// </summary>
        public static string GetBackendFolderName(AssetBackendType backend)
        {
            switch (backend)
            {
                case AssetBackendType.BundleMaster:
                    return FolderBundleMaster;
                case AssetBackendType.YooAsset:
                    return FolderYooAsset;
                case AssetBackendType.Addressables:
                    return FolderAddressables;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 运行时远端根 URL（含后端子目录，无末尾斜杠）。
        /// 例：http://192.168.18.62:8866/BundleMaster
        /// </summary>
        public static string GetRemoteUrl(AssetBackendType backend)
        {
            string folder = GetBackendFolderName(backend);
            if (string.IsNullOrEmpty(folder))
                return RemoteBaseUrl.TrimEnd('/');
            return $"{RemoteBaseUrl.TrimEnd('/')}/{folder}";
        }

        /// <summary>
        /// IIS 磁盘根（含后端子目录）。
        /// 例：C:\IIS_ServerData\BundleMaster
        /// </summary>
        public static string GetIisRoot(AssetBackendType backend)
        {
            string folder = GetBackendFolderName(backend);
            if (string.IsNullOrEmpty(folder))
                return IisDiskRoot;
            return Path.Combine(IisDiskRoot, folder);
        }

        /// <summary>
        /// version.txt 完整 URL。
        /// 例：http://192.168.18.62:8866/BundleMaster/version.txt
        /// </summary>
        public static string GetVersionFileUrl(AssetBackendType backend)
        {
            return $"{GetRemoteUrl(backend).TrimEnd('/')}/{VersionFileName}";
        }

        /// <summary>
        /// version.txt 磁盘路径。
        /// 例：C:\IIS_ServerData\BundleMaster\version.txt
        /// </summary>
        public static string GetVersionFileDiskPath(AssetBackendType backend)
        {
            return Path.Combine(GetIisRoot(backend), VersionFileName);
        }

        /// <summary>
        /// 某版本热更资源磁盘目录：{IisRoot}/{ver}/AssetBundles
        /// 其下再放 AllBundle、DllBundle 等分包。
        /// </summary>
        public static string GetVersionAssetBundlesDiskPath(AssetBackendType backend, string version)
        {
            return Path.Combine(GetIisRoot(backend), version, AssetBundlesFolder);
        }

        /// <summary>
        /// 运行时 BundleServerUrl：{RemoteUrl}/{ver}/AssetBundles
        /// 与 GameProduceUpdateAssetBundle 拼接规则保持一致。
        /// </summary>
        public static string GetBundleServerUrl(AssetBackendType backend, int version)
        {
            return $"{GetRemoteUrl(backend).TrimEnd('/')}/{version}/{AssetBundlesFolder}";
        }
    }
}
