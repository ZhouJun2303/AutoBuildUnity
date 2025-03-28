using BM;
using ET;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameProduceStartGame : GameProduceBase<GameProcedureState>
{
    public GameProduceStartGame(FSM<GameProcedureState> fsm, GameProcedureState state) : base(fsm, state)
    {


    }

    public override void OnProcedureEnter()
    {
        base.OnProcedureEnter();
        Init().Coroutine();
    }

    private async ETTask Init()
    {
        AssetComponentConfig.DefaultBundlePackageName = "AllBundle";
        await AssetComponent.Initialize(AssetComponentConfig.DefaultBundlePackageName);
        LoadScene();
    }

    private void LoadScene()
    {
        string SceneResPath = "Assets/Scenes_HotFix/ToolScene.unity";
        LoadSceneAsync(SceneResPath, LoadSceneMode.Additive, () =>  //SceneCtrl需要场景中有Root节点才能使用，加载初始场景用单独新加的接口
        {
        });
        void LoadSceneAsync(string sceneName, LoadSceneMode mode, Action completed)
        {
            Inner().Coroutine();
            async ETTask Inner()
            {
                LoadSceneHandler loadSceneHandler = await AssetComponent.LoadSceneAsync(sceneName);
                AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, mode);
                asyncOperation.completed += (AsyncOperation obj) =>
                {
                    if (null != completed)
                        completed();
                };
            }
        }
    }

}
