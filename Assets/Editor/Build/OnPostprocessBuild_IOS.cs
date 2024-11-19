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
        
        
    }
}


