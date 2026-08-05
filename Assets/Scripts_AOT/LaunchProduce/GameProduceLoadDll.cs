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
        // SuperSet：允许补充元数据为 AOT 的超集，对裁剪版本轻微不一致更宽容。
        // 仍建议 Strip 与当前 Player 同源生成；跨大版本混用仍可能失败。
        const HomologousImageMode mode = HomologousImageMode.SuperSet;

        foreach (var aotDllName in MetadataConfig.AotAssemblyMetadatas)
        {
            string finalName = MetadataConfig.GetStripMetadataName(aotDllName);
            string path = Path.Combine("Assets/HotDll/AOTAssemblyMetadataDlls", finalName + ".bytes");
            LogHelper.Log($"LoadAotMetadata：{path} mode={mode}");
            var asset = await AssetService.LoadAsync<TextAsset>(path, "DllBundle");
            if (asset == null)
                throw new FileNotFoundException($"{stateID}: cannot find AOT dll", path);

            byte[] dllBytes = asset.bytes;
            if (dllBytes == null || dllBytes.Length == 0)
                throw new InvalidOperationException(
                    $"{stateID}: AOT metadata empty: {aotDllName}, path={path}");

            try
            {
                var err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                if (err == LoadImageErrorCode.OK)
                {
                    Debug.Log(
                        $"{stateID}: LoadMetadataForAOTAssembly OK: {aotDllName}, " +
                        $"bytes={dllBytes.Length}, mode={mode}");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"{stateID}: LoadMetadataForAOTAssembly failed: {aotDllName}, " +
                        $"ret={err}, bytes={dllBytes.Length}, mode={mode}, path={path}");
                }
            }
            catch (Exception e) when (!(e is InvalidOperationException))
            {
                // Consistent/SuperSet 校验失败时 native 可能直接抛 ExecutionEngineException
                throw new InvalidOperationException(
                    $"{stateID}: LoadMetadataForAOTAssembly exception: {aotDllName}, " +
                    $"bytes={dllBytes.Length}, mode={mode}, path={path}", e);
            }
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
