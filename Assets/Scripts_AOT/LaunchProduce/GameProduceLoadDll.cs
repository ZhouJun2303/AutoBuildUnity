using ET;
using Game.AssetCore;
using HybridCLR;
using Scripts_AOT.Utility;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

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
        if (AssetService.Backend == AssetBackendType.Resources)
        {
            Debug.LogWarning($"{stateID}: Resources 后端不加载热更 DLL，跳过 LoadDll");
            dependenceFsm.SetState(GameProcedureState.StartGame);
            return;
        }

        bool initialized = await AssetService.InitializePackageAsync("DllBundle");
        if (!initialized)
            throw new InvalidOperationException($"{stateID}: DllBundle 初始化失败");
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
            var asset = await AssetService.LoadAsync<TextAsset>(path, "DllBundle");
            if (asset == null)
                throw new FileNotFoundException($"{stateID}: cannot find AOT dll", path);

            byte[] dllBytes = asset.bytes;
            var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.Consistent);
            if (err == HybridCLR.LoadImageErrorCode.OK)
                Debug.Log($"{stateID}: LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
            else
                throw new InvalidOperationException(
                    $"{stateID}: LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
        }
    }

    private async ETTask LoadHotDll()
    {
        string LaunchDllFileName = "Game.dll.bytes";
        string launchDllPath = Path.Combine("Assets/HotDll/HotUpdateDlls", LaunchDllFileName);
        LogHelper.Log("launchDllPath：" + launchDllPath);
        var dllBytes = await AssetService.LoadAsync<TextAsset>(launchDllPath, "DllBundle");
        if (dllBytes == null)
            throw new FileNotFoundException("加载热更 DLL 失败", launchDllPath);

#if UNITY_EDITOR
#else
        Assembly gameAss = System.Reflection.Assembly.Load(dllBytes.bytes);
#endif
    }
}
