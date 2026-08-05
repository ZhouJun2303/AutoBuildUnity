using Game.AssetCore;
using UnityEditor;
using UnityEngine;

public partial class ProjectBuildWindow
{
    void DrawCommonTab()
    {
        EditorGUILayout.LabelField("1. 版本与远端", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField($"远端 HTTP 总根: {BuildPaths.RemoteBaseUrl}/");
        EditorGUILayout.LabelField("各 AB 使用不同子目录，请求地址与 IIS 磁盘一一对应：");
        EditorGUILayout.LabelField("  BundleMaster → /BundleMaster/");
        EditorGUILayout.LabelField("  YooAsset     → /YooAsset/");
        EditorGUILayout.LabelField("  Addressables → /Addressables/");

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("资源版本", GUILayout.Width(70));
        string ver = EditorGUILayout.TextField(BuildParams.AssetVersion, GUILayout.Width(80));
        if (ver != BuildParams.AssetVersion && int.TryParse(ver, out _))
            BuildParams.AssetVersion = ver;
        if (GUILayout.Button("+1", GUILayout.Width(40)))
            BuildParams.BumpVersion();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("IIS 磁盘总根", GUILayout.Width(90));
        BuildParams.IisDiskRoot = EditorGUILayout.TextField(BuildParams.IisDiskRoot);
        if (GUILayout.Button("打开", GUILayout.Width(48)))
            BuildPaths.OpenInExplorer(BuildParams.IisDiskRoot);
        EditorGUILayout.EndHorizontal();

        BuildParams.CopyToResLocalServer = EditorGUILayout.ToggleLeft(
            "同时镜像到项目 ResLocalServer/{Backend}/", BuildParams.CopyToResLocalServer);

        var backendCfg = AssetBackendConfig.LoadOrDefault();
        EditorGUILayout.LabelField($"当前运行时 Backend 配置: {backendCfg.Backend}");
        EditorGUILayout.LabelField($"当前平台: {EditorUserBuildSettings.activeBuildTarget}");
        EditorGUILayout.LabelField($"对应请求: {BuildParams.GetRemoteUrl(backendCfg.Backend)}/");

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("2. 快捷打开目录", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("StreamingAssets"))
            BuildPaths.OpenInExplorer(BuildPaths.StreamingAssets);
        if (GUILayout.Button("BM IIS"))
            BuildPaths.OpenInExplorer(BuildParams.GetIisRoot(AssetBackendType.BundleMaster));
        if (GUILayout.Button("Yoo IIS"))
            BuildPaths.OpenInExplorer(BuildParams.GetIisRoot(AssetBackendType.YooAsset));
        if (GUILayout.Button("AA IIS"))
            BuildPaths.OpenInExplorer(BuildParams.GetIisRoot(AssetBackendType.Addressables));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("BMBuild"))
            BuildPaths.OpenInExplorer(BuildPaths.BmBuildRoot);
        if (GUILayout.Button("Bundles(Yoo)"))
            BuildPaths.OpenInExplorer(BuildPaths.YooBuildRoot);
        if (GUILayout.Button("HotDll"))
            BuildPaths.OpenInExplorer(BuildPaths.HotDllRoot);
        EditorGUILayout.EndHorizontal();
    }
}
