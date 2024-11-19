using UnityEngine;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class XcodeProjectHelper
{
    // 获取 Xcode 项目的路径
    public static string GetXcodeProjectPath(string buildPath)
    {
        return Path.Combine(buildPath, "Unity-iPhone.xcodeproj");
    }

    // 读取 Xcode 项目
    public static PBXProject LoadXcodeProject(string projectPath)
    {
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);
        return project;
    }

    // 保存 Xcode 项目
    public static void SaveXcodeProject(PBXProject project, string projectPath)
    {
        project.WriteToFile(projectPath);
        Debug.Log("Xcode project saved.");
    }

    // 获取 Unity-iPhone target GUID
    public static string GetUnityIphoneTarget(PBXProject project)
    {
        return project.TargetGuidByName("Unity-iPhone");
    }

    // 设置构建属性（例如：GCC_WARN_INHIBIT_ALL_WARNINGS）
    public static void SetBuildProperty(PBXProject project, string targetGuid, string key, string value)
    {
        project.SetBuildProperty(targetGuid, key, value);
    }

    // 向 Xcode 项目中添加一个文件
    public static void AddFileToXcodeProject(PBXProject project, string filePath, string targetGuid)
    {
        // 获取相对路径
        string fileName = Path.GetFileName(filePath);
        string relativePath = "Assets/" + fileName;

        // 将文件添加到项目中
        string fileGuid = project.AddFile(filePath, relativePath, PBXSourceTree.Source);

        // 将文件添加到目标构建中
        project.AddFileToBuild(targetGuid, fileGuid);
        Debug.Log($"File '{fileName}' added to Xcode project.");
    }

    // 向 Xcode 项目中添加一个框架
    public static void AddFrameworkToXcodeProject(PBXProject project, string targetGuid, string frameworkName)
    {
        project.AddFrameworkToProject(targetGuid, frameworkName, false);
        Debug.Log($"Framework '{frameworkName}' added to Xcode project.");
    }

    // 向 Xcode 项目添加一个自定义编译标志
    public static void AddCustomBuildFlag(PBXProject project, string targetGuid, string flag)
    {
        // string existingFlags = project.GetBuildProperty(targetGuid, "OTHER_CFLAGS");
        // string newFlags = existingFlags + " " + flag;
        // project.SetBuildProperty(targetGuid, "OTHER_CFLAGS", newFlags);
        // Debug.Log($"Custom build flag '{flag}' added to Xcode project.");
    }
}
