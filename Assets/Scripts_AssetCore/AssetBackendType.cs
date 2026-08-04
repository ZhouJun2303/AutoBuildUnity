namespace Game.AssetCore
{
    public enum AssetBackendType
    {
        BundleMaster = 0,
        YooAsset = 1,
        Addressables = 2,
        Resources = 3,
    }

    /// <summary>
    /// YooAsset 运行模式（仅 Yoo 后端使用）
    /// </summary>
    public enum YooPlayModeKind
    {
        EditorSimulate = 0,
        Offline = 1,
        Host = 2,
    }
}
