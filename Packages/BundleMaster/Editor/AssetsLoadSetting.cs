using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BM
{
    /// <summary>
    /// 用于配置单个Bundle包的构建信息
    /// </summary>
    public class AssetsLoadSetting : AssetsSetting
    {
        [Header("分包名字")]
        [Tooltip("当前分包的包名(建议英文)")] public string BuildName;

        [Header("版本索引")]
        [Tooltip("表示当前Bundle的索引")] public int BuildIndex;

        [Header("AssetBundle的后缀")]
        [Tooltip("AssetBundle资源的的后缀名(如'bundle')")] public string BundleVariant;

        [Header("是否启用Hash名")]
        [Tooltip("是否使用Hash名替换Bundle名称")] public bool NameByHash;

        [Header("构建选项")]
        public BuildAssetBundleOptions BuildAssetBundleOptions = BuildAssetBundleOptions.UncompressedAssetBundle;

        [Header("是否加密资源")]
        [Tooltip("加密启用后会多一步异或操作")] public bool EncryptAssets;

        [Header("加密密钥")]
        [Tooltip("进行异或操作的密钥")] public string SecretKey;

        [Header("被依赖次数限制")]
        [Tooltip("被依赖次数>=x时创建新包，否则资源可以重复入包")] public int LimitDependedCount = 1;
        [Header("被依赖资源大小限制")]
        [Tooltip("被依赖资源大小>=x时创建新包，否则资源可以重复入包")] public int LimitDependedFileSize = 100;

        [Header("资源路径")]
        [Tooltip("需要打包的资源所在的路径(不需要包含依赖, 只包括需要主动加载的资源)")]
        public List<string> AssetPath = new List<string>();

        [Header("一组资源路径")]
        [Tooltip("资源颗粒控制(每个路径下的资源会打在一个ab中而不是每个独立资源一个ab)")]
        public List<string> AssetGroupPaths = new List<string>();

        [Header("场景资源")]
        [Tooltip("需要通过Bundle加载的场景")]
        public List<SceneAsset> Scene = new List<SceneAsset>();

        /// <summary>
        /// 检查配置的资源路径是否存在
        /// </summary>
        /// <returns></returns>
        public override bool CheckResPathIsExist()
        {
            var absolutePath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);

            foreach (var path in AssetPath)
            {
                if (!Directory.Exists(Path.Combine(absolutePath, path)))
                {
                    throw new System.Exception($"{Path.Combine(absolutePath, path)} 路径不存在！");
                }
            }
            foreach (var path in AssetGroupPaths)
            {
                if (!Directory.Exists(Path.Combine(absolutePath, path)))
                {
                    throw new System.Exception($"{Path.Combine(absolutePath, path)} 路径不存在！");
                }
            }
            foreach (var scene in Scene)
            {
                if (scene == null)
                {
                    throw new System.Exception($"场景资源丢失！");
                }
            }
            return true;
        }

        /// <summary>
        /// 该依赖资源是否可以单独成ab包
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="dependCount">依赖次数</param>
        public bool IsSingleDependBundle(string assetPath, int dependCount)
        {
            if (dependCount > LimitDependedCount)
            {
                var file = new FileInfo(assetPath);
                var fileSize = (int)(file.Length / 1000f);
                if (fileSize >= LimitDependedFileSize) return true;
            }
            return false;
        }
    }
}


