
using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
///  状态机
/// </summary>
public class FSM<T> where T : System.Enum
{
    public string FsmName { private set; get; }
    public int Id { private set; get; }
    private bool IsStop;
    private Dictionary<T, FsmState<T>> stateDic = new Dictionary<T, FsmState<T>>(8);
    private T currentState;
    public bool isLog;
    public T State { get { return currentState; } }

    public FSM(string name, int id)
    {
        FsmName = name;
        IsStop = true;
        Id = id;
    }
    public void Destory()
    {
        stateDic.Clear();
        stateDic = null;
    }

    public void AddState(FsmState<T> state)
    {
        stateDic.Add(state.stateID, state);
    }

    public void StartFsm()
    {
        IsStop = false;
    }
    public void StopFsm()
    {
        IsStop = true;
    }
    public void EndFsm()
    {
        IsStop = true;
        currentState = default(T);
    }

    public void SetState(T newState)
    {
        if (isLog) Debug.Log($"FSM {FsmName}.{Id}# : {currentState}=>{newState}");

        var oldState = currentState;
        currentState = newState;

        try
        {
            if (stateDic.ContainsKey(oldState))
            {
                stateDic[oldState].leaveAction?.Invoke();
            }
            if (stateDic.ContainsKey(newState))
            {
                stateDic[newState].enterAction?.Invoke();
            }
            else
            {
                Debug.LogError("Error unhandled state:" + newState);
            }
        }
        catch (Exception err)
        {
            Debug.LogError(err);
        }
    }

    public void OnUpdate()
    {
        if (IsStop) return;
        if (!stateDic.ContainsKey(currentState)) return;
        stateDic[currentState].updateAction?.Invoke();
    }
}
