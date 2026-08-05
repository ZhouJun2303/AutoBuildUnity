using System.IO;
using BM;
using Game.AssetCore;
using UnityEditor;
using UnityEngine;

/// <summary>
/// BundleMaster 构建与分发步骤。
///
/// 输出：
///   构建 → 项目 BMBuild/{AllBundle,DllBundle}/
///   内置 → Assets/StreamingAssets/{AllBundle,DllBundle}/
///   IIS  → C:\IIS_ServerData\BundleMaster\{ver}\AssetBundles\{AllBundle,DllBundle}/
///   请求 → http://192.168.18.62:8866/BundleMaster/{ver}/AssetBundles/...
///   version → C:\IIS_ServerData\BundleMaster\version.txt
///            http://192.168.18.62:8866/BundleMaster/version.txt
/// </summary>
public static class BundleMasterSteps
{
    static readonly AssetBackendType Backend = AssetBackendType.BundleMaster;

    public static void CheckBuildMode()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<BundleMasterRuntimeConfig>(BundleMasterWindow.RuntimeConfigPath);
        if (cfg == null)
            throw new System.Exception($"缺少 BM 运行时配置: {BundleMasterWindow.RuntimeConfigPath}");
        if (cfg.AssetLoadMode != AssetLoadMode.Build)
            throw new System.Exception("打包需要 AssetLoadMode = Build，请在 BM 配置中修改。");
        Debug.Log("[BM] AssetLoadMode=Build 检查通过");
    }

    public static void ClearBuildCache()
    {
        if (Directory.Exists(BuildPaths.BmBuildRoot))
        {
            FileOps.CreateOrClearDirectory(BuildPaths.BmBuildRoot);
            Debug.Log("[BM] 已清理 BMBuild");
        }
    }

    public static void ClearStreamingAssetsBundles()
    {
        // 只清 BM 分包目录，不动 yoo/aa 等其它后端产物
        string all = Path.Combine(BuildPaths.StreamingAssets, "AllBundle");
        string dll = Path.Combine(BuildPaths.StreamingAssets, "DllBundle");
        if (Directory.Exists(all)) FileOps.CreateOrClearDirectory(all);
        if (Directory.Exists(dll)) FileOps.CreateOrClearDirectory(dll);
        string ver = Path.Combine(BuildPaths.StreamingAssets, "version.txt");
        if (File.Exists(ver)) File.Delete(ver);
        Debug.Log("[BM] 已清理 StreamingAssets 下 AllBundle/DllBundle");
        FileOps.RefreshAssets();
    }

    public static void BuildAllBundles()
    {
        CheckBuildMode();
        BuildAssets.BuildAllBundle();
        Debug.Log("[BM] BuildAllBundle 完成");
    }

    public static void BackupToResLocalRecord()
    {
        string ver = BuildParams.AssetVersion;
        var table = LoadTable();
        string dest = BuildPaths.GetResLocalRecordAssetBundles(ver);
        FileOps.CopyDirectory(table.BuildBundlePath, dest, clearDest: true);
        Debug.Log($"[BM] 备份到 ResLocalRecord: {dest}");
    }

    public static void CopyToStreamingAssets()
    {
        var table = LoadTable();
        // BM 构建输出根下直接是各分包文件夹，拷到 StreamingAssets 根
        FileOps.CopyDirectory(table.BuildBundlePath, BuildPaths.StreamingAssets, clearDest: false);
        FileOps.WriteVersionFile(Path.Combine(BuildPaths.StreamingAssets, "version.txt"), BuildParams.AssetVersion);
        Debug.Log("[BM] 已拷贝到 StreamingAssets");
        FileOps.RefreshAssets();
    }

    /// <summary>
    /// 拷贝到 IIS：C:\IIS_ServerData\BundleMaster\{ver}\AssetBundles\
    /// 对应请求：http://192.168.18.62:8866/BundleMaster/{ver}/AssetBundles/
    /// </summary>
    public static void CopyToLocalServer()
    {
        string ver = BuildParams.AssetVersion;
        var table = LoadTable();
        string iisAssetBundles = BuildParams.GetIisVersionAssetBundles(Backend, ver);
        FileOps.CopyDirectory(table.BuildBundlePath, iisAssetBundles, clearDest: true);
        FileOps.WriteVersionFile(BuildParams.GetIisVersionFile(Backend), ver);

        Debug.Log($"[BM] 已拷贝到 IIS: {iisAssetBundles}");
        Debug.Log($"[BM] 请求根: {BuildParams.GetRemoteUrl(Backend)}/{ver}/AssetBundles");
        Debug.Log($"[BM] version URL: {BuildParams.GetRemoteUrl(Backend)}/version.txt");

        if (BuildParams.CopyToResLocalServer)
        {
            string local = BuildPaths.GetResLocalServerAssetBundles(Backend, ver);
            FileOps.CopyDirectory(table.BuildBundlePath, local, clearDest: true);
            FileOps.WriteVersionFile(BuildPaths.GetResLocalServerVersionFile(Backend), ver);
            Debug.Log($"[BM] 已镜像 ResLocalServer: {local}");
        }
    }

    public static void WriteVersionFilesOnly()
    {
        string ver = BuildParams.AssetVersion;
        FileOps.WriteVersionFile(Path.Combine(BuildPaths.StreamingAssets, "version.txt"), ver);
        FileOps.WriteVersionFile(BuildParams.GetIisVersionFile(Backend), ver);
        if (BuildParams.CopyToResLocalServer)
            FileOps.WriteVersionFile(BuildPaths.GetResLocalServerVersionFile(Backend), ver);
    }

    static AssetLoadTable LoadTable()
    {
        var table = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterWindow.AssetLoadTablePath);
        if (table == null)
            throw new System.Exception($"缺少 AssetLoadTable: {BundleMasterWindow.AssetLoadTablePath}");
        return table;
    }
}
