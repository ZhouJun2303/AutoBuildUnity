using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuildProjectWindows : EditorWindow
{
    private string[] _buttonNames = new string[] { "Android", "iOS" };
    private int _localSeletTopType = 0;
    private bool _prebuildExportExcels;
    private bool _afterBuildCopyRes;
    private bool _afterBuildCopyLibRes;
    private bool _afterBuildCopyUnityDataAssetPack;
    private bool _shouldEnableSplitAPK;
    private string _androidOutPath;
    private string _iosOutPath;


    [MenuItem("打包/打包面板", false, 0)]
    public static void Init()
    {
        BuildProjectWindows window = (BuildProjectWindows)EditorWindow.GetWindow(typeof(BuildProjectWindows));
        window.minSize = new Vector2(400, 400);
        window.Show();
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(EditorStyles.textArea);
        style.wordWrap = true;
        style.richText = true;
        _localSeletTopType = GUILayout.Toolbar(_localSeletTopType, _buttonNames);

        _prebuildExportExcels = EditorGUI.Toggle(new Rect(10, 30, 100, 20), "打包前是否自动导出 Excel ", _prebuildExportExcels);
        GUILayout.Space(30);
        _afterBuildCopyRes = EditorGUI.Toggle(new Rect(10, 50, 100, 20), "打包之后自动拷贝资源 ", _afterBuildCopyRes);
        GUILayout.Space(30);
        if (_localSeletTopType == 0)
        {
            _afterBuildCopyLibRes = EditorGUI.Toggle(new Rect(10, 70, 100, 20), "打包之后自动拷贝库文件 ", _afterBuildCopyLibRes);
            GUILayout.Space(30);
            _afterBuildCopyUnityDataAssetPack = EditorGUI.Toggle(new Rect(10, 90, 100, 20), "打包之后自动拷贝分包文件 ", _afterBuildCopyUnityDataAssetPack);
            GUILayout.Space(30);
            _shouldEnableSplitAPK = EditorGUI.Toggle(new Rect(10, 110, 100, 20), "是否分包 ", _shouldEnableSplitAPK);
            GUILayout.Space(30);
        }

        EditorGUI.BeginDisabledGroup(_localSeletTopType == 1);
        _androidOutPath = EditorGUILayout.TextField("Android 导出路径 ", _androidOutPath, GUILayout.MinWidth(200));
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(_localSeletTopType == 0);
        _iosOutPath = EditorGUILayout.TextField("iOS 导出路径 ", _iosOutPath, GUILayout.MinWidth(200));
        EditorGUI.EndDisabledGroup();


        if (_localSeletTopType == 0)
        {
            if (GUILayout.Button("安卓工程", GUILayout.ExpandWidth(true)))
            {
                SaveOutPutPath();
                BuildProject.BuildAndroidProject();
            }
            if (GUILayout.Button("安卓工程资源拷贝", GUILayout.ExpandWidth(true)))
            {
                SaveOutPutPath();
                OnPostprocessBuild_Android.CopyUnityRes();
            }
            if (GUILayout.Button("安卓工程库文件拷贝", GUILayout.ExpandWidth(true)))
            {
                SaveOutPutPath();
                OnPostprocessBuild_Android.CopyUnityLibRes();
            }
            if (GUILayout.Button("安卓工程拷贝分包文件", GUILayout.ExpandWidth(true)))
            {
                SaveOutPutPath();
                OnPostprocessBuild_Android.CopyUnityDataAssetPack();
            }
        }
        if (_localSeletTopType == 1)
        {
            if (GUILayout.Button("iOS工程", GUILayout.ExpandWidth(true)))
            {
                SaveOutPutPath();
                BuildProject.BuildXcodeProject1();
            }
            if (GUILayout.Button("iOS工程资源拷贝", GUILayout.ExpandWidth(true)))
            {
                SaveOutPutPath();
                OnPostprocessBuild_IOS.CopyUnityRes();
            }
        }

        //GUIStyle coloredButtonStyle = new GUIStyle(EditorStyles.textArea);
        //coloredButtonStyle = new GUIStyle(GUI.skin.button);
        //coloredButtonStyle.normal.textColor = Color.red; // 设置文本颜色
        //coloredButtonStyle.normal.background = MakeTex(2, 2, Color.green); // 设置背景颜色
        //if (GUILayout.Button("保存配置路径,修改配置点击此按钮", coloredButtonStyle, GUILayout.ExpandWidth(true)))
        //{
        //    SaveOutPutPath();
        //}
    }

    // 创建一个纯色纹理
    private static Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = color;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnEnable()
    {
        GetOutPathByMemory();
    }


    private void OnDestroy()
    {
        SaveOutPutPath();
    }

    private void GetOutPathByMemory()
    {
        _localSeletTopType = EditorPrefs.GetInt("localSeletTopType", 0);
        _androidOutPath = GetAndroidOutPath();
        _iosOutPath = GetIosOutPath();
        _prebuildExportExcels = GetPrebuildExportExcels();
        _afterBuildCopyRes = GetAfterBuildCopyRes();
        _afterBuildCopyLibRes = GetAfterBuildCopyLibRes();
        _afterBuildCopyUnityDataAssetPack = GetAfterBuildCopyUnityDataAssetPack();
        _shouldEnableSplitAPK = GetShouldEnableSplitAPK();
    }

    private void SaveOutPutPath()
    {
        EditorPrefs.SetInt("localSeletTopType", _localSeletTopType);
        EditorPrefs.SetString("androidOutPath", _androidOutPath);
        EditorPrefs.SetString("iOSOutPath", _iosOutPath);
        EditorPrefs.SetBool("prebuildExportExcels", _prebuildExportExcels);
        EditorPrefs.SetBool("afterBuildCopyRes", _afterBuildCopyRes);
        EditorPrefs.SetBool("afterBuildCopyLibRes", _afterBuildCopyLibRes);
        EditorPrefs.SetBool("afterBuildCopyUnityDataAssetPack", _afterBuildCopyUnityDataAssetPack);
        EditorPrefs.SetBool("shouldEnableSplitAPK", _shouldEnableSplitAPK);
        if (!_shouldEnableSplitAPK)
        {
            _afterBuildCopyUnityDataAssetPack = _shouldEnableSplitAPK;
            EditorPrefs.SetBool("afterBuildCopyUnityDataAssetPack", _afterBuildCopyUnityDataAssetPack);
        }

        Debug.Log("SaveOutPutPath  success!");
    }

    /// <summary>
    /// 获取安卓导出路径
    /// </summary>
    /// <returns></returns>
    public static string GetAndroidOutPath()
    {
        return EditorPrefs.GetString("androidOutPath");
    }

    /// <summary>
    /// 获取iOS 导出路径
    /// </summary>
    /// <returns></returns>
    public static string GetIosOutPath()
    {
        return EditorPrefs.GetString("iOSOutPath");
    }

    /// <summary>
    /// 打包之前是否导表
    /// </summary>
    /// <returns></returns>
    public static bool GetPrebuildExportExcels()
    {
        return EditorPrefs.GetBool("prebuildExportExcels", true);
    }

    /// <summary>
    /// 打包之后是否拷贝资源
    /// </summary>
    /// <returns></returns>
    public static bool GetAfterBuildCopyRes()
    {
        return EditorPrefs.GetBool("afterBuildCopyRes", true);
    }

    /// <summary>
    /// 打包之后是否拷贝库文件
    /// </summary>
    /// <returns></returns>
    public static bool GetAfterBuildCopyLibRes()
    {
        return EditorPrefs.GetBool("afterBuildCopyLibRes", false);
    }

    /// <summary>
    /// 打包之后是否拷贝分包文件
    /// </summary>
    /// <returns></returns>
    public static bool GetAfterBuildCopyUnityDataAssetPack()
    {
        return EditorPrefs.GetBool("afterBuildCopyUnityDataAssetPack", false);
    }

    /// <summary>
    /// 是否分包
    /// </summary>
    /// <returns></returns>
    public static bool GetShouldEnableSplitAPK()
    {
        return EditorPrefs.GetBool("shouldEnableSplitAPK", true);
    }
}
