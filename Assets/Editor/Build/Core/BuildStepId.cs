/// <summary>
/// 细粒度构建步骤 ID。
/// 分组：HybridCLR / 分发公共 / BundleMaster / YooAsset / Addressables / Player。
/// 数值仅用于排序展示，可在中间插入新步骤。
/// </summary>
public enum BuildStepId
{
    // ---------- HybridCLR（打 DllBundle 前必须完成）----------
    HybridClr_ClearHotDll = 100,
    HybridClr_GenerateAll = 110,
    HybridClr_CompileDll = 120,
    HybridClr_StripAotMetadata = 130,
    HybridClr_CopyHotDllToAssets = 140,
    HybridClr_CopyAotMetadataToAssets = 150,

    // ---------- 分发公共 ----------
    Common_WriteVersionFiles = 200,

    // ---------- BundleMaster ----------
    BM_CheckBuildMode = 300,
    BM_ClearBuildCache = 310,
    BM_ClearStreamingAssets = 320,
    BM_BuildAllBundles = 330,
    BM_BackupToResLocalRecord = 340,
    BM_CopyToStreamingAssets = 350,
    BM_CopyToLocalServer = 360,

    // ---------- YooAsset ----------
    Yoo_BuildAllBundle = 400,
    Yoo_BuildDllBundle = 410,
    Yoo_ClearStreamingAssets = 420,
    Yoo_CopyToStreamingAssets = 430,
    Yoo_CopyToLocalServer = 440,

    // ---------- Addressables ----------
    AA_ClearCache = 500,
    AA_BuildPlayerContent = 510,
    AA_CopyToStreamingAssets = 520,
    AA_CopyToLocalServer = 530,

    // ---------- 导出工程 ----------
    Player_ExportAndroid = 600,
    Player_ExportIos = 610,
    Player_CopyAndroidRes = 620,
    Player_CopyIosRes = 630,
}
