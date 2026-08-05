using System.Collections.Generic;
using Game.AssetCore;
using UnityEditor;
using UnityEngine;

public partial class ProjectBuildWindow
{
    static readonly string[] BackendPresets = { "完整热更包", "仅 AB+分发", "自定义勾选" };

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
                displaySteps = BuildStepCatalog.StepsForBackendTab(backend);
                runSteps = preset == 0 ? BuildStepCatalog.BundleMasterFullHotUpdate
                    : preset == 1 ? BuildStepCatalog.BundleMasterOnlyAbAndDistribute
                    : null;
                break;
            case AssetBackendType.YooAsset:
                displaySteps = BuildStepCatalog.StepsForBackendTab(backend);
                runSteps = preset == 0 ? BuildStepCatalog.YooAssetFullHotUpdate
                    : preset == 1 ? BuildStepCatalog.YooAssetOnlyAbAndDistribute
                    : null;
                break;
            case AssetBackendType.Addressables:
                displaySteps = BuildStepCatalog.StepsForBackendTab(backend);
                runSteps = preset == 0 ? BuildStepCatalog.AddressablesFull
                    : preset == 1 ? new List<BuildStepId>
                    {
                        BuildStepId.AA_ClearCache,
                        BuildStepId.AA_BuildPlayerContent,
                        BuildStepId.AA_CopyToStreamingAssets,
                        BuildStepId.AA_CopyToLocalServer,
                    }
                    : null;
                break;
            default:
                return;
        }

        // 完整热更包展示 HybridCLR + 后端步骤
        if (preset == 0 && runSteps != null)
            displaySteps = runSteps;

        bool custom = preset == 2;
        if (custom)
        {
            // 自定义：展示后端步骤 + 可选 HybridCLR
            var all = new List<BuildStepId>();
            all.AddRange(BuildStepCatalog.HybridClrPrepare);
            all.AddRange(BuildStepCatalog.StepsForBackendTab(backend));
            displaySteps = all;
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
