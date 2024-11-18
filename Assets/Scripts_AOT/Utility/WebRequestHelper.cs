using ET;
using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;

namespace Scripts_AOT.Utility
{
    public static class WebRequestHelper
    {

        public static string MonitoringReqUrl
        {
            get
            {
                return @"https://home.hlwdgames.com:8080/add/cn/err";
            }
        }

        /// <summary>
        /// POST请求数据
        /// </summary>
        /// <param name="url">获取Token值的服务URL地址（很重要）</param>
        /// <param name="postData">传入请求的参数，此处参数为JOSN格式</param>
        /// <returns></returns>
        public static IEnumerator PostUrl(string url, string postData, Action<string> finishCallback)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))//第二种写法此行注释
            {
                //UnityWebRequest webRequest = new UnityWebRequest(url, "POST");//第二种写法此行取消注释
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(postData);
                webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(postBytes);
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    LogHelper.LogError(webRequest.error);
                }
                else
                {
                    if (finishCallback != null) finishCallback(webRequest.downloadHandler.text);
                }
            }
        }

        /// <summary>
        /// 异步POST请求数据
        /// </summary>
        /// <param name="url">获取Token值的服务URL地址（很重要）</param>
        /// <param name="postData">传入请求的参数，此处参数为JOSN格式</param>
        /// <returns></returns>
        public static async ETTask AsyncPostUrl(string url, string postData, Action<string> finishCallback)
        {
            ETTask logTcs = ETTask.Create();
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(postData);
                webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(postBytes);
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                UnityWebRequestAsyncOperation weq = webRequest.SendWebRequest();
                weq.completed += (o) =>
                {
                    logTcs.SetResult();
                };
                await logTcs;

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(webRequest.error);
                }
                else
                {
                    if (finishCallback != null) finishCallback(webRequest.downloadHandler.text);
                }
            }
        }
    }
}
