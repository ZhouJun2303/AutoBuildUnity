using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class ProjectBuildWindow
{
    void DrawPlayerTab()
    {
        EditorGUILayout.HelpBox(
            "导出工程使用现有 BuildProject / OnPostprocess 逻辑。\n" +
            "路径配置仍可在旧「打包/打包面板」中设置 Android/iOS 输出目录。",
            MessageType.Info);

        var steps = new List<BuildStepId>
        {
            BuildStepId.Player_ExportAndroid,
            BuildStepId.Player_CopyAndroidRes,
            BuildStepId.Player_ExportIos,
            BuildStepId.Player_CopyIosRes,
        };
        DrawStepList(steps, allowToggle: true);

        EditorGUILayout.Space(4);
        EditorGUI.BeginDisabledGroup(_runner.IsRunning);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("导出 Android", GUILayout.Height(30)))
            RunPipeline(new List<BuildStepId> { BuildStepId.Player_ExportAndroid });
        if (GUILayout.Button("导出 iOS", GUILayout.Height(30)))
            RunPipeline(new List<BuildStepId> { BuildStepId.Player_ExportIos });
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("打开旧打包面板（路径配置）", GUILayout.Height(26)))
            BuildProjectWindows.Init();
        EditorGUI.EndDisabledGroup();
    }
}
