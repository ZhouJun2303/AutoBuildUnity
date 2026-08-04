using BM;
using ET;
using Game.AssetAdapters;
using Game.AssetCore;
using UnityEngine;

public class LaunchAOT : MonoBehaviour
{
    private FsmCtrl _fsmCtrl = new FsmCtrl();
    public static GameConfig Config = new GameConfig();
    public static AssetBackendConfig BackendConfig;

    private void Awake()
    {
        System.AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            AssetLogHelper.LogError(e.ExceptionObject.ToString());
        };
        ETTask.ExceptionHandler += AssetLogHelper.LogError;

        BackendConfig = AssetBackendConfig.LoadOrDefault();
        AssetService.Bootstrap(AssetLoaderFactory.Create(BackendConfig.Backend), BackendConfig.ToRuntimeOptions());
    }

    void Start()
    {
        StartProduce().Coroutine();
    }

    private async ETTask StartProduce()
    {
        await AssetService.InitializeAsync();

        var fsm = _fsmCtrl.CreateFSM<FSM<GameProcedureState>, GameProcedureState>("GameProduce");
        fsm.AddState(new GameProduceLaunch(fsm, GameProcedureState.Launch));
        fsm.AddState(new GameProduceUpdateAssetBundle(fsm, GameProcedureState.UpdateAssetBundle));
        fsm.AddState(new GameProduceLoadDll(fsm, GameProcedureState.LoadDll));
        fsm.AddState(new GameProduceStartGame(fsm, GameProcedureState.StartGame));

#if UNITY_EDITOR
        fsm.SetState(GameProcedureState.StartGame);
#else
        fsm.SetState(GameProcedureState.Launch);
#endif
    }

    private void Update()
    {
        AssetService.Tick();
        _fsmCtrl.OnUpdate();
    }
}
