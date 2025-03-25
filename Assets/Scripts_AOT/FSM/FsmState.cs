
using System;
using System.Collections.Generic;
using UnityEngine;

public class FsmState<T> where T : System.Enum
{
    public T stateID;
    public Action enterAction;
    public Action updateAction;
    public Action leaveAction;
    public FsmState(T state)
    {
        this.stateID = state;
    }
    public FsmState<T> OnEnter(Action action)
    {
        enterAction = action;
        return this;
    }
    public FsmState<T> OnUpdate(Action action)
    {
        updateAction = action;
        return this;
    }
    public FsmState<T> OnLeave(Action action)
    {
        leaveAction = action;
        return this;
    }
}
