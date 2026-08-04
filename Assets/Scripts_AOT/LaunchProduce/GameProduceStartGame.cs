using ET;
using Game.AssetCore;
using System;
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
        bool initialized = await AssetService.InitializePackageAsync();
        if (!initialized)
            throw new InvalidOperationException(
                $"[{AssetService.Backend}] 默认资源包初始化失败: {AssetService.Options.DefaultPackageName}");

        ISceneHandle scene = await AssetService.LoadSceneAsync("Assets/Scenes_HotFix/ToolScene.unity", null,
            LoadSceneMode.Additive);
        if (scene == null || !scene.Succeeded)
            throw new InvalidOperationException(scene?.Error ?? "启动场景加载失败");
    }
}
