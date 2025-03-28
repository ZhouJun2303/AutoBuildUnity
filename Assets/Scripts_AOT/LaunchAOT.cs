using BM;
using ET;
using UnityEngine;

public class LaunchAOT : MonoBehaviour
{
    private FsmCtrl _fsmCtrl = new FsmCtrl();
    public static GameConfig Config = new GameConfig();

    private void Awake()
    {
        System.AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            AssetLogHelper.LogError(e.ExceptionObject.ToString());
        };
        ETTask.ExceptionHandler += AssetLogHelper.LogError;
    }

    void Start()
    {
        var fsm = _fsmCtrl.CreateFSM<FSM<GameProcedureState>, GameProcedureState>("GameProduce");
        fsm.AddState(new GameProduceLaunch(fsm, GameProcedureState.Launch));
        fsm.AddState(new GameProduceCheckHotfix(fsm, GameProcedureState.CheckHotfix));
        fsm.AddState(new GameProduceUpdateHotDll(fsm, GameProcedureState.UpdateHotDll));
        fsm.AddState(new GameProduceUpdateAotMetadata(fsm, GameProcedureState.UpdateAotMetadata));
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
        _fsmCtrl.OnUpdate();
    }
}
