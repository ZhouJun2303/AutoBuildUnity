using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 逐步执行构建步骤：日志、锁程序集重载、Refresh。
/// 无第三方 EditorCoroutine 依赖，使用 EditorApplication.update 驱动。
/// </summary>
public sealed class BuildStepRunner
{
    public bool IsRunning { get; private set; }
    public bool HasError { get; private set; }
    public IReadOnlyList<string> Logs => _logs;

    readonly List<string> _logs = new List<string>();
    IEnumerator _routine;
    Action _onChanged;

    public void Subscribe(Action onChanged) => _onChanged = onChanged;

    public void ClearLogs()
    {
        _logs.Clear();
        HasError = false;
        Notify();
    }

    public void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _logs.Add(line);
        Debug.Log(line);
        Notify();
    }

    public void RunSteps(IList<BuildStepInfo> steps)
    {
        if (IsRunning)
        {
            Log("已在构建中，忽略重复启动。");
            return;
        }
        if (steps == null || steps.Count == 0)
        {
            Log("没有可执行步骤。");
            return;
        }

        _routine = CoRun(steps);
        IsRunning = true;
        HasError = false;
        EditorApplication.update += Tick;
        Log($"开始流程，共 {steps.Count} 步。");
    }

    public void RunSingle(BuildStepInfo step)
    {
        if (step == null) return;
        RunSteps(new List<BuildStepInfo> { step });
    }

    public void Stop()
    {
        if (!IsRunning) return;
        EditorApplication.update -= Tick;
        _routine = null;
        IsRunning = false;
        try { EditorApplication.UnlockReloadAssemblies(); } catch { /* ignore */ }
        Log("流程已停止。");
        Notify();
    }

    void Tick()
    {
        if (_routine == null) return;
        try
        {
            if (!_routine.MoveNext())
            {
                EditorApplication.update -= Tick;
                _routine = null;
                IsRunning = false;
                Log(HasError ? "流程结束（有错误）。" : "所有步骤结束。");
                Notify();
            }
        }
        catch (Exception e)
        {
            HasError = true;
            Log("Runner 异常: " + e);
            Stop();
        }
    }

    IEnumerator CoRun(IList<BuildStepInfo> steps)
    {
        EditorApplication.LockReloadAssemblies();
        try
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (HasError) break;
                var step = steps[i];
                Log($"步骤 {i + 1}/{steps.Count} 开始:【{step.Title}】{step.Description}");
                yield return null;

                try
                {
                    step.Action?.Invoke();
                }
                catch (Exception e)
                {
                    HasError = true;
                    Log($"步骤失败: {step.Title}\n{e}");
                    break;
                }

                FileOps.RefreshAssets();
                Log($"步骤 {i + 1} 结束: {step.Title}");
                // 短暂让出一帧，便于 Unity 导入生成文件
                yield return null;
            }
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }

    void Notify() => _onChanged?.Invoke();
}
