using ET;
using Scripts_AOT.Utility;


public class GameProduceCheckHotfix : GameProduceBase<GameProcedureState>
{
    public GameProduceCheckHotfix(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        CheckNeedUpdate().Coroutine();

    }

    private async ETTask CheckNeedUpdate()
    {
        var (ServerVersion, PersisentVersion, StreamVersion) = await LaunchAOT.Config.GetAllVersion();

        LogHelper.Log($"serverVersion {ServerVersion} persisentVersion {PersisentVersion} streamVersion {StreamVersion}  ");
        if (LaunchAOT.Config.NeedUpdate())
        {
            dependenceFsm.SetState(GameProcedureState.UpdateHotDll);
        }
        else
        {
            LogHelper.Log("已是最新版本，直接进入游戏 ！");
            dependenceFsm.SetState(GameProcedureState.LoadDll);
        }
    }

}
