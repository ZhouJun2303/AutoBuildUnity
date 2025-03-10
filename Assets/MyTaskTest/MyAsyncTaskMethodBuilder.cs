using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MyTaskTest
{
    public struct MyAsyncTaskMethodBuilder
    {
        public MyTask _tsc;
        public MyTask Task
        {
            get
            {
                return _tsc;
            }
        }

        public static MyAsyncTaskMethodBuilder Create()
        {
            MyAsyncTaskMethodBuilder builder = new MyAsyncTaskMethodBuilder() { _tsc = MyTask.Create() };
            return builder;
        }

        public void SetException(Exception exception)
        {
            _tsc.SetException(exception);
        }

        public void SetResult()
        {
            _tsc.SetResult();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }
}