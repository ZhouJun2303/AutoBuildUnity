using UnityEditor;
using UnityEngine;

public partial class ProjectBuildWindow
{
    void DrawHybridClrTab()
    {
        EditorGUILayout.HelpBox(
            "HybridCLR 步骤在打 DllBundle 之前执行。\n" +
            "推荐顺序：清理 → GenerateAll → 编译 → 裁剪 AOT → 拷贝 DLL/元数据到 Assets/HotDll。\n" +
            "AOT 裁剪依赖曾成功 BuildPlayer 生成的 AssembliesPostIl2CppStrip。",
            MessageType.Info);

        DrawStepList(BuildStepCatalog.HybridClrPrepare, allowToggle: true);

        EditorGUI.BeginDisabledGroup(_runner.IsRunning);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("一键 HybridCLR 全流程", GUILayout.Height(32)))
            RunPipeline(BuildStepCatalog.HybridClrPrepare);
        if (GUILayout.Button("仅执行勾选步骤", GUILayout.Height(32)))
        {
            var selected = new System.Collections.Generic.List<BuildStepId>();
            foreach (var id in BuildStepCatalog.HybridClrPrepare)
                if (_customSelected.Contains(id))
                    selected.Add(id);
            RunPipeline(selected);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }
}
