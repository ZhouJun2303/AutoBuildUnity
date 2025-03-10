

public class MetadataConfig
{
    public static string[] AotAssemblyMetadatas = new string[] {
            "BundleMaster.dll",
            "AsyncAwaitUtil.dll",
            "GameAOT.dll",
            "mscorlib.dll",
            "System.Core.dll",
            "UnityEngine.AndroidJNIModule.dll",
            "System.dll",
            "MyTaskTest.dll",
        };

    public static string GetStripMetadataName(string name)
    {
        return "Strip_" + name;
    }
}
