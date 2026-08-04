using UnityEngine;

namespace Game.AssetCore
{
    [CreateAssetMenu(fileName = "AssetBackendConfig", menuName = "CreateAsset/AssetBackendConfig")]
    public class AssetBackendConfig : ScriptableObject
    {
        public const string ResourceName = "AssetBackendConfig";

        [Header("后端选择（启动前生效）")]
        public AssetBackendType Backend = AssetBackendType.BundleMaster;

        [Header("默认分包名")]
        public string DefaultPackageName = "AllBundle";

        [Header("YooAsset")]
        public YooPlayModeKind YooPlayMode = YooPlayModeKind.Offline;
        public string YooEditorSimulateRoot = "";

        public static AssetBackendConfig LoadOrDefault()
        {
            var config = Resources.Load<AssetBackendConfig>(ResourceName);
            if (config != null)
                return config;

            config = CreateInstance<AssetBackendConfig>();
            config.Backend = AssetBackendType.BundleMaster;
            config.DefaultPackageName = "AllBundle";
            Debug.LogWarning("[AssetBackendConfig] Resources 中未找到配置，使用默认 BundleMaster");
            return config;
        }

        public AssetRuntimeOptions ToRuntimeOptions(string bundleServerUrl = null)
        {
            return new AssetRuntimeOptions
            {
                DefaultPackageName = string.IsNullOrEmpty(DefaultPackageName) ? "AllBundle" : DefaultPackageName,
                BundleServerUrl = bundleServerUrl ?? string.Empty,
                YooPlayMode = YooPlayMode,
                YooEditorSimulateRoot = YooEditorSimulateRoot ?? string.Empty,
            };
        }
    }
}
