using System;
using Game.AssetCore;

/// <summary>单步构建描述：标题、说明、所属后端、执行体。</summary>
public sealed class BuildStepInfo
{
    public BuildStepId Id { get; }
    public string Title { get; }
    public string Description { get; }
    /// <summary>null 表示 HybridCLR / 公共步骤，可被任意 AB 预设引用。</summary>
    public AssetBackendType? Backend { get; }
    public Action Action { get; }

    public BuildStepInfo(BuildStepId id, string title, string description, Action action, AssetBackendType? backend = null)
    {
        Id = id;
        Title = title;
        Description = description;
        Action = action;
        Backend = backend;
    }
}
