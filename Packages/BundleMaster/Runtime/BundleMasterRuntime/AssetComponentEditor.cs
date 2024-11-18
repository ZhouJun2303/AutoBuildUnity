#if UNITY_EDITOR

namespace BM
{
    /// <summary>
    /// 提供editor下用的部分接口
    /// </summary>
    public partial class AssetComponent
    {
        public static (int, int, int, int) Editor_GetBundleRuntimeInfo()
        {
            if (BundleNameToRuntimeInfo == null || !BundleNameToRuntimeInfo.TryGetValue(AssetComponentConfig.DefaultBundlePackageName, out var info))
                return (-1, -1, -1, -1);
            return (info.AllAssetLoadHandler.Count, info.UnLoadHandler.Count, PreUnLoadPool.Count, TrueUnLoadPool.Count);
        }

    }
}

#endif
