

public class MetadataConfig
{
    public static string[] AotAssemblyMetadatas = new string[] {
        "BundleMaster.dll",
        "MyTaskTest.dll",
        "System.dll",
        "UnityEngine.AndroidJNIModule.dll",
        "UnityEngine.CoreModule.dll",
        "mscorlib.dll",
        };

    public static string GetStripMetadataName(string name)
    {
        return "Strip_" + name;
    }
}
