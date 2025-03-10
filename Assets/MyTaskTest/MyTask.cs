using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace System.Runtime.CompilerServices
{
    public class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType
        {
            get;
        }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
}

namespace MyTaskTest
{
    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(MyAsyncTaskMethodBuilder))]
    public class MyTask : ICriticalNotifyCompletion, INotifyCompletion
    {
        public void OnCompleted(Action continuation)
        {
            continuation?.Invoke();
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public static MyTask Create()
        {
            return new MyTask();
        }

        internal void SetException(Exception exception)
        {
            throw new NotImplementedException();
        }

        internal void SetResult()
        {

        }

        public MyTask GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted
        {
            get
            {
                return true;
            }
        }

        public void GetResult()
        {
        }
    }
}
