using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 构建用文件工具：清空、拷贝、写 version.txt。
/// 注释尽量写清「为什么」，方便对照学习热更目录布局。
/// </summary>
public static class FileOps
{
    /// <summary>创建目录；若已存在则清空其内容（保留目录本身）。</summary>
    public static void CreateOrClearDirectory(string dir)
    {
        if (string.IsNullOrEmpty(dir))
            return;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            return;
        }

        foreach (string file in Directory.GetFiles(dir))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }
        foreach (string sub in Directory.GetDirectories(dir))
        {
            Directory.Delete(sub, true);
        }
    }

    public static void EnsureDirectory(string dir)
    {
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    /// <summary>递归拷贝目录内容（跳过 .meta，避免把源工程 meta 带进 StreamingAssets/IIS）。</summary>
    public static void CopyDirectory(string source, string dest, bool clearDest = false)
    {
        if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
        {
            Debug.LogError($"[FileOps] 源目录不存在: {source}");
            return;
        }

        if (clearDest)
            CreateOrClearDirectory(dest);
        else
            EnsureDirectory(dest);

        foreach (string file in Directory.GetFiles(source))
        {
            if (file.EndsWith(".meta"))
                continue;
            string name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(dest, name), true);
        }

        foreach (string sub in Directory.GetDirectories(source))
        {
            string name = Path.GetFileName(sub);
            if (name == ".idea")
                continue;
            CopyDirectory(sub, Path.Combine(dest, name), false);
        }
    }

    /// <summary>
    /// 写入 version.txt，格式与 GameConfig.ReadVersionByTxt 一致：version=N
    /// </summary>
    public static void WriteVersionFile(string filePath, string version)
    {
        EnsureDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, $"version={version}", Encoding.UTF8);
        Debug.Log($"[FileOps] 写入版本文件: {filePath} => version={version}");
    }

    public static void RefreshAssets()
    {
        AssetDatabase.Refresh();
    }
}
