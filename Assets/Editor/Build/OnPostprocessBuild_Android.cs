using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System;

/*
 * 打包后将打包出来的资源拷贝到目标安卓项目中
 */
public class OnPostprocessBuild_Android : MonoBehaviour
{
    private static string _pathSetting; //被拷贝
    private static string _pathExport; //拷到哪里去

    //Lib 路径Path
    private static string _copyLibsPathRoot = "/unityLibrary/libs";
    //Lib 路径Path
    private static string _copyUnityDataAssetPackPathRoot = "/UnityDataAssetPack";
    //平时打包资源文件
    private static string _copyPathRoot = "/unityLibrary/src/main/";
    //拷贝文件夹
    private static string[] _copyPathFolders = new string[] { "assets", "Il2CppOutputProject", "jniLibs", "jniStaticLibs" };
    //忽略删除文件夹
    private static string[] _ignoreDeleteFolderOrFile = new string[] { "hw-services.json", "test" };
    //拷贝前清空删除文件夹
    private static string[] _deleteFolder = new string[] {
        "assets/DllBundle",
        "assets/AllBundle",
        "assets/bin",
        "Il2CppOutputProject",
    };

    [PostProcessBuild(999)]
    public static void OnPostprocessBuild(BuildTarget BuildTarget, string path)
    {
        path = path.Replace(@"\", "/");
        if (BuildTarget != BuildTarget.Android) return;
        if (!Directory.Exists(path)) return;
        if (!BuildProjectWindows.GetAfterBuildCopyRes()) return;
        //清除
        CleanFolder();
        //拷贝打包的资源到安卓项目
        for (int i = 0; i < _copyPathFolders.Length; i++)
        {
            _pathSetting = path + _copyPathRoot + _copyPathFolders[i];
            _pathExport = BuildProjectWindows.GetAndroidOutPath() + _copyPathRoot + _copyPathFolders[i];
            EditorUtility.DisplayProgressBar("拷贝文件", _pathSetting, i * 1.0f / _copyPathFolders.Length * 1.0f);
            CopyDirectory(_pathSetting, _pathExport);
            Debug.Log(_pathExport + "        Complete!");
        }

        EditorUtility.ClearProgressBar();
        Debug.LogWarning("拷贝打包的资源到安卓项目完成！" + System.DateTime.Now);
        //自动拷贝库文件
        if (BuildProjectWindows.GetAfterBuildCopyLibRes()) CopyUnityLibRes();
        //拷贝分包文件 
        if (BuildProjectWindows.GetAfterBuildCopyUnityDataAssetPack()) CopyUnityDataAssetPack();
    }

    /// <summary>
    /// 拷贝Unity 资源文件
    /// </summary>
    public static void CopyUnityRes()
    {
        string path = BuildProject.GetFullProjectPath();
        OnPostprocessBuild(BuildTarget.Android, path);
    }

    /// <summary>
    /// 拷贝Unity 库文件
    /// </summary>
    public static void CopyUnityLibRes()
    {
        string path = BuildProject.GetFullProjectPath();
        if (!Directory.Exists(path)) return;
        EditorUtility.DisplayProgressBar("拷贝库文件", path, 1);
        _pathSetting = path + _copyLibsPathRoot;
        _pathExport = BuildProjectWindows.GetAndroidOutPath() + _copyLibsPathRoot;
        FileHelper.CopyDir(_pathSetting, _pathExport);
        EditorUtility.ClearProgressBar();
        Debug.LogWarning(_pathExport + "     Complete!");
    }

    /// <summary>
    /// 拷贝Unity 分包资源文件
    /// </summary>
    public static void CopyUnityDataAssetPack()
    {
        string path = BuildProject.GetFullProjectPath();
        if (!Directory.Exists(path)) return;
        EditorUtility.DisplayProgressBar("拷贝 UnityDataAssetPack文件", path, 1);
        _pathSetting = path + _copyUnityDataAssetPackPathRoot;
        _pathExport = BuildProjectWindows.GetAndroidOutPath() + _copyUnityDataAssetPackPathRoot;
        if (Directory.Exists(_pathExport))
        {
            Directory.Delete(_pathExport, true);
        }
        FileHelper.CopyDir(_pathSetting, _pathExport);
        EditorUtility.ClearProgressBar();
        Debug.LogWarning(_pathExport + "     Complete!");
    }



    public static void CleanFolder()
    {
        for (int i = 0; i < _deleteFolder.Length; i++)
        {
            string path = BuildProjectWindows.GetAndroidOutPath() + _copyPathRoot + _deleteFolder[i];
            if (!Directory.Exists(path)) continue;

            //删除文件
            DirectoryInfo direction = new DirectoryInfo(path);
            FileInfo[] files = direction.GetFiles("*");
            for (int j = 0; j < files.Length; j++)
            {
                bool delete = true;
                for (int k = 0; k < _ignoreDeleteFolderOrFile.Length; k++)
                {
                    if (files[j].FullName.EndsWith(_ignoreDeleteFolderOrFile[k]))
                    {
                        delete = false;
                        break;
                    }
                }

                if (delete)
                {
                    EditorUtility.DisplayProgressBar("删除文件", files[i].FullName, j * 1.0f / files.Length * 1.0f);
                    File.Delete("删除文件" + files[i].FullName);
                }
            }

            //删除文件夹
            foreach (string pathString in Directory.GetDirectories(path))
            {
                //删除文件夹
                bool delete = true;
                for (int k = 0; k < _ignoreDeleteFolderOrFile.Length; k++)
                {
                    if (pathString.EndsWith(_ignoreDeleteFolderOrFile[k]))
                    {
                        delete = false;
                        break;
                    }
                }
                if (delete)
                {
                    EditorUtility.DisplayProgressBar("删除文件夹", pathString, 1f);
                    Directory.Delete(pathString, true);
                }
            }
        }
    }

    static void Copy(string path)
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        FileSystemInfo[] files = dir.GetFileSystemInfos();
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i] is DirectoryInfo)
            {
                string sub = _pathExport + files[i].FullName.Replace(@"\", "/").Replace(_pathSetting, "");
                if (!Directory.Exists(sub))
                    Directory.CreateDirectory(sub);
                Copy(files[i].FullName);
            }
            else
            {
                string sub = _pathExport + files[i].FullName.Replace(@"\", "/").Replace(_pathSetting, "");
                if (!File.Exists(sub))
                    File.Create(sub);
                Debug.Log($"Copy [{files[i].FullName}] to [{sub}]");
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

    /// <summary>
    /// 拷贝符号表
    /// </summary>
    public static IEnumerator CopySymbols()
    {
        yield return new WaitForSeconds(5f);
        try
        { // 拷贝Symbols
            string origin = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "Temp/StagingArea/symbols/";
            var originDirInfo = new DirectoryInfo(origin);

            string target = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "CachedSymbols/" + DateTime.Now.ToString("yyyy_M_dd_hh_mm") + "/";
            var targetDirInfo = new DirectoryInfo(target);
            if (targetDirInfo.Exists == false)
            {
                targetDirInfo.Create();
            }

            //创建所有新目录
            foreach (string dirPath in Directory.GetDirectories(origin, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(origin, target));
            }
            //复制所有文件 & 保持文件名和路径一致
            foreach (string newPath in Directory.GetFiles(origin, "*.*", SearchOption.AllDirectories))
            {
                Debug.Log($"Copy symbol file from {newPath} -> {newPath.Replace(origin, target)}");
                File.Copy(newPath, newPath.Replace(origin, target), true);
            }

            //Directory.Move(originDirInfo.ToString(), targetDirInfo.ToString());
            Debug.Log($"Copy Symbol from {origin} to {target}");
        }
        catch (Exception err)
        {
            Debug.LogError(err);
        }
    }
}