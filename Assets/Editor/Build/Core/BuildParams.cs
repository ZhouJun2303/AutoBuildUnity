using Game.AssetCore;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 构建面板持久化参数（EditorPrefs）。
/// 版本号优先读写 Resources/ResConfig，保证打进包内的版本一致。
/// </summary>
public static class BuildParams
{
    const string PrefIisRoot = "AutoBuild.IisDiskRoot";
    const string PrefCopyResLocalServer = "AutoBuild.CopyResLocalServer";
    const string PrefAssetVersionFallback = "AutoBuild.AssetVersion";

    /// <summary>
    /// IIS 总根，默认 C:\IIS_ServerData。
    /// 实际拷贝还会再拼 BundleMaster / YooAsset / Addressables 子目录。
    /// </summary>
    public static string IisDiskRoot
    {
        get => EditorPrefs.GetString(PrefIisRoot, AssetBackendRemotePaths.IisDiskRoot);
        set => EditorPrefs.SetString(PrefIisRoot, value);
    }

    /// <summary>是否同时镜像到项目内 ResLocalServer/{Backend}/。</summary>
    public static bool CopyToResLocalServer
    {
        get => EditorPrefs.GetBool(PrefCopyResLocalServer, true);
        set => EditorPrefs.SetBool(PrefCopyResLocalServer, value);
    }

    /// <summary>
    /// 资源版本（字符串形式的正整数）。
    /// 读写 ResConfig.ResVersion；找不到配置时回退 EditorPrefs。
    /// </summary>
    public static string AssetVersion
    {
        get
        {
            var cfg = ResConfig.Instance;
            if (cfg != null)
                return cfg.ResVersion.ToString();
            return EditorPrefs.GetString(PrefAssetVersionFallback, "1");
        }
        set
        {
            if (!int.TryParse(value, out int ver) || ver < 0)
                return;

            var cfg = LoadResConfigAsset();
            if (cfg != null)
            {
                cfg.ResVersion = ver;
                EditorUtility.SetDirty(cfg);
                AssetDatabase.SaveAssets();
            }
            EditorPrefs.SetString(PrefAssetVersionFallback, ver.ToString());
        }
    }

    public static int AssetVersionInt
    {
        get => int.TryParse(AssetVersion, out int v) ? v : 0;
        set => AssetVersion = Mathf.Max(0, value).ToString();
    }

    public static void BumpVersion()
    {
        AssetVersionInt = AssetVersionInt + 1;
    }

    /// <summary>
    /// 当前后端对应的远端 URL（只读展示用）。
    /// 注意：IisDiskRoot 若被改成非默认值，磁盘路径用 IisDiskRoot+子目录，URL 仍基于 8866 约定。
    /// </summary>
    public static string GetRemoteUrl(AssetBackendType backend) =>
        AssetBackendRemotePaths.GetRemoteUrl(backend);

    /// <summary>
    /// 可配置 IIS 总根下的后端磁盘根。
    /// 若 IisDiskRoot 仍是默认，则与 AssetBackendRemotePaths 一致。
    /// </summary>
    public static string GetIisRoot(AssetBackendType backend)
    {
        string folder = AssetBackendRemotePaths.GetBackendFolderName(backend);
        if (string.IsNullOrEmpty(folder))
            return IisDiskRoot;
        return PathCombine(IisDiskRoot, folder);
    }

    public static string GetIisVersionAssetBundles(AssetBackendType backend, string version) =>
        PathCombine(GetIisRoot(backend), version, AssetBackendRemotePaths.AssetBundlesFolder);

    public static string GetIisVersionFile(AssetBackendType backend) =>
        PathCombine(GetIisRoot(backend), AssetBackendRemotePaths.VersionFileName);

    static ResConfig LoadResConfigAsset()
    {
        return AssetDatabase.LoadAssetAtPath<ResConfig>(ResConfig.ResConfigPath);
    }

    static string PathCombine(params string[] parts)
    {
        string p = parts[0];
        for (int i = 1; i < parts.Length; i++)
            p = System.IO.Path.Combine(p, parts[i]);
        return p;
    }
}
