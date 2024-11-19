using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.Collections.Generic;
using System.IO;
using UnityEditor.iOS.Xcode.Extensions;

/*
 * 打包后将打包出来的资源拷贝到目标ios项目中
 *
 */
public class OnPostprocessBuild_IOS : MonoBehaviour
{

    static string pathSetting;
    static string pathExport;
    static PBXProject pbx;

    private static string _copyPathRoot = "/";
    //拷贝文件夹
    private static string[] _copyPathFolders = new string[] { "Data", "Classes/Native" };
    //忽略删除文件夹
    private static string[] _ignoreDeleteFolderOrFile = new string[] { "test" };
    //拷贝前清空删除文件夹
    private static string[] _deleteFolder = new string[] { "Data", "Classes/Native" };

    public static void CopyUnityRes()
    {
        string path = BuildProject.GetFullProjectPath();
        OnPostprocessBuild(BuildTarget.iOS, path);
    }

    [PostProcessBuild(999)]
    public static void OnPostprocessBuild(BuildTarget BuildTarget, string path)
    {
        path = path.Replace(@"\", "/");
        if (BuildTarget != BuildTarget.iOS) return;
        if (!Directory.Exists(path)) return;
        if (!BuildProjectWindows.GetAfterBuildCopyRes()) return;
        //清除
        // CleanFolder();
        //拷贝打包的资源到ios项目中
        for (int i = 0; i < _copyPathFolders.Length; i++)
        {
            pathSetting = path + _copyPathRoot + _copyPathFolders[i];
            pathExport = BuildProjectWindows.GetIosOutPath() + _copyPathRoot + _copyPathFolders[i];
            Copy(pathSetting);
            EditorUtility.DisplayProgressBar("拷贝文件", pathSetting, i * 1.0f / _copyPathFolders.Length * 1.0f);
            Debug.Log(pathExport + " Complete!");
        }

        EditorUtility.ClearProgressBar();
        Debug.LogWarning("拷贝打包的资源到ios项目完成！" + System.DateTime.Now);
    }


    static void Copy(string path)
    {
        Debug.Log(path);
        DirectoryInfo dir = new DirectoryInfo(path);
        FileSystemInfo[] files = dir.GetFileSystemInfos();
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i] is DirectoryInfo)
            {
                string sub = pathExport + files[i].FullName.Replace(@"\", "/").Replace(pathSetting, "");
                if (!Directory.Exists(sub))
                    Directory.CreateDirectory(sub);
                Copy(files[i].FullName);
            }
            else
            {
                string sub = pathExport + files[i].FullName.Replace(@"\", "/").Replace(pathSetting, "");
                File.Copy(files[i].FullName, sub, true);
            }
        }
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


    private static void SetXcodeSetting(string targetPath)
    {
        string projPath = PBXProject.GetPBXProjectPath(targetPath);
        PBXProject proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));
        string unityTarget = proj.GetUnityFrameworkTargetGuid();

        proj.AddBuildProperty(unityTarget, "OTHER_LDFLAGS", "-ObjC");
        proj.AddBuildProperty(unityTarget, "OTHER_LDFLAGS", "-l\"c++\"");
        proj.AddBuildProperty(unityTarget, "OTHER_LDFLAGS", "-l\"c++abi\"");
        proj.AddBuildProperty(unityTarget, "OTHER_LDFLAGS", "-l\"sqlite3\"");
        proj.AddBuildProperty(unityTarget, "OTHER_LDFLAGS", "-l\"z\"");

        // 获取OpenUDID.m文件对应的文件GUID
        string openUDIDFileGuid = proj.FindFileGuidByProjectPath("Plugins/IOS/OpenUDID/OpenUDID.m");
        // 设置OpenUDID.m文件的Compiler Flags
        proj.SetCompileFlagsForFile(projPath, openUDIDFileGuid, new List<string>() { "-fno-objc-arc" });
        // 设置"Enable Objective-C Exceptions"的值为"Yes"
        proj.SetBuildProperty(unityTarget, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
        // 设置"Enable Bitcode"的值为"No"
        proj.SetBuildProperty(unityTarget, "ENABLE_BITCODE", "NO");

        proj.AddFrameworkToProject(unityTarget, "Accelerate.framework", false);
        proj.AddFrameworkToProject(unityTarget, "AdSupport.framework", false);
        proj.AddFrameworkToProject(unityTarget, "AppTrackingTransparency.framework", false);
        proj.AddFrameworkToProject(unityTarget, "AudioToolbox.framework", false);
        proj.AddFrameworkToProject(unityTarget, "AVFoundation.framework", false);
        proj.AddFrameworkToProject(unityTarget, "CoreGraphics.framework", false);
        proj.AddFrameworkToProject(unityTarget, "CoreImage.framework", false);
        proj.AddFrameworkToProject(unityTarget, "CoreLocation.framework", false);
        proj.AddFrameworkToProject(unityTarget, "CoreMedia.framework", false);
        proj.AddFrameworkToProject(unityTarget, "CoreMotion.framework", false);
        proj.AddFrameworkToProject(unityTarget, "CoreTelephony.framework", false);
        proj.AddFrameworkToProject(unityTarget, "CoreText.framework", false);
        proj.AddFrameworkToProject(unityTarget, "ImageIO.framework", false);
        proj.AddFrameworkToProject(unityTarget, "JavaScriptCore.framework", false);
        proj.AddFrameworkToProject(unityTarget, "MapKit.framework", false);
        proj.AddFrameworkToProject(unityTarget, "MediaPlayer.framework", false);
        proj.AddFrameworkToProject(unityTarget, "MobileCoreServices.framework", false);
        proj.AddFrameworkToProject(unityTarget, "QuartzCore.framework", false);
        proj.AddFrameworkToProject(unityTarget, "Security.framework", false);
        proj.AddFrameworkToProject(unityTarget, "StoreKit.framework", false);
        proj.AddFrameworkToProject(unityTarget, "SystemConfiguration.framework", false);
        proj.AddFrameworkToProject(unityTarget, "UIKit.framework", false);
        proj.AddFrameworkToProject(unityTarget, "WebKit.framework", false);
        proj.AddFrameworkToProject(unityTarget, "DeviceCheck.framework", false);
        proj.AddFrameworkToProject(unityTarget, "libbz2.tbd", false);
        proj.AddFrameworkToProject(unityTarget, "libc++.tbd", false);
        proj.AddFrameworkToProject(unityTarget, "libiconv.tbd", false);
        proj.AddFrameworkToProject(unityTarget, "libresolv.9.tbd", false);
        proj.AddFrameworkToProject(unityTarget, "libsqlite3.tbd", false);
        proj.AddFrameworkToProject(unityTarget, "libxml2.tbd", false);
        proj.AddFrameworkToProject(unityTarget, "libz.tbd", false);
        proj.AddFrameworkToProject(unityTarget, "libc++abi.tbd", false);

        string mainTarget = proj.GetUnityMainTargetGuid();
        string feedBackBundle = proj.FindFileGuidByProjectPath("Frameworks/Plugins/IOS/Pods/ZYFeedback/ZYFeedback/ZYFeedback/ZYFeedback.bundle");
        proj.RemoveFileFromBuild(unityTarget, feedBackBundle);
        proj.AddFileToBuild(mainTarget, feedBackBundle);

        string content = proj.WriteToString();
        File.WriteAllText(projPath, content);

        string infoPlistPath = targetPath + "/info.plist";
        PlistDocument infoPlistDoc = new PlistDocument();
        infoPlistDoc.ReadFromFile(infoPlistPath);

        // 修改为中文
        infoPlistDoc.root.SetString("CFBundleDevelopmentRegion", "zh_CN");

        if (!infoPlistDoc.root.values.ContainsKey("Launch screen interface file base name"))
        {
            infoPlistDoc.root.SetString("Launch screen interface file base name", "Launch screen");
        }

        if (!infoPlistDoc.root.values.ContainsKey("NSPhotoLibraryUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSLocationWhenInUseUsageDescription", "授权后您可以选择相册中的照片作为头像");
        }

        if (!infoPlistDoc.root.values.ContainsKey("NSLocationWhenInUseUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSLocationWhenInUseUsageDescription", "APP需要你的允许，才能访问地理位置");
        }

        if (!infoPlistDoc.root.values.ContainsKey("NSContactsUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSContactsUsageDescription", "APP需要你的允许，才能访问通讯录");
        }

        if (!infoPlistDoc.root.values.ContainsKey("NSLocationUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSLocationUsageDescription", "APP需要你的允许，才能访问位置权限");
        }

        if (!infoPlistDoc.root.values.ContainsKey("NSLocationAlwaysUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSLocationAlwaysUsageDescription", "地理位置权限");
        }

        if (!infoPlistDoc.root.values.ContainsKey("NSMicrophoneUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSMicrophoneUsageDescription", "APP需要你的允许，才能访问麦克风");
        }

        // 穿山甲广告
        if (!infoPlistDoc.root.values.ContainsKey("NSMotionUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSMotionUsageDescription",
                "This app needs to be able to access your motion use");
        }

        if (!infoPlistDoc.root.values.ContainsKey("NSUserTrackingUsageDescription"))
        {
            infoPlistDoc.root.SetString("NSUserTrackingUsageDescription", "该标识符将用于向您投放个性化广告");
        }

        if (!infoPlistDoc.root.values.ContainsKey("SKAdNetworkIdentifier"))
        {
            infoPlistDoc.root.SetString("SKAdNetworkIdentifier", "238da6jt44.skadnetwork");
        }

        if (!infoPlistDoc.root.values.ContainsKey("SKAdNetworkIdentifier"))
        {
            infoPlistDoc.root.SetString("SKAdNetworkIdentifier", "x2jnk7ly8j.skadnetwork");
        }

        if (!infoPlistDoc.root.values.ContainsKey("SKAdNetworkIdentifier"))
        {
            infoPlistDoc.root.SetString("SKAdNetworkIdentifier", "22mmun2rn5.skadnetwork");
        }

        infoPlistDoc.WriteToFile(infoPlistPath);

        // 添加代码
        // // 获取UnityAppController.mm文件路径
        // string unityAppControllerPath = Path.Combine(targetPath, "Classes/UnityAppController.mm");
        // // 获取UnityAppController.mm文件内容
        // string unityAppControllerContent = File.ReadAllText(unityAppControllerPath);
        //
        // // 添加代码到UnityAppController.mm文件
        // string codeToAddH = @"#import <AppTrackingTransparency/AppTrackingTransparency.h>\n";
        // // 在UnityAppController.mm文件中添加代码
        //
        // unityAppControllerContent = unityAppControllerContent.Insert(0, codeToAddH);
        //
        // // 定位到applicationWillResignActive方法的位置
        // string methodSignature = "- (void)applicationWillResignActive:(UIApplication*)application\n{";
        // int methodStartIndex = unityAppControllerContent.IndexOf(methodSignature);
        //
        // if (methodStartIndex != -1)
        // {
        //     // 在applicationWillResignActive方法中添加代码
        //     string codeToAdd = @"
        //         if (@available(iOS 14, *)) {
        //             [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
        //         
        //             }];
        //         }
        //         ";
        //     unityAppControllerContent =
        //         unityAppControllerContent.Insert(methodStartIndex + methodSignature.Length + 1, codeToAdd);
        //
        //     // 将修改后的内容写回到UnityAppController.mm文件
        //     File.WriteAllText(unityAppControllerPath, unityAppControllerContent);
        //}
    }
}


