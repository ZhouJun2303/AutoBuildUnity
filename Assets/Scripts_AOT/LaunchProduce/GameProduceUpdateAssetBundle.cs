using ET;
using Game.AssetCore;
using Scripts_AOT.Utility;
using System;
using System.Collections.Generic;

public class GameProduceUpdateAssetBundle : GameProduceBase<GameProcedureState>
{
    private IUpdateInfo _updateInfo;
    private bool _updateCompleted;
    private bool _persistServerVersion;

    public GameProduceUpdateAssetBundle(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        _updateCompleted = false;
        _persistServerVersion = false;
        UpdateAssetBundle().Coroutine();
    }

    public override void OnProcedureLeave()
    {
        if (_updateCompleted && _persistServerVersion)
            LaunchAOT.Config.OverwriteServerVersionToPersisentVersion();
        base.OnProcedureLeave();
    }

    private async ETTask UpdateAssetBundle()
    {
        if (!AssetService.SupportsHotUpdate)
        {
            LogHelper.Log($"[{AssetService.Backend}] 不支持当前运行模式下的热更，跳过资源更新");
            _updateCompleted = true;
            dependenceFsm.SetState(GameProcedureState.LoadDll);
            return;
        }

        if (AssetService.Backend == AssetBackendType.BundleMaster ||
            AssetService.Backend == AssetBackendType.YooAsset)
        {
            // RemotePath 已在 LaunchAOT 中按后端设为：
            //   http://192.168.18.62:8866/BundleMaster  或  .../YooAsset
            // version.txt → {RemotePath}/version.txt
            // 资源目录   → {RemotePath}/{ver}/AssetBundles  （磁盘 C:\IIS_ServerData\{Backend}\{ver}\AssetBundles）
            await LaunchAOT.Config.GetAllVersion();
            if (LaunchAOT.Config.ServerVersion < 0)
                throw new InvalidOperationException(
                    $"资源版本服务器不可用：{AssetBackendRemotePaths.GetVersionFileUrl(AssetService.Backend)}");

            AssetService.Options.BundleServerUrl = AssetBackendRemotePaths.GetBundleServerUrl(
                AssetService.Backend,
                LaunchAOT.Config.ServerVersion);
            LogHelper.Log($"[{AssetService.Backend}] BundleServerUrl={AssetService.Options.BundleServerUrl}");
            _persistServerVersion = true;
        }

        Dictionary<string, bool> updatePackageBundle = new Dictionary<string, bool>()
        {
            { AssetService.Options.DefaultPackageName, false },
        };
        updatePackageBundle["DllBundle"] = false;

        _updateInfo = await AssetService.CheckUpdateAsync(updatePackageBundle);
        if (_updateInfo == null)
            throw new InvalidOperationException($"[{AssetService.Backend}] CheckUpdateAsync returned null");
        if (!_updateInfo.NeedUpdate)
        {
            _updateCompleted = true;
            dependenceFsm.SetState(GameProcedureState.LoadDll);
            LogHelper.Log("assetBundle 不需要更新");
            return;
        }

        LogHelper.Log("需要更新, 大小: " + _updateInfo.TotalBytes);
        var progress = new ProgressReporter();
        await AssetService.DownloadUpdateAsync(_updateInfo, progress);
        LogHelper.Log("资源热更完成 !");
        _updateCompleted = true;
        dependenceFsm.SetState(GameProcedureState.LoadDll);
    }

    private sealed class ProgressReporter : System.IProgress<UpdateProgress>
    {
        public void Report(UpdateProgress value)
        {
            LogHelper.Log(value.Percent.ToString("#0.00") + "%");
            if (value.SpeedBytesPerSec > 0)
                LogHelper.Log((value.SpeedBytesPerSec / 1024.0f).ToString("#0.00") + " kb/s");
        }
    }
}
