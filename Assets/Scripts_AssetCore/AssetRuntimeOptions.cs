namespace Game.AssetCore
{
    public class AssetRuntimeOptions
    {
        public string DefaultPackageName = "AllBundle";
        public string BundleServerUrl = "";
        public YooPlayModeKind YooPlayMode = YooPlayModeKind.Offline;
        /// <summary>
        /// Yoo EditorSimulate 模式的包根目录；为空时回退 Offline
        /// </summary>
        public string YooEditorSimulateRoot = "";
    }
}
