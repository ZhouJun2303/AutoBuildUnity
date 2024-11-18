using System;
using UnityEngine;

namespace Scripts_AOT.Utility
{
    public static class LogHelper
    {
        public const string Log_Flag = "[Hlwd Log]: ";

        public static void Log(string str)
        {
            Debug.Log(Log_Flag + str);
        }
        public static void LogError(string str)
        {
            Debug.LogError(Log_Flag + str);
        }
    }
}
