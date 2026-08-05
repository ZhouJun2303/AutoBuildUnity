using System.Collections.Generic;
using Game.AssetCore;

/// <summary>
/// 注册全部构建步骤 + 各后端预设流水线。
/// 学习建议：先读 HybridCLR 步骤，再读对应 AB 的 Build → StreamingAssets → IIS。
/// </summary>
public static class BuildStepCatalog
{
    static Dictionary<BuildStepId, BuildStepInfo> _map;

    public static BuildStepInfo Get(BuildStepId id)
    {
        EnsureInit();
        return _map[id];
    }

    public static List<BuildStepInfo> GetMany(IEnumerable<BuildStepId> ids)
    {
        var list = new List<BuildStepInfo>();
        foreach (var id in ids)
            list.Add(Get(id));
        return list;
    }

    public static IEnumerable<BuildStepInfo> All => EnsureInit().Values;

    public static List<BuildStepId> HybridClrPrepare = new List<BuildStepId>
    {
        BuildStepId.HybridClr_ClearHotDll,
        BuildStepId.HybridClr_GenerateAll,
        BuildStepId.HybridClr_CompileDll,
        BuildStepId.HybridClr_StripAotMetadata,
        BuildStepId.HybridClr_CopyHotDllToAssets,
        BuildStepId.HybridClr_CopyAotMetadataToAssets,
    };

    /// <summary>BM 完整热更包：HybridCLR + 打 AB + StreamingAssets + IIS。</summary>
    public static List<BuildStepId> BundleMasterFullHotUpdate = new List<BuildStepId>
    {
        BuildStepId.HybridClr_ClearHotDll,
        BuildStepId.HybridClr_GenerateAll,
        BuildStepId.HybridClr_CompileDll,
        BuildStepId.HybridClr_StripAotMetadata,
        BuildStepId.HybridClr_CopyHotDllToAssets,
        BuildStepId.HybridClr_CopyAotMetadataToAssets,
        BuildStepId.BM_CheckBuildMode,
        BuildStepId.BM_ClearStreamingAssets,
        BuildStepId.BM_BuildAllBundles,
        BuildStepId.BM_BackupToResLocalRecord,
        BuildStepId.BM_CopyToStreamingAssets,
        BuildStepId.BM_CopyToLocalServer,
    };

    public static List<BuildStepId> BundleMasterOnlyAbAndDistribute = new List<BuildStepId>
    {
        BuildStepId.BM_CheckBuildMode,
        BuildStepId.BM_BuildAllBundles,
        BuildStepId.BM_CopyToStreamingAssets,
        BuildStepId.BM_CopyToLocalServer,
    };

    public static List<BuildStepId> YooAssetFullHotUpdate = new List<BuildStepId>
    {
        BuildStepId.HybridClr_ClearHotDll,
        BuildStepId.HybridClr_GenerateAll,
        BuildStepId.HybridClr_CompileDll,
        BuildStepId.HybridClr_StripAotMetadata,
        BuildStepId.HybridClr_CopyHotDllToAssets,
        BuildStepId.HybridClr_CopyAotMetadataToAssets,
        // Build 自带 ClearAndCopyAll，勿在构建后再清 StreamingAssets/yoo
        BuildStepId.Yoo_BuildAllBundle,
        BuildStepId.Yoo_BuildDllBundle,
        BuildStepId.Yoo_CopyToStreamingAssets,
        BuildStepId.Yoo_CopyToLocalServer,
    };

    public static List<BuildStepId> YooAssetOnlyAbAndDistribute = new List<BuildStepId>
    {
        BuildStepId.Yoo_BuildAllBundle,
        BuildStepId.Yoo_BuildDllBundle,
        BuildStepId.Yoo_CopyToStreamingAssets,
        BuildStepId.Yoo_CopyToLocalServer,
    };

    public static List<BuildStepId> AddressablesFull = new List<BuildStepId>
    {
        BuildStepId.HybridClr_ClearHotDll,
        BuildStepId.HybridClr_GenerateAll,
        BuildStepId.HybridClr_CompileDll,
        BuildStepId.HybridClr_StripAotMetadata,
        BuildStepId.HybridClr_CopyHotDllToAssets,
        BuildStepId.HybridClr_CopyAotMetadataToAssets,
        BuildStepId.AA_ClearCache,
        BuildStepId.AA_BuildPlayerContent,
        BuildStepId.AA_CopyToStreamingAssets,
        BuildStepId.AA_CopyToLocalServer,
    };

    public static List<BuildStepId> StepsForBackendTab(AssetBackendType backend)
    {
        switch (backend)
        {
            case AssetBackendType.BundleMaster:
                return new List<BuildStepId>
                {
                    BuildStepId.BM_CheckBuildMode,
                    BuildStepId.BM_ClearBuildCache,
                    BuildStepId.BM_ClearStreamingAssets,
                    BuildStepId.BM_BuildAllBundles,
                    BuildStepId.BM_BackupToResLocalRecord,
                    BuildStepId.BM_CopyToStreamingAssets,
                    BuildStepId.BM_CopyToLocalServer,
                };
            case AssetBackendType.YooAsset:
                return new List<BuildStepId>
                {
                    // 清理仅作可选前置；构建后切勿再跑 Clear
                    BuildStepId.Yoo_ClearStreamingAssets,
                    BuildStepId.Yoo_BuildAllBundle,
                    BuildStepId.Yoo_BuildDllBundle,
                    BuildStepId.Yoo_CopyToStreamingAssets,
                    BuildStepId.Yoo_CopyToLocalServer,
                };
            case AssetBackendType.Addressables:
                return new List<BuildStepId>
                {
                    BuildStepId.AA_ClearCache,
                    BuildStepId.AA_BuildPlayerContent,
                    BuildStepId.AA_CopyToStreamingAssets,
                    BuildStepId.AA_CopyToLocalServer,
                };
            default:
                return new List<BuildStepId>();
        }
    }

    static Dictionary<BuildStepId, BuildStepInfo> EnsureInit()
    {
        if (_map != null) return _map;
        _map = new Dictionary<BuildStepId, BuildStepInfo>();

        Add(BuildStepId.HybridClr_ClearHotDll, "清理 HotDll 目录",
            "清空 Assets/HotDll/HotUpdateDlls 与 AOTAssemblyMetadataDlls", HybridClrSteps.ClearHotDllFolders);
        Add(BuildStepId.HybridClr_GenerateAll, "HybridCLR GenerateAll",
            "生成 link.xml / AOT 泛型引用 / wrapper 等桥接文件", HybridClrSteps.GenerateAll);
        Add(BuildStepId.HybridClr_CompileDll, "编译热更 DLL",
            "CompileDll(activeBuildTarget) → HybridCLRData/HotUpdateDlls", HybridClrSteps.CompileHotUpdateDll);
        Add(BuildStepId.HybridClr_StripAotMetadata, "AOT 元数据裁剪",
            "Strip 补充元数据：源优先 AssembliesPostIl2CppStrip，缺失则从 Library/Bee/.../ManagedStripped 同步；输出 Strip_*.dll",
            HybridClrSteps.StripAotMetadata);
        Add(BuildStepId.HybridClr_CopyHotDllToAssets, "拷贝热更 DLL → Assets",
            "Game.dll → Assets/HotDll/HotUpdateDlls/Game.dll.bytes", HybridClrSteps.CopyHotDllToAssets);
        Add(BuildStepId.HybridClr_CopyAotMetadataToAssets, "拷贝 AOT 元数据 → Assets",
            "Strip_*.dll → Assets/HotDll/AOTAssemblyMetadataDlls/*.bytes", HybridClrSteps.CopyAotMetadataToAssets);

        Add(BuildStepId.Common_WriteVersionFiles, "写 version.txt",
            "StreamingAssets + 当前面板关注的 IIS 后端 version", BundleMasterSteps.WriteVersionFilesOnly);

        Add(BuildStepId.BM_CheckBuildMode, "检查 BM Build 模式",
            "BundleMasterRuntimeConfig.AssetLoadMode 必须为 Build", BundleMasterSteps.CheckBuildMode, AssetBackendType.BundleMaster);
        Add(BuildStepId.BM_ClearBuildCache, "清理 BMBuild 缓存",
            "清空项目根 BMBuild/", BundleMasterSteps.ClearBuildCache, AssetBackendType.BundleMaster);
        Add(BuildStepId.BM_ClearStreamingAssets, "清理 StreamingAssets(BM)",
            "仅清理 AllBundle/DllBundle，不动 yoo/aa", BundleMasterSteps.ClearStreamingAssetsBundles, AssetBackendType.BundleMaster);
        Add(BuildStepId.BM_BuildAllBundles, "构建 BM 全部 AB",
            "BM.BuildAssets.BuildAllBundle()", BundleMasterSteps.BuildAllBundles, AssetBackendType.BundleMaster);
        Add(BuildStepId.BM_BackupToResLocalRecord, "备份到 ResLocalRecord",
            "ResLocalRecord/{ver}/AssetBundles", BundleMasterSteps.BackupToResLocalRecord, AssetBackendType.BundleMaster);
        Add(BuildStepId.BM_CopyToStreamingAssets, "BM → StreamingAssets",
            "BMBuild 分包拷入 StreamingAssets，并写 version.txt", BundleMasterSteps.CopyToStreamingAssets, AssetBackendType.BundleMaster);
        Add(BuildStepId.BM_CopyToLocalServer, "BM → IIS(BundleMaster)",
            "C:\\IIS_ServerData\\BundleMaster\\{ver}\\AssetBundles 与 version.txt；URL: .../8866/BundleMaster/",
            BundleMasterSteps.CopyToLocalServer, AssetBackendType.BundleMaster);

        Add(BuildStepId.Yoo_ClearStreamingAssets, "清理 StreamingAssets/yoo",
            "仅作构建前可选清理；构建后勿执行（会删掉刚写入的首包）",
            YooAssetSteps.ClearStreamingAssetsYoo, AssetBackendType.YooAsset);
        Add(BuildStepId.Yoo_BuildAllBundle, "Yoo 构建 AllBundle",
            "LegacyBuildPipeline + BundledCopyOption=ClearAndCopyAll（打 AB、拷 StreamingAssets、写 BuiltinCatalog）",
            YooAssetSteps.BuildAllBundlePackage, AssetBackendType.YooAsset);
        Add(BuildStepId.Yoo_BuildDllBundle, "Yoo 构建 DllBundle",
            "同上；需先 HybridCLR 拷贝 .bytes 进 Assets/HotDll", YooAssetSteps.BuildDllBundlePackage, AssetBackendType.YooAsset);
        Add(BuildStepId.Yoo_CopyToStreamingAssets, "Yoo 校验 StreamingAssets 首包",
            "确认构建已写入 yoo/{Package} 且存在 BuiltinCatalog.bytes（不再手工拷贝/二次生成）",
            YooAssetSteps.CopyToStreamingAssets, AssetBackendType.YooAsset);
        Add(BuildStepId.Yoo_CopyToLocalServer, "Yoo → IIS(YooAsset)",
            "C:\\IIS_ServerData\\YooAsset\\{ver}\\AssetBundles；URL: .../8866/YooAsset/",
            YooAssetSteps.CopyToLocalServer, AssetBackendType.YooAsset);

        Add(BuildStepId.AA_ClearCache, "清理 AA 构建缓存",
            "CleanPlayerContent", AddressablesSteps.ClearCache, AssetBackendType.Addressables);
        Add(BuildStepId.AA_BuildPlayerContent, "AA BuildPlayerContent",
            "AddressableAssetSettings.BuildPlayerContent", AddressablesSteps.BuildPlayerContent, AssetBackendType.Addressables);
        Add(BuildStepId.AA_CopyToStreamingAssets, "AA → StreamingAssets",
            "拷贝到 StreamingAssets/aa（学习用）", AddressablesSteps.CopyToStreamingAssets, AssetBackendType.Addressables);
        Add(BuildStepId.AA_CopyToLocalServer, "AA → IIS(Addressables)",
            "C:\\IIS_ServerData\\Addressables\\{ver}；URL: .../8866/Addressables/",
            AddressablesSteps.CopyToLocalServer, AssetBackendType.Addressables);

        Add(BuildStepId.Player_ExportAndroid, "导出 Android 工程",
            "Gradle 工程到 TempBuild", PlayerExportSteps.ExportAndroid);
        Add(BuildStepId.Player_ExportIos, "导出 iOS 工程",
            "Xcode 工程到 TempBuild", PlayerExportSteps.ExportIos);
        Add(BuildStepId.Player_CopyAndroidRes, "拷贝 Android 资源到导出工程",
            "OnPostprocessBuild_Android.CopyUnityRes", PlayerExportSteps.CopyAndroidRes);
        Add(BuildStepId.Player_CopyIosRes, "拷贝 iOS 资源到导出工程",
            "OnPostprocessBuild_IOS.CopyUnityRes", PlayerExportSteps.CopyIosRes);

        return _map;
    }

    static void Add(BuildStepId id, string title, string desc, System.Action act, AssetBackendType? backend = null)
    {
        _map[id] = new BuildStepInfo(id, title, desc, act, backend);
    }
}
