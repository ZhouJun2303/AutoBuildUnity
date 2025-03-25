using Scripts_AOT.Utility;
using System.IO;
using UnityEngine;

/// <summary>
/// 游戏流程阶段
/// </summary>
public enum GameProcedureState
{
    Launch = 1,     // 开始
    CheckHotfix,    // 检查热更
    UpdateHotDll,   // 热更dll
    UpdateAotMetadata,   // 热更补充元数据
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

    public static string GetLocalPath(string path, bool isWebLoad)
    {
        string localPath = Path.Combine(Application.persistentDataPath, path);
        LogHelper.Log(localPath + " " + File.Exists(localPath));
        //热更目录存在，返回热更目录
        if (!File.Exists(localPath))
        {
            //热更目录不存在，返回streaming目录
            localPath = Path.Combine(Application.streamingAssetsPath, path);
        }
        if (isWebLoad)
        {
            //通过webReq加载
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!localPath.Contains("file:///"))
                {
                    localPath = "file://" + localPath;
                }
#elif UNITY_IOS && !UNITY_EDITOR
                localPath = "file://" + localPath;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                localPath = "file://" + localPath;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#else
#endif
            return localPath;
        }
        else
            return localPath;
    }
}
