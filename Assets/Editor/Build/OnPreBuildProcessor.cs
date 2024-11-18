using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class OnPreBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        // 在打包之前执行的操作
        Debug.Log("Pre-processing build...");
        // 可以在这里添加任何你需要在打包之前执行的操作
        if (BuildProjectWindows.GetPrebuildExportExcels())
        {

        }
    }
}
