using System;
using System.IO;
using System.Linq;
using HybridCLR.Editor;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HybridCLR 细粒度步骤。
/// 顺序建议：Clear → GenerateAll → Compile → StripAot → CopyHotDll → CopyAot。
/// 必须在打 DllBundle 之前完成，否则 AB 打进旧 DLL。
/// </summary>
public static class HybridClrSteps
{
    const string HotUpdateDllName = "Game.dll";

    public static void ClearHotDllFolders()
    {
        FileOps.CreateOrClearDirectory(BuildPaths.HotUpdateDlls);
        FileOps.CreateOrClearDirectory(BuildPaths.AotMetadataDlls);
        Debug.Log("[HybridCLR] 已清理 HotDll/HotUpdateDlls 与 AOTAssemblyMetadataDlls");
        FileOps.RefreshAssets();
    }

    public static void GenerateAll()
    {
        PrebuildCommand.GenerateAll();
        Debug.Log("[HybridCLR] GenerateAll 完成（link.xml / AOT 泛型 / wrapper）");
    }

    public static void CompileHotUpdateDll()
    {
        CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget);
        FileOps.RefreshAssets();
        Debug.Log($"[HybridCLR] CompileDll 完成: {EditorUserBuildSettings.activeBuildTarget}");
    }

    /// <summary>
    /// 对 AssembliesPostIl2CppStrip 中的 AOT DLL 做元数据裁剪，输出 Strip_*.dll。
    ///
    /// 注意：
    /// 1. 源 DLL 来自 BuildPlayer / HybridCLR Generate AOTDlls 后的裁剪结果；
    /// 2. 若 HybridCLRData/AssembliesPostIl2CppStrip/{Target} 为空，会尝试从
    ///    Library/Bee/artifacts/.../ManagedStripped 同步（Unity 2021+ 常见路径）；
    /// 3. 命名与旧逻辑一致：BundleMaster.dll → Strip_BundleMaster.dll（再拷贝为 .bytes）。
    /// </summary>
    public static void StripAotMetadata()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string aotDir = GetAotStripDirAbsolute(target);

        EnsureAotStripDllsReady(target, aotDir);

        int ok = 0;
        int fail = 0;
        foreach (string name in MetadataConfig.AotAssemblyMetadatas)
        {
            string originDll = ResolveOriginAotDll(aotDir, name);
            if (string.IsNullOrEmpty(originDll))
            {
                Debug.LogError(
                    $"[HybridCLR] 缺少补充元数据源文件: {name}\n" +
                    $"  搜索目录: {aotDir}\n" +
                    $"  请先执行菜单 HybridCLR/Generate/AOTDlls（或本面板 GenerateAll），" +
                    $"确保 AssembliesPostIl2CppStrip 有裁剪后的 AOT DLL。");
                fail++;
                continue;
            }

            // 与 BuildProject.MyAOTAssemblyMetadataStripper 完全一致：
            // GetStripMetadataName("BundleMaster.dll") => "Strip_BundleMaster.dll"
            string stripFileName = MetadataConfig.GetStripMetadataName(name);
            string targetDll = Path.GetFullPath(Path.Combine(aotDir, stripFileName));

            try
            {
                Debug.Log($"[HybridCLR] Strip 开始: {originDll} -> {targetDll}");
                AOTAssemblyMetadataStripper.Strip(originDll, targetDll);
                if (!File.Exists(targetDll))
                {
                    Debug.LogError($"[HybridCLR] Strip 未生成文件: {targetDll}");
                    fail++;
                    continue;
                }
                Debug.Log($"[HybridCLR] Strip 完成: {stripFileName} ({new FileInfo(targetDll).Length} bytes)");
                ok++;
            }
            catch (Exception e)
            {
                fail++;
                Debug.LogError($"[HybridCLR] Strip 异常 {name}: {e}");
            }
        }

        if (ok == 0)
        {
            throw new Exception(
                $"[HybridCLR] AOT 元数据裁剪失败：0 成功 / {fail} 失败。\n" +
                $"目录: {aotDir}\n" +
                "请先 HybridCLR/Generate/AOTDlls 生成 AssembliesPostIl2CppStrip 后再裁剪。");
        }

        if (fail > 0)
            Debug.LogWarning($"[HybridCLR] AOT 裁剪部分失败：成功 {ok}，失败 {fail}");
        else
            Debug.Log($"[HybridCLR] AOT 元数据裁剪全部成功：{ok} 个");
    }

    public static void CopyHotDllToAssets()
    {
        string hotDir = GetHotUpdateDllDirAbsolute();
        FileOps.EnsureDirectory(BuildPaths.HotUpdateDlls);

        string src = Path.Combine(hotDir, HotUpdateDllName);
        if (!File.Exists(src))
        {
            throw new FileNotFoundException(
                $"[HybridCLR] 找不到热更 DLL: {src}，请先执行「编译热更 DLL」。", src);
        }

        string dest = Path.Combine(BuildPaths.HotUpdateDlls, HotUpdateDllName + ".bytes");
        File.Copy(src, dest, true);
        Debug.Log($"[HybridCLR] 拷贝热更 DLL: {src} -> {dest}");
        FileOps.RefreshAssets();
    }

    public static void CopyAotMetadataToAssets()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string aotDir = GetAotStripDirAbsolute(target);
        FileOps.EnsureDirectory(BuildPaths.AotMetadataDlls);

        int ok = 0;
        foreach (string name in MetadataConfig.AotAssemblyMetadatas)
        {
            // 源：Strip_BundleMaster.dll ；目标：Strip_BundleMaster.dll.bytes（与 LoadDll 一致）
            string stripFileName = MetadataConfig.GetStripMetadataName(name);
            string full = Path.Combine(aotDir, stripFileName);
            if (!File.Exists(full))
            {
                Debug.LogError($"[HybridCLR] 缺少裁剪后元数据: {full}，请先执行 AOT 元数据裁剪。");
                continue;
            }

            string destName = stripFileName + ".bytes";
            string dest = Path.Combine(BuildPaths.AotMetadataDlls, destName);
            File.Copy(full, dest, true);
            Debug.Log($"[HybridCLR] 拷贝 AOT 元数据: {full} -> {dest}");
            ok++;
        }

        if (ok == 0)
            throw new Exception("[HybridCLR] 未拷贝任何 AOT 元数据，请先完成 Strip 步骤。");

        FileOps.RefreshAssets();
    }

    /// <summary>一键：清理+编译+裁剪+拷贝（不含 GenerateAll，GenerateAll 较慢可单独跑）。</summary>
    public static void CompileStripAndCopy()
    {
        ClearHotDllFolders();
        CompileHotUpdateDll();
        StripAotMetadata();
        CopyHotDllToAssets();
        CopyAotMetadataToAssets();
    }

    // -------------------------------------------------------------------------
    // 路径与源 DLL 准备
    // -------------------------------------------------------------------------

    static string GetHotUpdateDllDirAbsolute()
    {
        // 与 HybridCLR SettingsUtil.GetHotUpdateDllsOutputDirByTarget 一致，但返回绝对路径
        string relative = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget);
        return ToAbsoluteProjectPath(relative);
    }

    static string GetAotStripDirAbsolute(BuildTarget target)
    {
        // HybridCLR 配置默认: HybridCLRData/AssembliesPostIl2CppStrip/{Target}
        string relative = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
        return ToAbsoluteProjectPath(relative);
    }

    static string ToAbsoluteProjectPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);
        return Path.GetFullPath(Path.Combine(SettingsUtil.ProjectDir, path));
    }

    /// <summary>
    /// 保证 aotDir 内有 MetadataConfig 列出的源 DLL。
    /// 若缺失，尝试从 Unity ManagedStripped 产物同步（无需完整再打一次包）。
    /// </summary>
    static void EnsureAotStripDllsReady(BuildTarget target, string aotDir)
    {
        FileOps.EnsureDirectory(aotDir);

        bool anyMissing = MetadataConfig.AotAssemblyMetadatas.Any(name =>
            string.IsNullOrEmpty(ResolveOriginAotDll(aotDir, name)));

        if (!anyMissing)
        {
            Debug.Log($"[HybridCLR] AOT 源目录已就绪: {aotDir}");
            return;
        }

        string managedStripped = GetManagedStrippedDir(target);
        Debug.LogWarning(
            $"[HybridCLR] AssembliesPostIl2CppStrip 缺少源 DLL，尝试从 ManagedStripped 同步:\n" +
            $"  目标: {aotDir}\n" +
            $"  来源: {managedStripped}");

        if (string.IsNullOrEmpty(managedStripped) || !Directory.Exists(managedStripped))
        {
            throw new DirectoryNotFoundException(
                $"[HybridCLR] 找不到 AOT 裁剪源目录。\n" +
                $"  AssembliesPostIl2CppStrip: {aotDir} （空或不完整）\n" +
                $"  ManagedStripped: {managedStripped} （不存在）\n" +
                "请先执行菜单 HybridCLR/Generate/AOTDlls 或完整导出一次 Player，" +
                "让 HybridCLR 把裁剪后的 AOT DLL 拷到 AssembliesPostIl2CppStrip。");
        }

        int copied = 0;
        foreach (string file in Directory.GetFiles(managedStripped, "*.dll"))
        {
            string dest = Path.Combine(aotDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
            copied++;
        }
        Debug.Log($"[HybridCLR] 已从 ManagedStripped 同步 {copied} 个 DLL → {aotDir}");

        // 再检查 MetadataConfig 是否齐
        var stillMissing = MetadataConfig.AotAssemblyMetadatas
            .Where(name => string.IsNullOrEmpty(ResolveOriginAotDll(aotDir, name)))
            .ToArray();
        if (stillMissing.Length > 0)
        {
            throw new FileNotFoundException(
                "[HybridCLR] 同步后仍缺少以下 AOT DLL（请检查 MetadataConfig 与裁剪产物）:\n- " +
                string.Join("\n- ", stillMissing));
        }
    }

    static string ResolveOriginAotDll(string aotDir, string name)
    {
        // MetadataConfig 写的是 "BundleMaster.dll" / "System.dll"
        string direct = Path.Combine(aotDir, name);
        if (File.Exists(direct))
            return Path.GetFullPath(direct);

        if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            string withExt = Path.Combine(aotDir, name + ".dll");
            if (File.Exists(withExt))
                return Path.GetFullPath(withExt);
        }

        return null;
    }

    /// <summary>
    /// Unity 2021+ 各平台 ManagedStripped 路径（与 HybridCLR CopyStrippedAOTAssemblies 保持一致）。
    /// </summary>
    static string GetManagedStrippedDir(BuildTarget target)
    {
        string projectDir = SettingsUtil.ProjectDir;
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return Path.Combine(projectDir, "Library/Bee/artifacts/WinPlayerBuildProgram/ManagedStripped");
            case BuildTarget.StandaloneLinux64:
                return Path.Combine(projectDir, "Library/Bee/artifacts/LinuxPlayerBuildProgram/ManagedStripped");
            case BuildTarget.Android:
                return Path.Combine(projectDir, "Library/Bee/artifacts/Android/ManagedStripped");
            case BuildTarget.iOS:
                return Path.Combine(projectDir, "Library/Bee/artifacts/iOS/ManagedStripped");
            case BuildTarget.WebGL:
                return Path.Combine(projectDir, "Library/Bee/artifacts/WebGL/ManagedStripped");
            case BuildTarget.StandaloneOSX:
                return Path.Combine(projectDir, "Library/Bee/artifacts/MacStandalonePlayerBuildProgram/ManagedStripped");
            default:
                return Path.Combine(projectDir, "Library/Bee/artifacts", target.ToString(), "ManagedStripped");
        }
    }
}
