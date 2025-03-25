using ET;
using HybridCLR;
using Scripts_AOT.Utility;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

public class GameProduceLoadDll : GameProduceBase<GameProcedureState>
{
    public GameProduceLoadDll(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        LoadDll().Coroutine();
    }

    private async ETTask LoadDll()
    {
        await LoadAotMetadata();
        await LoadDll();

        dependenceFsm.SetState(GameProcedureState.StartGame);
    }
    private async ETTask LoadAotMetadata()
    {
        foreach (var aotDllName in MetadataConfig.AotAssemblyMetadatas)
        {
            string finalName = MetadataConfig.GetStripMetadataName(aotDllName);
            string path = GetLocalPath(Path.Combine(LaunchAOT.Config.AOT_Assembly_Metadata_Dlls_Dir, finalName), true);
            LogHelper.Log("LoadAotMetadata：" + path);
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
                        LogHelper.Log($"LoadMetadataForAOTAssembly:{finalName}. ret:{err}");
                    }
                }
            }
            await new WaitForEndOfFrame();
        }
    }

    private async ETTask LoadHotDll()
    {
        string LaunchDllFileName = "Game.dll.bytes";
        string launchDllPath = GetLocalPath(Path.Combine(LaunchAOT.Config.Hot_Update_Dlls_Dir, LaunchDllFileName), true);
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
}
