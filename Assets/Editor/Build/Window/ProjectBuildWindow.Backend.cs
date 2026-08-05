using System.Collections.Generic;
using Game.AssetCore;
using UnityEditor;
using UnityEngine;

public partial class ProjectBuildWindow
{
    // 0 完整 HybridCLR+AB  1 日常：编译热更DLL+AB+分发  2 自定义勾选
    static readonly string[] BackendPresets = { "完整热更包", "热更DLL+AB分发", "自定义勾选" };

    void DrawBackendTab(AssetBackendType backend, ref int preset)
    {
        DrawPathHelpBox(backend);

        preset = GUILayout.Toolbar(preset, BackendPresets);
        EditorGUILayout.Space(4);

        List<BuildStepId> displaySteps;
        List<BuildStepId> runSteps;

        switch (backend)
        {
            case AssetBackendType.BundleMaster:
                runSteps = preset == 0 ? BuildStepCatalog.BundleMasterFullHotUpdate
                    : preset == 1 ? BuildStepCatalog.BundleMasterOnlyAbAndDistribute
                    : null;
                break;
            case AssetBackendType.YooAsset:
                runSteps = preset == 0 ? BuildStepCatalog.YooAssetFullHotUpdate
                    : preset == 1 ? BuildStepCatalog.YooAssetOnlyAbAndDistribute
                    : null;
                break;
            case AssetBackendType.Addressables:
                runSteps = preset == 0 ? BuildStepCatalog.AddressablesFull
                    : preset == 1 ? BuildStepCatalog.AddressablesOnlyAbAndDistribute
                    : null;
                break;
            default:
                return;
        }

        bool custom = preset == 2;
        if (custom)
        {
            // 自定义：HybridCLR（含编译热更 DLL）+ 后端步骤，可勾选
            var all = new List<BuildStepId>();
            all.AddRange(BuildStepCatalog.HybridClrPrepare);
            all.AddRange(BuildStepCatalog.StepsForBackendTab(backend));
            displaySteps = all;
        }
        else
        {
            // 预设流水线：展示与执行一致（完整包 / 仅AB 均含编译热更 DLL）
            displaySteps = runSteps;
        }

        DrawStepList(displaySteps, allowToggle: custom);

        EditorGUI.BeginDisabledGroup(_runner.IsRunning);
        if (GUILayout.Button(custom ? "执行勾选步骤" : "开始本流水线", GUILayout.Height(34)))
        {
            if (custom)
            {
                var sel = new List<BuildStepId>();
                foreach (var id in displaySteps)
                    if (_customSelected.Contains(id))
                        sel.Add(id);
                RunPipeline(sel);
            }
            else if (runSteps != null)
            {
                RunPipeline(runSteps);
            }
        }
        EditorGUI.EndDisabledGroup();
    }
}
