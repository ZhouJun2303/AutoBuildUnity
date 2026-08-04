using System.IO;

namespace Game.AssetCore
{
    public static class AssetPathUtility
    {
        private const string ResourcesPrefix = "Assets/Resources/";

        /// <summary>
        /// Assets/Resources/Foo/Bar.png -> Foo/Bar
        /// </summary>
        public static bool TryToResourcesPath(string assetPath, out string resourcesPath)
        {
            resourcesPath = null;
            if (string.IsNullOrEmpty(assetPath))
                return false;

            var normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(ResourcesPrefix))
                return false;

            var relative = normalized.Substring(ResourcesPrefix.Length);
            resourcesPath = Path.ChangeExtension(relative, null)?.Replace('\\', '/');
            return !string.IsNullOrEmpty(resourcesPath);
        }

        public static string GetSceneName(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return scenePath;
            var name = Path.GetFileNameWithoutExtension(scenePath.Replace('\\', '/'));
            return name;
        }

    }
}
