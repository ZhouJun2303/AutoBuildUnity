using ET;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Scripts_AOT.Utility;
using System;
using HybridCLR;
using BM;
using UnityEngine.SceneManagement;

public class LaunchAOT : MonoBehaviour
{
    //服务器地址
    public const string RemotePath = "http://192.168.11.230:9998";
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
    public string Hot_Update_Dlls_Dir;
    //AOT补充元数据 dll存放目录
    public string AOT_Assembly_Metadata_Dlls_Dir;
    //资源更新目录
    public string HotfixPath;

    void Start()
    {
        Hot_Update_Dlls_Dir = Path.Combine(PersistentDataPath, "HotUpdateDlls/");
        AOT_Assembly_Metadata_Dlls_Dir = Path.Combine(PersistentDataPath, "AOTAssemblyMetadataDlls/");
        HotfixPath = Path.Combine(PersistentDataPath, "AssetBundles");
#if UNITY_EDITOR
        LoadScene().Coroutine();
        return;
#endif
        Init().Coroutine();

        //test
        Test11 t1 = new Test11();
        Test22 t12 = new Test22();
        Test33 t13= new Test33();
    }

    private async ETTask Init()
    {
        //热更dll
        await UpdateHotDll();
        //热更补充元数据
        await UpdateAotMetadata();
        //热更资源文件
        await UpdateAssetBundle();
        //加载补充元数据
        await LoadAotMetadata();
        //加载脚本Dll
        await LoadHotDll();
        //启动初始场景
        await LoadScene();
    }

    private async ETTask UpdateHotDll()
    {
        string fileName = "Game.dll.bytes";
        string url = Path.Combine(RemotePath, "HotUpdateDlls", fileName);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            await request.SendWebRequest();
            if (request.isDone)
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"请求错误 {url}");
                }
                else
                {
                    string localFilePath = Path.Combine(Hot_Update_Dlls_Dir, fileName);
                    byte[] data = request.downloadHandler.data;
                    LogHelper.Log("update the dll：" + localFilePath);
                    long beginTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    FileHelper.FileClearWrite(localFilePath, data);
                    LogHelper.Log($"{fileName} Write IO消耗：{((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - beginTime)}ms");
                }
            }
        }

        await new WaitForSeconds(2);
    }

    private async ETTask UpdateAotMetadata()
    {
        foreach (var item in MetadataConfig.AotAssemblyMetadatas)
        {
            string url = Path.Combine(RemotePath, "AOTAssemblyMetadataDlls", item);
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();
                if (request.isDone)
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"请求错误 {url}");
                    }
                    else
                    {
                        string localFilePath = Path.Combine(AOT_Assembly_Metadata_Dlls_Dir, item);
                        byte[] data = request.downloadHandler.data;
                        LogHelper.Log("update the Metadata ：" + localFilePath);
                        long beginTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        FileHelper.FileClearWrite(localFilePath, data);
                        LogHelper.Log($"{item} Write IO消耗：{((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - beginTime)}ms");
                    }
                }
            }
        }
        await new WaitForSeconds(2);
    }

    private async ETTask UpdateAssetBundle()
    {

    }

    private async ETTask LoadAotMetadata()
    {
        foreach (var aotDllName in MetadataConfig.AotAssemblyMetadatas)
        {
            string path = GetLocalPath(Path.Combine(AOT_Assembly_Metadata_Dlls_Dir, aotDllName), true);
            UnityWebRequest request = UnityWebRequest.Get(path);
            request.timeout = 5;
            await request.SendWebRequest();
            if (request.isDone)
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogHelper.LogError("load MetadataForAOTAssembly Error: " + request.error + " url = " + path);
                }
                else
                {
                    byte[] dllBytes = request.downloadHandler.data;
                    LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
                    if (err != LoadImageErrorCode.OK)
                    {
                        LogHelper.Log($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
                    }
                }
            }
            await new WaitForEndOfFrame();
        }
    }

    private async ETTask LoadHotDll()
    {
        string LaunchDllFileName = "Game.dll.bytes";
        string launchDllPath = GetLocalPath(Path.Combine(Hot_Update_Dlls_Dir, LaunchDllFileName), true);
        LogHelper.Log("launchDllPath：" + launchDllPath);
        UnityWebRequest request = UnityWebRequest.Get(launchDllPath);
        request.timeout = 5;
        long beginTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        await request.SendWebRequest();
        if (request.isDone)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                LogHelper.LogError("No Finded LaunchDll：" + launchDllPath);
            }
            else
            {
                byte[] data = request.downloadHandler.data;
                LogHelper.Log($"热更dll 读取 IO消耗：{((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - beginTime)}ms");

                //加密代码 解密运行
                beginTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                LogHelper.Log($"代码解密耗时：{((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - beginTime)}ms");
                Assembly.Load(data);
            }
        }

    }


    private async ETTask LoadScene()
    {
        AssetComponentConfig.DefaultBundlePackageName = "AllBundle";
        long beginTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        await AssetComponent.Initialize(AssetComponentConfig.DefaultBundlePackageName);
        //运行场景
        string SceneResPath = "Assets/Scenes_HotFix/ToolScene.unity";
        //SceneCtrl.Instance.LoadSceneAsync(SceneResPath, LoadSceneMode.Additive, () =>
        LoadSceneAsync(SceneResPath, LoadSceneMode.Additive, () =>  //SceneCtrl需要场景中有Root节点才能使用，加载初始场景用单独新加的接口
        {
        });
        void LoadSceneAsync(string sceneName, LoadSceneMode mode, Action completed)
        {
            Inner().Coroutine();
            async ETTask Inner()
            {
                LoadSceneHandler loadSceneHandler = await AssetComponent.LoadSceneAsync(sceneName);
                AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, mode);
                asyncOperation.completed += (AsyncOperation obj) =>
                {
                    if (null != completed)
                        completed();
                };
            }
        }
    }




    public static string GetLocalPath(string path, bool isWebLoad)
    {
        string localPath = Path.Combine(Application.persistentDataPath, path);
        LogHelper.Log(localPath + " " + File.Exists(localPath));
        //热更目录存在，返回热更目录
        if (!File.Exists(localPath))
        {
            //热更目录不存在，返回streaming目录
            localPath = Path.Combine(Application.streamingAssetsPath, path);
        }
        if (isWebLoad)
        {
            //通过webReq加载
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
        else
            return localPath;
    }
}
