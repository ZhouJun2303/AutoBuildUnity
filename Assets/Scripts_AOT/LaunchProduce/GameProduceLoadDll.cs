using BM;
using ET;
using HybridCLR;
using Scripts_AOT.Utility;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
        await AssetComponent.Initialize("DllBundle");
        await LoadAotMetadata();
        await LoadHotDll();
        dependenceFsm.SetState(GameProcedureState.StartGame);
    }
    private async ETTask LoadAotMetadata()
    {
        foreach (var aotDllName in MetadataConfig.AotAssemblyMetadatas)
        {
            string finalName = MetadataConfig.GetStripMetadataName(aotDllName);
            string path = Path.Combine("Assets/HotDll/AOTAssemblyMetadataDlls", finalName + ".bytes");
            LogHelper.Log("LoadAotMetadata：" + path);
            var asset = await AssetComponent.LoadAsync<TextAsset>(path, "DllBundle");
            if (asset == null)
            {
                Debug.LogError($"{stateID}: cannot find AOTdll path: {path}");
                continue;
            }
            byte[] dllBytes = asset.bytes;
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.Consistent);
            if (err == HybridCLR.LoadImageErrorCode.OK)
                Debug.Log($"{stateID}: LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
            else
                Debug.LogError($"{stateID}: LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
        }
    }

    private async ETTask LoadHotDll()
    {
        string LaunchDllFileName = "Game.dll.bytes";
        string launchDllPath = Path.Combine("Assets/HotDll/HotUpdateDlls", LaunchDllFileName);
        LogHelper.Log("launchDllPath：" + launchDllPath);
        var dllBytes = await AssetComponent.LoadAsync<TextAsset>(launchDllPath, "DllBundle");
        if (dllBytes == null)
        {
            Debug.LogError("Error 加载热更dll失败：" + launchDllPath);
            return;
        }

#if UNITY_EDITOR
#else
        Assembly gameAss = System.Reflection.Assembly.Load(dllBytes.bytes);
#endif
    }
}
