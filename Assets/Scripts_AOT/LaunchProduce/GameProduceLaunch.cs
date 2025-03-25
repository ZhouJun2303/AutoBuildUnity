using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameProduceLaunch : GameProduceBase<GameProcedureState>
{
    public GameProduceLaunch(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        dependenceFsm.SetState(GameProcedureState.CheckHotfix);
    }
}
