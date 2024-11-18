using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class launchGame : MonoBehaviour
{
    static AndroidJavaObject jo;
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
                AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
#endif
        Text text = transform.Find("DeviceUniqueIdentifier").GetComponent<Text>();
        text.text = $"Unity设备ID \n {SystemInfo.deviceUniqueIdentifier}";
        Button button = transform.Find("CopyButton").GetComponent<Button>();
        button.onClick.AddListener(OnCopyButtonClick);
        Button button1 = transform.Find("TestButton1").GetComponent<Button>();
        button1.onClick.AddListener(() =>
        {
            //jo?.Call("TestButton1");
            Debug.Log("资源热更测试按钮");
        });
        Button button2 = transform.Find("TestButton2").GetComponent<Button>();
        button2.onClick.AddListener(() =>
        {
            jo?.Call("TestButton2");
        });
        Button button3 = transform.Find("TestButton3").GetComponent<Button>();
        button3.onClick.AddListener(() =>
        {
            jo?.Call("TestButton3");
        });
    }

    private void OnCopyButtonClick()
    {
        GUIUtility.systemCopyBuffer = SystemInfo.deviceUniqueIdentifier;
    }
}
