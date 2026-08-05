using UnityEditor;
using UnityEngine;

/// <summary>导出 Android/iOS 工程及后处理拷贝（委托现有 BuildProject / OnPostprocess）。</summary>
public static class PlayerExportSteps
{
    public static void ExportAndroid()
    {
        BuildProject.BuildAndroidProject();
    }

    public static void ExportIos()
    {
        BuildProject.BuildXcodeProject1();
    }

    public static void CopyAndroidRes()
    {
        OnPostprocessBuild_Android.CopyUnityRes();
    }

    public static void CopyIosRes()
    {
        OnPostprocessBuild_IOS.CopyUnityRes();
    }
}
