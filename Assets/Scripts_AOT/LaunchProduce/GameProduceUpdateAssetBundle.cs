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
    public GameProduceUpdateAssetBundle(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        UpdateAssetBundle().Coroutine();
    }

    private async ETTask UpdateAssetBundle()
    {
        dependenceFsm.SetState(GameProcedureState.LoadDll);
    }
}
