using System.Collections.Generic;
using System.IO;
using BM;
using Scripts_AOT.Utility;
using UnityEditor;
using UnityEngine;
using UnityEditor.iOS.Xcode;
using HybridCLR.Editor.AOT;
using System.Text;

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

    [MenuItem("打包/Test/Build Success")]
    public static void TestBuildSuccess()
    {
        Debug.Log("TestBuildSuccess");
        EditorApplication.Exit(0);
    }

    [MenuItem("打包/Test/Build Fail")]
    public static void TestBuildFail()
    {
        Debug.Log("TestBuildFail");
        EditorApplication.Exit(-1);
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

        BackupBundleAsset();
    }

    [MenuItem("打包/BackupBundleAsset")]
    public static void BackupBundleAsset()
    {
        int version = ResConfig.Instance.ResVersion;
        string path = Path.Combine(GetProjectPath(), "ResLocalRecord", version.ToString(), "AssetBundles");
        //BuildAssets.CopyFileToTargetFolder(path);
        AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterWindow.AssetLoadTablePath);
        FileHelper.CopyDir(assetLoadTable.BuildBundlePath, path);
    }

    [MenuItem("打包/CopyAssetBundleToStreamingAssets")]
    public static void CopyAssetBundleToStreamingAssets()
    {
        int version = ResConfig.Instance.ResVersion;
        string path = Path.Combine(GetProjectPath(), "ResLocalRecord", version.ToString(), AssetComponentConfig.LocalBundlePath);
        AssetLoadTable assetLoadTable = AssetDatabase.LoadAssetAtPath<AssetLoadTable>(BundleMasterWindow.AssetLoadTablePath);
        FileHelper.CopyDir(assetLoadTable.BuildBundlePath, path);
    }

    const string Hot_Update_Dlls = "HotUpdateDlls";
    const string AOT_Assembly_Metadata_Dlls = "AOTAssemblyMetadataDlls";
    [MenuItem("打包/删除AOT和热更dll文件夹")]
    public static void ClearDllsAndAOTMetadata()
    {
        var targetPath = Path.Combine(Application.dataPath, "HotDll");
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
        var targetPath = Path.Combine(Application.dataPath, "HotDll");
        HybridCLR.Editor.Commands.CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
        //编译dll源文件夹
        string hybridCLRDataPath = Application.dataPath + @"\..\HybridCLRData";
#if UNITY_ANDROID
        //源路径
        string aotMetadataDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"AssembliesPostIl2CppStrip\Android"));  //AOT补充元数据
        string hotUpdateDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"HotUpdateDlls\Android"));   //热更新DLL路径
#elif UNITY_IOS
        //源路径
        string aotMetadataDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"AssembliesPostIl2CppStrip\iOS"));  //AOT补充元数据
        string hotUpdateDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"HotUpdateDlls\iOS"));   //热更新DLL路径
#endif
        //版本控制文件名
        string AOTAssemblyMetadataVersion = "AOTAssemblyMetadataVersion.txt";
        string hotUpdateDllsVersion = "DllHotUpdateVersion.txt";
        //拷贝目标路径
        string targetHotUpdateDllsPath = Path.Combine(targetPath, Hot_Update_Dlls);
        string targetAOTAssemblyMetadataDlls = Path.Combine(targetPath, AOT_Assembly_Metadata_Dlls);

        int version = ResConfig.Instance.ResVersion;

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

        //裁剪补充元数据
        MyAOTAssemblyMetadataStripper();

        foreach (string name in MetadataConfig.AotAssemblyMetadatas)
        {
            string finalName = MetadataConfig.GetStripMetadataName(name);
            string targetName = finalName + ".bytes";
            string fullName = ConvertPath(Path.Combine(aotMetadataDllsPath, finalName));
            Debug.Log($"fullpath {fullName}");
            if (!File.Exists(fullName))
            {
                Debug.LogError("【警告】缺少补充元数据文件：" + finalName);
                continue;
            }

            byte[] data = File.ReadAllBytes(fullName);
            HotUpdateFileInfo hotUpdateFileInfo;
            hotUpdateFileInfo.Name = finalName;
            hotUpdateFileInfo.Size = data.Length;
            hotUpdateFileInfo.Version = HashHelper.GetHashString(data, HashAlgorithmType.MD5);
            aotLocalVersionDic.Add(finalName, hotUpdateFileInfo);
            File.Copy(fullName, ConvertPath(Path.Combine(targetAOTAssemblyMetadataDlls, targetName)), true);
        }
        Debug.Log("AOT 补充元数据 版本信息处理完毕");

        List<string> hotUpdateDllFiles = new List<string> { "Game.dll" };
        Dictionary<string, HotUpdateFileInfo> hotUpdateLocalVersionDic = new Dictionary<string, HotUpdateFileInfo>();
        foreach (string dllName in hotUpdateDllFiles)
        {
            string fullName = ConvertPath(Path.Combine(hotUpdateDllsPath, dllName));
            string targetName = dllName + ".bytes";
            byte[] data = File.ReadAllBytes(fullName);

            HotUpdateFileInfo hotUpdateFileInfo;
            hotUpdateFileInfo.Name = targetName;
            hotUpdateFileInfo.Size = data.Length;
            hotUpdateFileInfo.Version = HashHelper.GetHashString(data, HashAlgorithmType.MD5);
            hotUpdateLocalVersionDic.Add(targetName, hotUpdateFileInfo);
            string filePath = ConvertPath(Path.Combine(targetHotUpdateDllsPath, targetName));
            File.WriteAllBytes(filePath, data);
        }
        AssetDatabase.Refresh();
        Debug.Log("热更dll 版本信息处理完毕");
    }

    [MenuItem("打包/补充元数据裁剪")]
    public static void MyAOTAssemblyMetadataStripper()
    {
        var targetPath = Path.Combine(Application.dataPath, "HotDll");
        HybridCLR.Editor.Commands.CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
        //编译dll源文件夹
        string hybridCLRDataPath = Application.dataPath + @"\..\HybridCLRData";
#if UNITY_ANDROID
        //源路径
        string aotMetadataDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"AssembliesPostIl2CppStrip\Android"));  //AOT补充元数据
        string hotUpdateDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"HotUpdateDlls\Android"));   //热更新DLL路径
#elif UNITY_IOS
        //源路径
        string aotMetadataDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"AssembliesPostIl2CppStrip\iOS"));  //AOT补充元数据
        string hotUpdateDllsPath = ConvertPath(Path.Combine(hybridCLRDataPath, @"HotUpdateDlls\iOS"));   //热更新DLL路径
#endif
        foreach (string name in MetadataConfig.AotAssemblyMetadatas)
        {
            string originDll = ConvertPath(Path.Combine(aotMetadataDllsPath, name));
            Debug.Log($"fullpath {originDll}");
            if (!File.Exists(originDll))
            {
                Debug.LogError("【警告】缺少补充元数据文件：" + name);
                continue;
            }
            string targetDll = ConvertPath(Path.Combine(aotMetadataDllsPath, MetadataConfig.GetStripMetadataName(name)));
            AOTAssemblyMetadataStripper.Strip(originDll, targetDll);
        }

    }

    [MenuItem("打包/生成版本文件")]
    public static void CreateVersionFile()
    {
        int version = ResConfig.Instance.ResVersion;
        string path = Path.Combine(GetProjectPath(), "ResLocalRecord");
        string versionPath = Path.Combine(path, "version.txt");

        File.WriteAllText(versionPath, $"version={version}", Encoding.UTF8);

        string versionServerPath = Path.Combine(Application.streamingAssetsPath, "version.txt");
        if (File.Exists(versionServerPath))
        {
            File.Delete(versionServerPath);
        }
        FileUtil.CopyFileOrDirectory(versionPath, versionServerPath);
        Debug.Log("生成版本文件 完成！");
    }

    [MenuItem("打包/拷贝当前版本到本地服务器")]
    public static void CopyHotResToLocalServer()
    {
        int version = ResConfig.Instance.ResVersion;
        string srcPath = Path.Combine(GetProjectPath(), "ResLocalRecord", version.ToString());
        string destPath = Path.Combine(GetProjectPath(), "ResLocalServer", version.ToString());
        FileHelper.CopyDir(srcPath, destPath);

        string path = Path.Combine(GetProjectPath(), "ResLocalRecord");
        string versionPath = Path.Combine(path, "version.txt");
        string versionServerPath = Path.Combine(GetProjectPath(), "ResLocalServer", "version.txt");
        if (File.Exists(versionServerPath))
        {
            File.Delete(versionServerPath);
        }
        File.Copy(versionPath, versionServerPath);

        Debug.Log("CopyHotResToLocalServer 完成！");
    }

    [MenuItem("打包/Test/iOS 全拷贝测试")]
    public static void IOSFullCopyTest()
    {
        string originPath = Path.GetFullPath(Path.Combine(GetProjectPath(), "TempBuild"));
        //string targetPath = Path.GetFullPath(Path.Combine(GetProjectPath(), "../TempBuild"));
        string targetPath = BuildProjectWindows.GetIosOutPath();
        Debug.Log(originPath);
        Debug.Log(targetPath);

        static void CopyDirectory(string from, string to)
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

        CopyDirectory(originPath, targetPath);
    }

    [MenuItem("打包/Test/引用文件更新")]
    public static void UpdateFefFile()
    {
        string buildPath = BuildProjectWindows.GetIosOutPath();
        // 获取 Xcode 项目的路径
        string projectPath = PBXProject.GetPBXProjectPath(buildPath);

        // 加载 Xcode 项目
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        // 获取 target
        string target = project.GetUnityFrameworkTargetGuid();

        // 移除已经存在的 Classes/Native 文件引用
        RemoveExistingFilesFromProject(project, buildPath, target);

        // 添加新的文件到项目中
        AddFilesToProject(project, buildPath, target);

        // 保存修改
        project.WriteToFile(projectPath);
    }


    // 移除项目中已经存在的 Classes/Native 文件引用
    private static void RemoveExistingFilesFromProject(PBXProject project, string buildPath, string target)
    {
        // `Classes/Native` 文件夹路径
        string classesNativePath = Path.Combine(buildPath, "Classes/Native");

        // 获取所有文件
        string[] files = Directory.GetFiles(classesNativePath, "*.*", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            // 获取相对路径
            string relativePath = "Classes/Native/" + Path.GetFileName(file);

            // 查找项目中是否已经包含此文件
            string fileGuid = project.FindFileGuidByProjectPath(relativePath);

            if (!string.IsNullOrEmpty(fileGuid))
            {
                // 如果文件已经存在于项目中，删除它
                project.RemoveFile(fileGuid);
                project.RemoveFileFromBuild(target, fileGuid);
            }
        }
    }

    // 添加文件到 Xcode 项目
    private static void AddFilesToProject(PBXProject project, string buildPath, string target)
    {
        // `Classes/Native` 文件夹路径
        string classesNativePath = Path.Combine(buildPath, "Classes/Native");

        // 获取所有文件
        string[] files = Directory.GetFiles(classesNativePath, "*.*", SearchOption.AllDirectories);

        // 遍历文件并添加到 Xcode 项目
        foreach (string file in files)
        {
            // 添加文件到 Xcode 项目
            string relativePath = "Classes/Native/" + Path.GetFileName(file);
            string fileGuid = project.AddFile(file, relativePath);
            project.AddFileToBuild(target, fileGuid);
        }

        // 示例：添加必要的 Frameworks（根据项目需求）
        //project.AddFrameworkToProject(target, "AudioToolbox.framework", false);
        //project.AddFrameworkToProject(target, "CoreGraphics.framework", false);
    }

    [MenuItem("打包/Test/查找文件引用关系")]
    public static string CheckFileTarget()
    {
        string buildPath = BuildProjectWindows.GetIosOutPath();
        // 获取 Xcode 项目的路径
        string projectPath = PBXProject.GetPBXProjectPath(buildPath);
        // 加载 Xcode 项目
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        // 获取 targets 的 GUID
        string unityIphoneTarget = project.GetUnityMainTargetGuid();
        string unityFrameworkTarget = project.GetUnityFrameworkTargetGuid();
        Debug.Log("Iphone TargetId " + unityIphoneTarget);
        Debug.Log("Framwork TargetId " + unityFrameworkTarget);
        // 获取文件的相对路径
        string relativePath = "Libraries/GameAnalytics/Plugins/iOS/GameAnalytics.h";

        // 查找该文件在项目中的 GUID
        string fileGuid = project.FindFileGuidByProjectPath(relativePath);

        Debug.Log(fileGuid);
        if (!string.IsNullOrEmpty(fileGuid))
        {
            var GetTargetProductFileRef = project.GetTargetProductFileRef(unityIphoneTarget);
            Debug.Log($"GetTargetProductFileRef {GetTargetProductFileRef}");
        }


        //// 如果没有找到文件或文件不在任何 target 下
        return "Unknown Target";
    }

    public static string ConvertPath(string path)
    {
        // 将反斜杠 \ 替换为正斜杠 /
        path = path.Replace('\\', '/');

        // 处理相对路径 ..，在 macOS 中这会按照文件系统规则自动解析
        path = Path.GetFullPath(path); // 将路径转化为绝对路径

        return path;
    }
}

public struct HotUpdateFileInfo
{
    public string Name;
    public int Size;
    public string Version;
}
