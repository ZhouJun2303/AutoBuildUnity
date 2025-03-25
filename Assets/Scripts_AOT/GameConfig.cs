using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using Scripts_AOT.Utility;

public class GameConfig
{
    //服务器地址
    public string RemotePath { get; private set; } = "http://192.168.18.62:9998";
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
    public string Hot_Update_Dlls_Dir { get; private set; }
    //AOT补充元数据 dll存放目录
    public string AOT_Assembly_Metadata_Dlls_Dir { get; private set; }
    //资源更新目录
    public string HotfixPath { get; private set; }

    public GameConfig()
    {
        Hot_Update_Dlls_Dir = Path.Combine(PersistentDataPath, "HotUpdateDlls/");
        AOT_Assembly_Metadata_Dlls_Dir = Path.Combine(PersistentDataPath, "AOTAssemblyMetadataDlls/");
        HotfixPath = Path.Combine(PersistentDataPath, "AssetBundles");
        LogHelper.Log($"Hot_Update_Dlls_Dir {Hot_Update_Dlls_Dir}");
        LogHelper.Log($"AOT_Assembly_Metadata_Dlls_Dir {AOT_Assembly_Metadata_Dlls_Dir}");
        LogHelper.Log($"HotfixPath {HotfixPath}");
    }
}