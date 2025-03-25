
using Scripts_AOT.Utility;
using System;
using System.Collections.Generic;


/// <summary>
/// 有限状态机
/// </summary>

public class FsmCtrl
{
    private static readonly string FSM_UPDATE_TIMER = "FSM_UPDATE_TIMER";
    private int idGenerator = 0;
    private List<Action> updateList = new List<Action>();
    public int GetNewID() { return idGenerator++; }

    /// <summary>
    /// 创建FSM
    /// </summary>
    public FSM<T> CreateFSM<T>(string name) where T : System.Enum
    {
        var fsm = new FSM<T>(name, GetNewID());
        AddUpdate(fsm.OnUpdate);
        return fsm;
    }

    /// <summary>
    /// 创建继承于FSM的子类
    /// </summary>
    public T1 CreateFSM<T1, T2>(string name) where T1 : FSM<T2> where T2 : System.Enum
    {
        object[] paramers = new object[] { name, GetNewID() };
        var fsm = (T1)Activator.CreateInstance(typeof(T1), paramers);
        AddUpdate(fsm.OnUpdate);
        return fsm;
    }

    public void DestroyFsm<T>(FSM<T> fsm) where T : System.Enum
    {
        if (fsm == null) return;
        fsm.Destory();
        RemoveUpdate(fsm.OnUpdate);
    }

    private void AddUpdate(Action updateAction)
    {
        if (!updateList.Contains(updateAction))
            updateList.Add(updateAction);
    }

    private void RemoveUpdate(Action updateAction)
    {
        if (updateList.Contains(updateAction))
            updateList.Remove(updateAction);
    }

    public void OnUpdate()
    {
        for (int i = 0; i < updateList.Count; i++)
        {
            try
            {
                updateList[i].Invoke();
            }
            catch (Exception err)
            {
                LogHelper.LogError(err.ToString());
            }
        }
    }
}

