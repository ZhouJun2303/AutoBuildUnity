using System.Collections.Generic;
using Game.AssetCore;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 完整构建面板（学习向）。
/// 菜单：打包/构建面板
///
/// Tab：通用 | HybridCLR | BundleMaster | YooAsset | Addressables | 导出工程
/// 远端：http://192.168.18.62:8866/{Backend}/  ⇔  磁盘 C:\IIS_ServerData\{Backend}\
/// </summary>
public partial class ProjectBuildWindow : EditorWindow
{
    enum Tab
    {
        Common = 0,
        HybridClr = 1,
        BundleMaster = 2,
        YooAsset = 3,
        Addressables = 4,
        Player = 5,
    }

    static readonly string[] TabNames =
    {
        "通用", "HybridCLR", "BundleMaster", "YooAsset", "Addressables", "导出工程"
    };

    Tab _tab;
    Vector2 _logScroll;
    Vector2 _stepScroll;
    readonly BuildStepRunner _runner = new BuildStepRunner();
    readonly HashSet<BuildStepId> _customSelected = new HashSet<BuildStepId>();

    int _bmPreset;   // 0 完整热更 1 热更DLL+AB分发 2 自定义
    int _yooPreset;
    int _aaPreset;

    [MenuItem("打包/构建面板", false, 0)]
    public static void Open()
    {
        var win = GetWindow<ProjectBuildWindow>("构建面板");
        win.minSize = new Vector2(520, 640);
        win.Show();
    }

    void OnEnable()
    {
        _runner.Subscribe(Repaint);
    }

    void OnGUI()
    {
        _tab = (Tab)GUILayout.Toolbar((int)_tab, TabNames);
        EditorGUILayout.Space(4);

        switch (_tab)
        {
            case Tab.Common: DrawCommonTab(); break;
            case Tab.HybridClr: DrawHybridClrTab(); break;
            case Tab.BundleMaster: DrawBackendTab(AssetBackendType.BundleMaster, ref _bmPreset); break;
            case Tab.YooAsset: DrawBackendTab(AssetBackendType.YooAsset, ref _yooPreset); break;
            case Tab.Addressables: DrawBackendTab(AssetBackendType.Addressables, ref _aaPreset); break;
            case Tab.Player: DrawPlayerTab(); break;
        }

        EditorGUILayout.Space(8);
        DrawGlobalButtons();
        DrawLogArea();
    }

    void DrawGlobalButtons()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(_runner.IsRunning);
        if (GUILayout.Button("重置日志", GUILayout.Height(28)))
            _runner.ClearLogs();
        EditorGUI.EndDisabledGroup();

        if (_runner.IsRunning)
        {
            if (GUILayout.Button("停止", GUILayout.Height(28)))
                _runner.Stop();
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawLogArea()
    {
        EditorGUILayout.LabelField("构建日志", EditorStyles.boldLabel);
        _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.MinHeight(160));
        foreach (var line in _runner.Logs)
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
    }

    void DrawStepList(IList<BuildStepId> stepIds, bool allowToggle)
    {
        _stepScroll = EditorGUILayout.BeginScrollView(_stepScroll, GUILayout.MinHeight(220));
        foreach (var id in stepIds)
        {
            var info = BuildStepCatalog.Get(id);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            if (allowToggle)
            {
                bool on = _customSelected.Contains(id);
                bool n = EditorGUILayout.ToggleLeft($"{(int)id}. {info.Title}", on, GUILayout.ExpandWidth(true));
                if (n && !on) _customSelected.Add(id);
                if (!n && on) _customSelected.Remove(id);
            }
            else
            {
                EditorGUILayout.LabelField($"{(int)id}. {info.Title}", EditorStyles.boldLabel);
            }

            EditorGUI.BeginDisabledGroup(_runner.IsRunning);
            if (GUILayout.Button("执行", GUILayout.Width(52)))
                _runner.RunSingle(info);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(info.Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    void RunPipeline(IList<BuildStepId> ids)
    {
        _runner.ClearLogs();
        _runner.RunSteps(BuildStepCatalog.GetMany(ids));
    }

    void DrawPathHelpBox(AssetBackendType backend)
    {
        string remote = BuildParams.GetRemoteUrl(backend);
        string iis = BuildParams.GetIisRoot(backend);
        EditorGUILayout.HelpBox(
            $"请求根: {remote}/\n" +
            $"version: {remote}/version.txt\n" +
            $"资源(BM/Yoo): {remote}/{{ver}}/AssetBundles/\n" +
            $"磁盘根: {iis}\n" +
            $"磁盘 version: {BuildParams.GetIisVersionFile(backend)}",
            MessageType.Info);
    }
}
