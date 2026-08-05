using System;
using System.IO;
using Game.AssetCore;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// YooAsset 3.0.5 构建与分发（按官方构建参数走，不二次造 Catalog）。
///
/// 关键参数：
///   BundledCopyOption = ClearAndCopyAll
///   → 构建管线 TaskCopyBundledFiles 把首包拷到 StreamingAssets/yoo/{Package}
///   → TaskCreateCatalog 自动生成 BuiltinCatalog.bytes（Offline/Host 初始化必需）
///
/// 输出约定：
///   构建输出：Bundles/{Platform}/{Package}/{version}/
///   首包内置：StreamingAssets/yoo/{Package}/   （含 BuiltinCatalog.bytes）
///   IIS 热更：C:\IIS_ServerData\YooAsset\{ver}\AssetBundles\{Package}/
///   请求：http://192.168.18.62:8866/YooAsset/{ver}/AssetBundles/...
/// </summary>
public static class YooAssetSteps
{
    static readonly AssetBackendType Backend = AssetBackendType.YooAsset;

    public const string PackageAllBundle = "AllBundle";
    public const string PackageDllBundle = "DllBundle";

    public static void BuildAllBundlePackage() => BuildPackage(PackageAllBundle);

    public static void BuildDllBundlePackage() => BuildPackage(PackageDllBundle);

    /// <summary>
    /// 构建单个 Package。
    /// 通过 BundledCopyOption 让官方管线同时完成：打 AB + 拷 StreamingAssets + 写 Catalog。
    /// </summary>
    public static void BuildPackage(string packageName)
    {
        string version = BuildParams.AssetVersion;
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string buildOutputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot();
        Debug.Log($"[Yoo] 开始构建 Package={packageName}, Version={version}, Target={target}");

        // Yoo TaskPrepare：Bundles/{Target}/{Package}/{Version} 已存在会直接 ErrorCode115。
        // 同版本重建必须先删该目录——必须提示用户确认，禁止静默删除。
        string packageOutputDirectory = Path.Combine(buildOutputRoot, target.ToString(), packageName, version);
        if (Directory.Exists(packageOutputDirectory))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "YooAsset 版本输出已存在",
                $"以下目录已存在，同版本无法直接覆盖构建：\n\n{packageOutputDirectory}\n\n" +
                $"Package: {packageName}\nVersion: {version}\nTarget: {target}\n\n" +
                "选择「覆盖」将删除该版本目录后重新构建。\n" +
                "选择「取消」可回去把资源版本 +1 再构建。",
                "覆盖并重建",
                "取消构建");

            if (!overwrite)
            {
                Debug.LogWarning(
                    $"[Yoo] 用户取消构建：版本输出目录已存在。\n" +
                    $"  目录: {packageOutputDirectory}\n" +
                    $"  建议：在构建面板把资源版本从 {version} 改为 {version}+1 后再构建。");
                throw new OperationCanceledException(
                    $"[Yoo] 已取消：输出目录已存在 {packageOutputDirectory}。请升版本号或确认覆盖后重试。");
            }

            Directory.Delete(packageOutputDirectory, true);
            Debug.Log($"[Yoo] 用户确认覆盖，已删除版本输出目录: {packageOutputDirectory}");
        }

        var buildParameters = new LegacyBuildParameters
        {
            BuildOutputRoot = buildOutputRoot,
            // 首包根目录 = StreamingAssets/yoo（Yoo 默认）
            BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot(),
            BuildPipeline = EBuildPipeline.LegacyBuildPipeline.ToString(),
            BuildBundleType = (int)EBundleType.AssetBundle,
            BuildTarget = target,
            PackageName = packageName,
            PackageVersion = version,
            VerifyBuildingResult = true,
            EnableSharePackRule = true,
            FileNameStyle = EFileNameStyle.BundleName_HashName,
            CompressOption = ECompressOption.LZ4,
            // false：不删整个 Package 根（保留 OutputCache 便于增量）
            // true：会删掉 Bundles/{Target}/{Package}/ 整棵树，更干净但更慢
            ClearBuildCacheFiles = false,
            UseAssetDependencyDB = true,

            // ★ 官方首包拷贝选项（学习重点）
            // None              : 不拷 StreamingAssets，也不生成 Catalog
            // ClearAndCopyAll   : 清空该 Package 首包目录 → 拷全部 → TaskCreateCatalog 写 BuiltinCatalog
            // OnlyCopyAll       : 不清空，直接拷全部并生成 Catalog
            BundledCopyOption = EBundledCopyOption.ClearAndCopyAll,
            BundledCopyParams = string.Empty,
        };

        var pipeline = new LegacyBuildPipeline();
        BuildResult result = pipeline.Run(buildParameters, true);
        if (!result.Success)
            throw new Exception($"[Yoo] 构建失败 {packageName}: {result.ErrorInfo}");

        string streamingPkg = BuildPaths.YooStreamingPackage(packageName);
        AssertBuiltinCatalogExists(packageName, streamingPkg);

        Debug.Log($"[Yoo] 构建成功: {result.OutputPackageDirectory}");
        Debug.Log($"[Yoo] 首包目录(已含 Catalog): {streamingPkg}");
    }

    public static void ClearStreamingAssetsYoo()
    {
        string yooRoot = Path.Combine(BuildPaths.StreamingAssets, "yoo");
        if (Directory.Exists(yooRoot))
            FileOps.CreateOrClearDirectory(yooRoot);
        Debug.Log("[Yoo] 已清理 StreamingAssets/yoo（下次构建会按 BundledCopyOption 重新写入）");
        FileOps.RefreshAssets();
    }

    /// <summary>
    /// 校验 StreamingAssets 首包是否完整。
    /// 说明：首包应由「构建 + BundledCopyOption」写入，不要手工 File.Copy 整包替代，
    /// 否则会缺 BuiltinCatalog.bytes。若缺失请重新执行构建步骤。
    /// </summary>
    public static void CopyToStreamingAssets()
    {
        // 构建已用 ClearAndCopyAll 写入 StreamingAssets，这里只做完整性检查 + 写 version.txt
        foreach (string package in new[] { PackageAllBundle, PackageDllBundle })
        {
            string dest = BuildPaths.YooStreamingPackage(package);
            if (!Directory.Exists(dest))
            {
                throw new DirectoryNotFoundException(
                    $"[Yoo] StreamingAssets 中没有 {package}。\n" +
                    $"请先执行构建（BundledCopyOption=ClearAndCopyAll 会自动拷贝到 {dest}）。");
            }

            AssertBuiltinCatalogExists(package, dest);
            Debug.Log($"[Yoo] 首包校验通过: {dest}");
        }

        FileOps.WriteVersionFile(Path.Combine(BuildPaths.StreamingAssets, "version.txt"), BuildParams.AssetVersion);
        FileOps.RefreshAssets();
    }

    /// <summary>
    /// 构建管线在 ClearAndCopyAll 后应已生成 Catalog；缺失则明确报错，引导重新构建。
    /// </summary>
    static void AssertBuiltinCatalogExists(string packageName, string packageDirectory)
    {
        string catalog = Path.Combine(packageDirectory, "BuiltinCatalog.bytes");
        if (File.Exists(catalog))
            return;

        throw new FileNotFoundException(
            $"[Yoo] 缺少 BuiltinCatalog.bytes: {catalog}\n" +
            "Offline/Host 初始化需要该文件。\n" +
            "正确做法：构建时设置 BundledCopyOption = ClearAndCopyAll（本步骤已配置），" +
            "由官方 TaskCreateCatalog 生成。\n" +
            "请重新执行「Yoo 构建 AllBundle / DllBundle」，不要只手工拷贝 bundle 文件。",
            catalog);
    }

    /// <summary>
    /// 拷贝到 IIS：C:\IIS_ServerData\YooAsset\{ver}\AssetBundles\{Package}\
    /// 热更 CDN 用构建输出目录即可（不需要 BuiltinCatalog；Catalog 只服务 StreamingAssets 内置文件系统）。
    /// </summary>
    public static void CopyToLocalServer()
    {
        string version = BuildParams.AssetVersion;
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string iisAssetBundles = BuildParams.GetIisVersionAssetBundles(Backend, version);

        foreach (string package in new[] { PackageAllBundle, PackageDllBundle })
        {
            string src = BuildPaths.YooPackageOutput(target, package, version);
            if (!Directory.Exists(src))
                throw new Exception($"[Yoo] 构建产物不存在: {src}");

            string dest = Path.Combine(iisAssetBundles, package);
            FileOps.CopyDirectory(src, dest, clearDest: true);
            Debug.Log($"[Yoo] IIS: {src} -> {dest}");
        }

        FileOps.WriteVersionFile(BuildParams.GetIisVersionFile(Backend), version);
        Debug.Log($"[Yoo] 请求根: {BuildParams.GetRemoteUrl(Backend)}/{version}/AssetBundles");
        Debug.Log($"[Yoo] version URL: {BuildParams.GetRemoteUrl(Backend)}/version.txt");

        if (BuildParams.CopyToResLocalServer)
        {
            string localRoot = BuildPaths.GetResLocalServerAssetBundles(Backend, version);
            foreach (string package in new[] { PackageAllBundle, PackageDllBundle })
            {
                string src = BuildPaths.YooPackageOutput(target, package, version);
                FileOps.CopyDirectory(src, Path.Combine(localRoot, package), clearDest: true);
            }
            FileOps.WriteVersionFile(BuildPaths.GetResLocalServerVersionFile(Backend), version);
        }
    }
}
