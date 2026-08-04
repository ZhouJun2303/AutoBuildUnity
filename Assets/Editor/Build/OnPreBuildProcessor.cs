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
        Debug.Log($"Pre-processing build for target: {report.summary.platform}");
        
        // 根据配置执行Excel导出操作
        if (BuildProjectWindows.GetPrebuildExportExcels())
        {
            // 这里可以添加实际的Excel导出逻辑
            // 例如：ExcelExportUtility.ExportAll();
            Debug.Log("Exporting Excel files before build...");
        }
    }
}
