using ET;
using Scripts_AOT.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


public class GameProduceUpdateHotDll : GameProduceBase<GameProcedureState>
{
    public GameProduceUpdateHotDll(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {
    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        UpdateHotDll().Coroutine();
    }

    private async ETTask UpdateHotDll()
    {
        string fileName = "Game.dll.bytes";
        string url = Path.Combine(LaunchAOT.Config.RemotePath, "HotUpdateDlls", fileName);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            await request.SendWebRequest();
            if (request.isDone)
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"请求错误 {url}");
                }
                else
                {
                    string localFilePath = Path.Combine(LaunchAOT.Config.Hot_Update_Dlls_Dir, fileName);
                    byte[] data = request.downloadHandler.data;
                    LogHelper.Log("update the dll：" + localFilePath);
                    long beginTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    FileHelper.FileClearWrite(localFilePath, data);
                    LogHelper.Log($"{fileName} Write IO消耗：{((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - beginTime)}ms");
                }
            }
        }
        dependenceFsm.SetState(GameProcedureState.UpdateAotMetadata);
    }
}
