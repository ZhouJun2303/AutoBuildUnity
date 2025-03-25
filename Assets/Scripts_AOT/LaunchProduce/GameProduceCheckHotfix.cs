using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameProduceCheckHotfix : GameProduceBase<GameProcedureState>
{
    public GameProduceCheckHotfix(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        dependenceFsm.SetState(GameProcedureState.UpdateHotDll);
    }
}
