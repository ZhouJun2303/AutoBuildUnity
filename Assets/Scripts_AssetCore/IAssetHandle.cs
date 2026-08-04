using System;
using Object = UnityEngine.Object;

namespace Game.AssetCore
{
    public interface IAssetHandle : IDisposable
    {
        string AssetPath { get; }
        bool IsValid { get; }
    }

    public interface IAssetHandle<out T> : IAssetHandle where T : Object
    {
        T Asset { get; }
    }

    public interface ISceneHandle : IDisposable
    {
        string ScenePath { get; }
        float Progress { get; }
        bool IsDone { get; }
        bool Succeeded { get; }
        string Error { get; }
    }

    public interface IUpdateInfo
    {
        bool NeedUpdate { get; }
        long TotalBytes { get; }
        System.Collections.Generic.IReadOnlyList<string> Packages { get; }
    }
}
