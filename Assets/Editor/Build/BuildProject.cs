using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using BM;
using Scripts_AOT.Utility;
/*
 * 导出工程
 * 
 * 安卓包上传到后台后，记得把build.gradle里的版本号+1提交,备份好Buyly mapping文件
 */

public class BuildProject
{

    private static string _tempBuildFolderName = "TempBuild";

    public static void BuildAndroidProject()
    {
        string outPutPath = Path.Combine(GetProjectPath(), _tempBuildFolderName);
        bool goon = EditorUtility.DisplayDialog("打包平台为安卓", $"临时输出路径为：{outPutPath}\n安卓项目路径：{BuildProjectWindows.GetAndroidOutPath()}\n", "继续", "取消");
        if (!goon)
        {
            BuildProjectWindows.Init();
            return;
        };
        // 切换Split Application Binary状态
        PlayerSettings.Android.useAPKExpansionFiles = BuildProjectWindows.GetShouldEnableSplitAPK();
        EditorUserBuildSettings.buildAppBundle = BuildProjectWindows.GetShouldEnableSplitAPK();
        AssetDatabase.SaveAssets();
        // 检查当前的Split Application Binary状态
        bool currentSetting = PlayerSettings.Android.useAPKExpansionFiles;
        // 显示状态信息
        string message = currentSetting ? "分包设置 已启用" : "分包设置 已禁用";
        goon = EditorUtility.DisplayDialog("Split Application Binary Setting ", message, "继续", "取消");
        if (!goon)
        {
            BuildProjectWindows.Init();
            return;
        };

        Debug.Log("OutputPath :" + outPutPath);
        Time.timeScale = 1;
        Application.targetFrameRate = 30;
        BuildProjectAndroidStudio(outPutPath);
    }

    public static void BuildXcodeProject1()
    {
        string outPutPath = Path.Combine(GetProjectPath(), _tempBuildFolderName);
        bool goon = EditorUtility.DisplayDialog("打包平台为IOS", $"临时输出路径为：{outPutPath}\nios项目路径：{BuildProjectWindows.GetIosOutPath()}\n", "继续", "取消");
        if (!goon)
        {
            BuildProjectWindows.Init();
            return;
        };
        Debug.Log("OutputPath :" + outPutPath);
        Time.timeScale = 1;
        Application.targetFrameRate = 30;
        BuildProjectXcode(outPutPath);
    }

    static List<string> GetSceneName()
    {
        List<string> sceneNames = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled)
                sceneNames.Add(scene.path);
        return sceneNames;
    }

    public static void BuildProjectAndroidStudio(string exportDirPath)
    {
        if (!Directory.Exists(exportDirPath))
            Directory.CreateDirectory(exportDirPath);

        // 清除资源目录
        var srcPath = Path.Combine(exportDirPath, "unityLibrary/src/main");
        if (Directory.Exists(srcPath))
        {
            Directory.Delete(srcPath, true);
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        EditorUserBuildSettings.buildAppBundle = BuildProjectWindows.GetShouldEnableSplitAPK();

        //EditorUserBuildSettings.development = true;
        //EditorUserBuildSettings.connectProfiler = true;
        //EditorUserBuildSettings.buildWithDeepProfilingSupport = true;

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        List<string> sceneNames = GetSceneName();
        BuildPipeline.BuildPlayer(sceneNames.ToArray(), exportDirPath, BuildTarget.Android, BuildOptions.AcceptExternalModificationsToPlayer);
    }

    public static void BuildProjectXcode(string exportDirPath)
    {
        if (!Directory.Exists(exportDirPath))
            Directory.CreateDirectory(exportDirPath);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        List<string> sceneNames = GetSceneName();
        BuildPipeline.BuildPlayer(sceneNames.ToArray(), exportDirPath, BuildTarget.iOS, BuildOptions.None);
    }

    public static void CopyDirectory(string from, string to)
    {
        if (!Directory.Exists(from)) return;

        if (!Directory.Exists(to))
            Directory.CreateDirectory(to);

        List<string> files = new List<string>(Directory.GetFiles(from));
        files.ForEach(c =>
        {
            string destFile = Path.Combine(new string[] { to, Path.GetFileName(c) });
            File.Copy(c, destFile, true);
        });
        List<string> folders = new List<string>(Directory.GetDirectories(from));
        folders.ForEach(c =>
        {
            string destDir = Path.Combine(new string[] { to, Path.GetFileName(c) });
            CopyDirectory(c, destDir);
        });
    }

    public static string GetProjectPath()
    {
        return Application.dataPath.Remove(Application.dataPath.Length - 7);
    }

    public static string GetFullProjectPath()
    {
        return Path.Combine(GetProjectPath(), _tempBuildFolderName);
    }

    [MenuItem("打包/删除AB包资源缓存")]
    public static void ClearABBundleCached()
    {
        var path = Application.dataPath + "/../BMBuild";
        if (Directory.Exists(path))
        {
            FileUtils.ClearDir(path);
        }
        Debug.Log("删除文件夹下的内容：" + path);

        path = AssetComponentConfig.LocalBundlePath;
        if (Directory.Exists(path))
        {
            FileUtils.ClearDir(path);
        }
        Debug.Log("删除文件夹下的内容：" + path);
        AssetDatabase.Refresh();
    }

    [MenuItem("打包/构建AB资源")]
    public static void BuildAssetBundle()
    {
        //加载配置文件
        BundleMasterRuntimeConfig BMRuntimeConfig = AssetDatabase.LoadAssetAtPath<BundleMasterRuntimeConfig>(BundleMasterWindow.RuntimeConfigPath);
        if (BMRuntimeConfig == null)
        {
            if (EditorUtility.DisplayDialog("错误", $"缺少运行时BM配置文件:{BundleMasterWindow.RuntimeConfigPath}", "确认"))
            {
                Debug.Log("缺少运行时BM配置文件 取消了操作！");
            }
            return;
        }
        if (BMRuntimeConfig.AssetLoadMode != AssetLoadMode.Build)
        {
            if (EditorUtility.DisplayDialog("错误", $"打包需要设置为构建模式", "确认"))
            {
                Debug.Log("打包需要设置为构建模式 取消了操作！");
            }
            return;
        }
        BM.BuildAssets.BuildAllBundle();
    }

    [MenuItem("打包/CopyAssetBundleToStreamingAssets")]
    public static void CopyAssetBundleToStreamingAssets()
    {
        BuildAssets.CopyFileToTargetFolder(AssetComponentConfig.LocalBundlePath);
    }

    const string Hot_Update_Dlls = "HotUpdateDlls";
    const string AOT_Assembly_Metadata_Dlls = "AOTAssemblyMetadataDlls";
    [MenuItem("打包/删除AOT和热更dll文件夹")]
    public static void ClearDllsAndAOTMetadata()
    {
        var targetPath = Application.streamingAssetsPath;
        //目标路径
        string targetHotUpdateDllsPath = Path.Combine(targetPath, Hot_Update_Dlls);
        string targetAOTAssemblyMetadataDlls = Path.Combine(targetPath, AOT_Assembly_Metadata_Dlls);

        if (Directory.Exists(targetHotUpdateDllsPath))
        {
            DeleteHelper.DeleteDir(targetHotUpdateDllsPath);
        }

        if (Directory.Exists(targetAOTAssemblyMetadataDlls))
        {
            DeleteHelper.DeleteDir(targetAOTAssemblyMetadataDlls);
        }
    }

    [MenuItem("打包/生成热更桥接文件等")]
    public static void HybirdCLRGenerateAll()
    {
        HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
    }

    [MenuItem("打包/编译热更dll")]
    public static void CompileHotfixDll()
    {
        var targetPath = Application.streamingAssetsPath;
        HybridCLR.Editor.Commands.CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
        //编译dll源文件夹
        string hybridCLRDataPath = Application.dataPath + @"\..\HybridCLRData";
        //源路径
        string aotMetadataDllsPath = Path.Combine(hybridCLRDataPath, @"AssembliesPostIl2CppStrip\Android");  //AOT补充元数据
        string hotUpdateDllsPath = Path.Combine(hybridCLRDataPath, @"HotUpdateDlls\Android");   //热更新DLL路径
        //版本控制文件名
        string AOTAssemblyMetadataVersion = "AOTAssemblyMetadataVersion.txt";
        string hotUpdateDllsVersion = "DllHotUpdateVersion.txt";
        //拷贝目标路径
        string targetHotUpdateDllsPath = Path.Combine(targetPath, Hot_Update_Dlls);
        string targetAOTAssemblyMetadataDlls = Path.Combine(targetPath, AOT_Assembly_Metadata_Dlls);

        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        if (!Directory.Exists(targetHotUpdateDllsPath))
        {
            Directory.CreateDirectory(targetHotUpdateDllsPath);
        }
        DeleteHelper.DeleteDir(targetHotUpdateDllsPath);

        if (!Directory.Exists(targetAOTAssemblyMetadataDlls))
        {
            Directory.CreateDirectory(targetAOTAssemblyMetadataDlls);
        }
        DeleteHelper.DeleteDir(targetAOTAssemblyMetadataDlls);

        Dictionary<string, HotUpdateFileInfo> aotLocalVersionDic = new Dictionary<string, HotUpdateFileInfo>();


        Debug.Log("导出路径：" + targetPath);

        foreach (string name in MetadataConfig.AotAssemblyMetadatas)
        {
            string fullName = Path.Combine(aotMetadataDllsPath, name);
            if (!File.Exists(fullName))
            {
                Debug.LogError("【警告】缺少补充元数据文件：" + name);
                continue;
            }

            byte[] data = File.ReadAllBytes(fullName);
            HotUpdateFileInfo hotUpdateFileInfo;
            hotUpdateFileInfo.Name = name;
            hotUpdateFileInfo.Size = data.Length;
            hotUpdateFileInfo.Version = HashHelper.GetHashString(data, HashAlgorithmType.MD5);
            aotLocalVersionDic.Add(name, hotUpdateFileInfo);
            File.Copy(fullName, Path.Combine(targetAOTAssemblyMetadataDlls, name), true);
        }
        Debug.Log("AOT 补充元数据 版本信息处理完毕");

        List<string> hotUpdateDllFiles = new List<string> { "Game.dll" };
        Dictionary<string, HotUpdateFileInfo> hotUpdateLocalVersionDic = new Dictionary<string, HotUpdateFileInfo>();
        foreach (string dllName in hotUpdateDllFiles)
        {
            string fullName = Path.Combine(hotUpdateDllsPath, dllName);
            string targetName = dllName + ".bytes";
            byte[] data = File.ReadAllBytes(fullName);

            HotUpdateFileInfo hotUpdateFileInfo;
            hotUpdateFileInfo.Name = targetName;
            hotUpdateFileInfo.Size = data.Length;
            hotUpdateFileInfo.Version = HashHelper.GetHashString(data, HashAlgorithmType.MD5);
            hotUpdateLocalVersionDic.Add(targetName, hotUpdateFileInfo);
            File.WriteAllBytes(Path.Combine(targetHotUpdateDllsPath, targetName), data);
        }
        AssetDatabase.Refresh();
        Debug.Log("热更dll 版本信息处理完毕");
    }
}

public struct HotUpdateFileInfo
{
    public string Name;
    public int Size;
    public string Version;
}
