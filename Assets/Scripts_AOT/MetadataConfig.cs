

public class MetadataConfig
{
    public static string[] AotAssemblyMetadatas = new string[] {
        "BundleMaster.dll",
        "MyTaskTest.dll",
        "System.dll",
        };

    public static string GetStripMetadataName(string name)
    {
        return "Strip_" + name;
    }
}
