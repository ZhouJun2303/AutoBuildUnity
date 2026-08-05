using System.IO;
using Game.AssetCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// Addressables 构建与分发。
///
/// IIS：C:\IIS_ServerData\Addressables\{ver}\
/// 请求：http://192.168.18.62:8866/Addressables/...
/// version：http://192.168.18.62:8866/Addressables/version.txt
///
/// Profile 的 Remote Load Path 建议指向上述 URL；本步骤负责构建产物拷贝。
/// </summary>
public static class AddressablesSteps
{
    static readonly AssetBackendType Backend = AssetBackendType.Addressables;

    public static void ClearCache()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            throw new System.Exception("Addressables Settings 为空，请先初始化 Addressables。");
        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        Debug.Log("[AA] CleanPlayerContent 完成");
    }

    public static void BuildPlayerContent()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            throw new System.Exception("Addressables Settings 为空。");

        Debug.Log("[AA] 开始 BuildPlayerContent，版本=" + BuildParams.AssetVersion);
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        if (!string.IsNullOrEmpty(result.Error))
            throw new System.Exception("[AA] 构建失败: " + result.Error);
        Debug.Log("[AA] BuildPlayerContent 完成: " + result.OutputPath);
    }

    public static void CopyToStreamingAssets()
    {
        string buildPath = GetDefaultBuildOutput();
        if (!Directory.Exists(buildPath))
            throw new System.Exception($"[AA] 构建输出不存在: {buildPath}");

        // Addressables 内置目录通常由管线自动处理；此处额外拷一份便于学习查看
        string dest = Path.Combine(BuildPaths.StreamingAssets, "aa");
        FileOps.CopyDirectory(buildPath, dest, clearDest: true);
        FileOps.WriteVersionFile(Path.Combine(BuildPaths.StreamingAssets, "version.txt"), BuildParams.AssetVersion);
        Debug.Log($"[AA] 拷贝到 StreamingAssets/aa: {buildPath}");
        FileOps.RefreshAssets();
    }

    public static void CopyToLocalServer()
    {
        string version = BuildParams.AssetVersion;
        string buildPath = GetDefaultBuildOutput();
        if (!Directory.Exists(buildPath))
            throw new System.Exception($"[AA] 构建输出不存在: {buildPath}");

        string iisDest = Path.Combine(BuildParams.GetIisRoot(Backend), version);
        FileOps.CopyDirectory(buildPath, iisDest, clearDest: true);
        FileOps.WriteVersionFile(BuildParams.GetIisVersionFile(Backend), version);

        Debug.Log($"[AA] IIS: {iisDest}");
        Debug.Log($"[AA] 请求根: {BuildParams.GetRemoteUrl(Backend)}/{version}/");
        Debug.Log($"[AA] version URL: {BuildParams.GetRemoteUrl(Backend)}/version.txt");

        if (BuildParams.CopyToResLocalServer)
        {
            string folder = AssetBackendRemotePaths.GetBackendFolderName(Backend);
            string local = Path.Combine(BuildPaths.ResLocalServerRoot, folder, version);
            FileOps.CopyDirectory(buildPath, local, clearDest: true);
            FileOps.WriteVersionFile(BuildPaths.GetResLocalServerVersionFile(Backend), version);
        }
    }

    static string GetDefaultBuildOutput()
    {
        // Addressables 默认 Local.BuildPath 常见为 ServerData/[BuildTarget]
        string project = BuildPaths.ProjectRoot;
        string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        string serverData = Path.Combine(project, "ServerData", platform);
        if (Directory.Exists(serverData))
            return serverData;

        // 回退：Library/com.unity.addressables
        string lib = Path.Combine(project, "Library", "com.unity.addressables");
        return lib;
    }
}
