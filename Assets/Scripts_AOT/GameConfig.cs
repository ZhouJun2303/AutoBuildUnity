using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using Scripts_AOT.Utility;
using System.Text;
using ET;
using UnityEngine.Networking;

public class GameConfig
{
    /// <summary>
    /// 当前后端的远端根 URL（含 AB 子目录，无末尾斜杠）。
    /// 默认 BundleMaster；Launch 启动时会按 AssetBackendConfig.Backend 调用 ApplyBackendRemote 覆盖。
    /// 例：http://192.168.18.62:8866/BundleMaster
    /// </summary>
    public string RemotePath { get; private set; } = Game.AssetCore.AssetBackendRemotePaths.GetRemoteUrl(Game.AssetCore.AssetBackendType.BundleMaster);

    /// <summary>IIS HTTP 总根，不含后端子目录。</summary>
    public string RemoteBaseUrl { get; private set; } = Game.AssetCore.AssetBackendRemotePaths.RemoteBaseUrl;
    public string PersistentDataPath
    {
        get
        {
#if UNITY_EDITOR
            return Application.streamingAssetsPath;
#endif
            return Application.persistentDataPath;
        }
    }
    //代码热更 dll存放目录
    public string HotDllPath { get; private set; }
    //代码热更 AOT补充元数据 dll存放目录
    public string HotMetaDataDllPath { get; private set; }
    //资源热更 资源更新目录
    public string AssetHofixPath { get; private set; }
    public string VersionFileName { get; private set; } = "version.txt";

    public string GetPathByPlatform(string localPath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!localPath.Contains("file:///"))
                {
                    localPath = "file://" + localPath;
                }
#elif UNITY_IOS && !UNITY_EDITOR
                localPath = "file://" + localPath;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                localPath = "file://" + localPath;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#else
#endif
        return localPath;
    }

    public GameConfig()
    {
        HotDllPath = Path.Combine(PersistentDataPath, "HotUpdateDlls/");
        HotMetaDataDllPath = Path.Combine(PersistentDataPath, "AOTAssemblyMetadataDlls/");
        AssetHofixPath = Path.Combine(PersistentDataPath, "AssetBundles/");
        LogHelper.Log($"HotDllPath {HotDllPath}");
        LogHelper.Log($"HotMetaDataDllPath {HotMetaDataDllPath}");
        LogHelper.Log($"AssetHofixPath {AssetHofixPath}");
        LogHelper.Log($"RemotePath(default) {RemotePath}");
    }

    /// <summary>
    /// 按资源后端切换远端根路径，保证请求地址与 IIS 子目录一致。
    /// BundleMaster → http://192.168.18.62:8866/BundleMaster
    /// YooAsset     → http://192.168.18.62:8866/YooAsset
    /// Addressables → http://192.168.18.62:8866/Addressables
    /// </summary>
    public void ApplyBackendRemote(Game.AssetCore.AssetBackendType backend)
    {
        RemotePath = Game.AssetCore.AssetBackendRemotePaths.GetRemoteUrl(backend);
        LogHelper.Log($"[GameConfig] RemotePath => {RemotePath} (backend={backend})");
    }

    public int ServerVersion { get; private set; } = -1;

    public int StreamVersion { get; private set; } = -1;

    public int PersisentVersion { get; private set; } = -1;

    /// <summary>
    /// 服务器，本地，持久化
    /// </summary>
    /// <returns></returns>
    public async ETTask<(int, int, int)> GetAllVersion()
    {
        //服务器最新热更版本
        ServerVersion = await GetVersionByFileName($"{LaunchAOT.Config.RemotePath}/{LaunchAOT.Config.VersionFileName}");
        PersisentVersion = await GetVersionByFileName(GetPathByPlatform(Path.Combine(Application.persistentDataPath, LaunchAOT.Config.VersionFileName)));
        StreamVersion = await GetVersionByFileName(GetPathByPlatform(Path.Combine(Application.streamingAssetsPath, LaunchAOT.Config.VersionFileName)));
        return (ServerVersion, PersisentVersion, StreamVersion);
    }

    private async ETTask<int> GetVersionByFileName(string str)
    {
        UnityWebRequest request = UnityWebRequest.Get(str);
        request.timeout = 5;
        await request.SendWebRequest();
        if (request.isDone)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                LogHelper.LogError("No Finded VersionFileName：" + str);
            }
            else
            {
                string reuslt = request.downloadHandler.text;
                return ReadVersionByTxt(reuslt);
            }
        }
        return -1;
    }

    public void OverwriteServerVersionToPersisentVersion()
    {
        string persistentFilePath = Path.Combine(Application.persistentDataPath, LaunchAOT.Config.VersionFileName);
        File.WriteAllText(persistentFilePath, $"version={ServerVersion}", Encoding.UTF8);
        PersisentVersion = ServerVersion;
    }

    public bool NeedUpdate()
    {
        int curMaxversion = -1;
        curMaxversion = StreamVersion;
        if (PersisentVersion >= StreamVersion)
        {
            curMaxversion = PersisentVersion;
        }
        return curMaxversion < ServerVersion;
    }

    private int ReadVersion(string filePath)
    {
        int version = -1;
        Debug.Log("ReadVersion " + filePath);
        // 文件不存在时自动创建并写入默认值
        if (!File.Exists(filePath))
        {
            Debug.LogError("ReadVersion " + filePath + " 不存在！");
            return version;
        }
        // 读取全部内容
        string content = File.ReadAllText(filePath, Encoding.UTF8);
        // 解析版本号（示例格式：version=1.2.3）
        int.TryParse(content.Split('=')[1].Trim(), out version);
        return version;
    }

    private int ReadVersionByTxt(string content)
    {
        int version = -1;
        int.TryParse(content.Split('=')[1].Trim(), out version);
        return version;
    }
}