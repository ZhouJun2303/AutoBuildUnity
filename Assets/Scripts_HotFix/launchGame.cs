using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using MyTaskTest;
using BM;
using ET;

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
            CustomLog("资源热更测试按钮");
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
        Test5();

        StartCoroutine(Custom1Enumerator());

        LoadResTest().Coroutine();
    }

    IEnumerator Custom1Enumerator()
    {
        CustomLog("CustomEnumerator log1");
        yield return new WaitForSeconds(1);
        CustomLog("CustomEnumerator log2");
        yield return new WaitForFixedUpdate();
        CustomLog("CustomEnumerator log3");
        yield return new WaitForFixedUpdate();
        Invoke("InvokTest", 1);
    }

    private void InvokTest()
    {
        CustomLog("InvokTest log1");
    }

    private void CustomLog(string log)
    {
        Debug.Log(log);
    }

    private void OnCopyButtonClick()
    {
        GUIUtility.systemCopyBuffer = SystemInfo.deviceUniqueIdentifier;
    }

    /// <summary>
    /// My Task
    /// </summary>
    /// <returns></returns>
    private async MyTask Test1()
    {
        CustomLog($"TimeNow1_1 {DateTime.Now.ToString()}");
        await new WaitForSeconds(3);
        CustomLog($"TimeNow2_1 {DateTime.Now.ToString()}");
    }


    /// <summary>
    /// My Task
    /// </summary>
    /// <returns></returns>
    private async MyTask Test2()
    {
        CustomLog($"TimeNow2_1 {DateTime.Now.ToString()}");
        await new WaitForSeconds(3);
        CustomLog($"TimeNow2_2 {DateTime.Now.ToString()}");
        await new WaitForSeconds(3);
        CustomLog($"TimeNow2_3 {DateTime.Now.ToString()}");
    }

    private async Task Test3()
    {
        CustomLog($"TimeNow3_1 {DateTime.Now.ToString()}");
        await new WaitForSeconds(3);
        CustomLog($"TimeNow3_2 {DateTime.Now.ToString()}");
    }

    private async Task Test4()
    {
        CustomLog($"TimeNow4_1 {DateTime.Now.ToString()}");
        await new WaitForSeconds(3);
        CustomLog($"TimeNow4_2 {DateTime.Now.ToString()}");
    }

    private async MyTask Test5()
    {
        await Test1();
        await Test2();
        await Test3();
        await Test4();
        await new WaitForSeconds(5);
        Test6();
    }


    private void Test6()
    {
        Test1();
        Test2();
        Test3();
        Test4();
    }

    private async ETTask LoadResTest()
    {
        Debug.Log("LoadResTest");
        var res = await AssetComponent.LoadAsync<Sprite>(BPath.Assets_HotRes_egg__png);
        Debug.Log(res);
        Transform t = transform.Find("TestImage");
        if (null != t)
        {
            Debug.Log(t);
            Image image = t.GetComponent<Image>();
            image.sprite = res;
        }

    }
}
