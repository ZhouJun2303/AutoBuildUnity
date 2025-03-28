using BM;
using ET;
using Scripts_AOT.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


public class GameProduceUpdateAssetBundle : GameProduceBase<GameProcedureState>
{
    private UpdateBundleDataInfo _updateBundleDataInfo;
    public GameProduceUpdateAssetBundle(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        UpdateAssetBundle().Coroutine();
    }

    public override void OnProcedureUpdate()
    {
        base.OnProcedureUpdate();
        AssetComponent.Update();
    }

    public override void OnProcedureLeave()
    {
        LaunchAOT.Config.OverwriteServerVersionToPersisentVersion();
        base.OnProcedureLeave();
    }

    private async ETTask UpdateAssetBundle()
    {
        AssetComponentConfig.DefaultBundlePackageName = "AllBundle";
        AssetComponentConfig.BundleServerUrl = Path.Combine(LaunchAOT.Config.RemotePath, LaunchAOT.Config.ServerVersion.ToString(), "AssetBundles");
        Dictionary<string, bool> updatePackageBundle = new Dictionary<string, bool>()
        {
            {AssetComponentConfig.DefaultBundlePackageName, false},
        };
        _updateBundleDataInfo = await AssetComponent.CheckAllBundlePackageUpdate(updatePackageBundle);
        if (!_updateBundleDataInfo.NeedUpdate)
        {
            dependenceFsm.SetState(GameProcedureState.LoadDll);
            LogHelper.Log("assetBundle 不需要更新");
            return;
        }
        LogHelper.LogError("需要更新, 大小: " + _updateBundleDataInfo.NeedUpdateSize);
        _updateBundleDataInfo.DownLoadFinishCallback += () =>
        {
            LogHelper.Log("资源热更完成 !");
            dependenceFsm.SetState(GameProcedureState.LoadDll);
        };
        _updateBundleDataInfo.ProgressCallback += p =>
        {
            LogHelper.Log(p.ToString("#0.00") + "%");
        };
        _updateBundleDataInfo.DownLoadSpeedCallback += s =>
        {
            LogHelper.Log((s / 1024.0f).ToString("#0.00") + " kb/s");
        };
        _updateBundleDataInfo.ErrorCancelCallback += () =>
        {
            LogHelper.LogError("下载取消");
        };

        AssetComponent.DownLoadUpdate(_updateBundleDataInfo).Coroutine();
    }
}
