using Scripts_AOT.Utility;
using System.IO;
using UnityEngine;

/// <summary>
/// 游戏流程阶段
/// </summary>
public enum GameProcedureState
{
    Launch = 1,     // 开始
    UpdateAssetBundle,   // 热更资源文件
    LoadDll,    //加载Dll
    StartGame,      // 开始游戏
}

public class GameProduceBase<T> : FsmState<T> where T : System.Enum
{
    protected FSM<T> dependenceFsm;
    public GameProduceBase(FSM<T> fsm, T state) : base(state)
    {
        dependenceFsm = fsm;
        this.OnEnter(OnProcedureEnter)
                .OnUpdate(OnProcedureUpdate)
                .OnLeave(OnProcedureLeave);
    }

    public virtual void OnProcedureEnter() { LogHelper.Log("GameProcedure OnEnter: " + stateID); }

    public virtual void OnProcedureUpdate() { }

    public virtual void OnProcedureLeave() { }

}
