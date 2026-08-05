using System.IO;
using Game.AssetCore;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 构建相关路径集中定义（学习入口：先看这里）。
///
/// 远端约定：
///   http://192.168.18.62:8866/{BundleMaster|YooAsset|Addressables}/...
/// 磁盘约定：
///   C:\IIS_ServerData\{BundleMaster|YooAsset|Addressables}\...
///
/// 与运行时 <see cref="AssetBackendRemotePaths"/> 保持同源，避免「拷到 A 目录、请求 B 地址」。
/// </summary>
public static class BuildPaths
{
    public static string ProjectRoot =>
        Application.dataPath.Remove(Application.dataPath.Length - 7);

    public static string StreamingAssets => Application.streamingAssetsPath;

    public static string HotDllRoot => Path.Combine(Application.dataPath, "HotDll");
    public static string HotUpdateDlls => Path.Combine(HotDllRoot, "HotUpdateDlls");
    public static string AotMetadataDlls => Path.Combine(HotDllRoot, "AOTAssemblyMetadataDlls");

    /// <summary>BundleMaster 默认输出（见 AssetLoadTable.BundlePath）。</summary>
    public static string BmBuildRoot => Path.Combine(ProjectRoot, "BMBuild");

    /// <summary>YooAsset 默认输出根 Bundles/。</summary>
    public static string YooBuildRoot => Path.Combine(ProjectRoot, "Bundles");

    public static string ResLocalRecordRoot => Path.Combine(ProjectRoot, "ResLocalRecord");
    public static string ResLocalServerRoot => Path.Combine(ProjectRoot, "ResLocalServer");

    public static string RemoteBaseUrl => AssetBackendRemotePaths.RemoteBaseUrl;
    public static string IisDiskRoot => AssetBackendRemotePaths.IisDiskRoot;

    public static string GetIisRoot(AssetBackendType backend) =>
        AssetBackendRemotePaths.GetIisRoot(backend);

    public static string GetRemoteUrl(AssetBackendType backend) =>
        AssetBackendRemotePaths.GetRemoteUrl(backend);

    /// <summary>
    /// IIS 上某版本 AssetBundles 目录。
    /// 例 BM：C:\IIS_ServerData\BundleMaster\4\AssetBundles
    /// </summary>
    public static string GetIisVersionAssetBundles(AssetBackendType backend, string version) =>
        AssetBackendRemotePaths.GetVersionAssetBundlesDiskPath(backend, version);

    public static string GetIisVersionFile(AssetBackendType backend) =>
        AssetBackendRemotePaths.GetVersionFileDiskPath(backend);

    /// <summary>
    /// 项目内 ResLocalServer 镜像：ResLocalServer/{Backend}/{ver}/AssetBundles
    /// </summary>
    public static string GetResLocalServerAssetBundles(AssetBackendType backend, string version)
    {
        string folder = AssetBackendRemotePaths.GetBackendFolderName(backend);
        return Path.Combine(ResLocalServerRoot, folder, version, AssetBackendRemotePaths.AssetBundlesFolder);
    }

    public static string GetResLocalServerVersionFile(AssetBackendType backend)
    {
        string folder = AssetBackendRemotePaths.GetBackendFolderName(backend);
        return Path.Combine(ResLocalServerRoot, folder, AssetBackendRemotePaths.VersionFileName);
    }

    public static string GetResLocalRecordAssetBundles(string version) =>
        Path.Combine(ResLocalRecordRoot, version, AssetBackendRemotePaths.AssetBundlesFolder);

    public static string YooPackageOutput(BuildTarget target, string packageName, string version) =>
        Path.Combine(YooBuildRoot, target.ToString(), packageName, version);

    public static string YooStreamingPackage(string packageName) =>
        Path.Combine(StreamingAssets, "yoo", packageName);

    public static void OpenInExplorer(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        EditorUtility.RevealInFinder(path);
    }
}
