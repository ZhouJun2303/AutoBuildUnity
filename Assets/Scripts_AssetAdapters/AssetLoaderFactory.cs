using Game.AssetCore;

namespace Game.AssetAdapters
{
    public static class AssetLoaderFactory
    {
        public static IAssetLoader Create(AssetBackendType backend)
        {
            switch (backend)
            {
                case AssetBackendType.YooAsset:
                    return new YooAssetAssetLoader();
                case AssetBackendType.Addressables:
                    return new AddressablesAssetLoader();
                case AssetBackendType.Resources:
                    return new ResourcesAssetLoader();
                case AssetBackendType.BundleMaster:
                default:
                    return new BundleMasterAssetLoader();
            }
        }
    }
}
