﻿using System.Collections.Generic;
using UnityEngine;
using ET;
using UnityEngine.Networking;

namespace BM
{
    public partial class LoadBase
    {
        /// <summary>
        /// 引用计数
        /// </summary>
        private int _refCount = 0;

        /// <summary>
        /// AssetBundle加载的状态
        /// </summary>
        private LoadState _loadState = LoadState.NoLoad;

        /// <summary>
        /// 加载请求索引
        /// </summary>
        private AssetBundleCreateRequest _assetBundleCreateRequest;

        /// <summary>
        /// AssetBundle的引用
        /// </summary>
        public AssetBundle AssetBundle;

        /// <summary>
        /// 加载完成后需要执行的Task
        /// </summary>
        private List<ETTask> _loadFinishTasks = new List<ETTask>();

        /// <summary>
        /// 预下载加载完成后需要执行的Task
        /// </summary>
        private List<ETTask> _preDownloadFinishTasks = new List<ETTask>();

        /// <summary>
        /// 需要统计进度
        /// </summary>
        private WebLoadProgress _loadProgress = null;

        private void AddRefCount()
        {
            _refCount++;
            if (_refCount == 1 && _loadState == LoadState.Finish)
            {
                AssetComponent.SubPreUnLoadPool(this);
            }
        }

        internal void SubRefCount()
        {
            _refCount--;
            if (_loadState == LoadState.NoLoad)
            {
                AssetLogHelper.LogError("资源未被加载，引用不可能减少\n" + FilePath);
                return;
            }
            if (_loadState == LoadState.Loading)
            {
                AssetLogHelper.Log("资源加载中，等加载完成后再进入卸载逻辑\n" + FilePath);
                return;
            }
            if (_refCount <= 0)
            {
                //需要进入预卸载池等待卸载
                AssetComponent.AddPreUnLoadPool(this);
            }
        }

        internal void LoadAssetBundle(string bundlePackageName)
        {
            throw new System.Exception("LoadBase: not support LoadAssetBundle function!");
            //AddRefCount();
            //if (_loadState == LoadState.Finish)
            //{
            //    return;
            //}
            //if (_loadState == LoadState.Loading)
            //{
            //    AssetLogHelper.LogError("同步加载了正在异步加载的资源, 打断异步加载资源会导致所有异步加载的资源都立刻同步加载出来。资源名: " + FilePath +
            //                            "\nAssetBundle包名: " + AssetBundleName);
            //    if (_assetBundleCreateRequest != null)
            //    {
            //        AssetBundle = _assetBundleCreateRequest.assetBundle;
            //        return;
            //    }
            //}

            //if (AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].Encrypt)
            //{
            //    string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, true);
            //    byte[] data = VerifyHelper.GetDecryptData(assetBundlePath, AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].SecretKey);
            //    AssetBundle = AssetBundle.LoadFromMemory(data);
            //}
            //else
            //{
            //    string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, false);
            //    AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            //}
            //_loadState = LoadState.Finish;
            //for (int i = 0; i < _loadFinishTasks.Count; i++)
            //{
            //    _loadFinishTasks[i].SetResult();
            //}
            //_loadFinishTasks.Clear();
        }

        internal async ETTask LoadAssetBundleAsync(ETTask tcs, string bundlePackageName, bool isPreDownload = false, System.Action failCB = null)
        {
            AddRefCount();
            if (_loadState == LoadState.Finish)
            {
                tcs.SetResult();
                return;
            }
            if (_loadState == LoadState.Loading)
            {
                _loadFinishTasks.Add(tcs);
                if (isPreDownload) _preDownloadFinishTasks.Add(tcs);
                return;
            }
            _loadFinishTasks.Add(tcs);
            if (isPreDownload) _preDownloadFinishTasks.Add(tcs);
            _loadState = LoadState.Loading;

            if (AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].Encrypt)
            {
                string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, true);
                await LoadDataFinish(assetBundlePath, bundlePackageName);
            }
            else
            {
                string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, false);
                LoadBundleFinish(assetBundlePath, failCB);
            }
        }

        /// <summary>
        /// 通过Byte加载完成(只有启用了异或加密才使用此加载方式)
        /// </summary>
        private async ETTask LoadDataFinish(string assetBundlePath, string bundlePackageName)
        {
            byte[] data = await VerifyHelper.GetDecryptDataAsync(assetBundlePath, _loadProgress, AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].SecretKey);
            if (_loadState == LoadState.Finish)
            {
                return;
            }
            _assetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(data);
            _assetBundleCreateRequest.completed += operation =>
            {
                AssetBundle = _assetBundleCreateRequest.assetBundle;
                for (int i = 0; i < _loadFinishTasks.Count; i++)
                {
                    _loadFinishTasks[i].SetResult();
                }
                _loadFinishTasks.Clear();
                _loadState = LoadState.Finish;
                //判断是否还需要
                if (_refCount <= 0)
                {
                    AssetComponent.AddPreUnLoadPool(this);
                }
            };
        }

        /// <summary>
        /// 通过路径直接加载硬盘上的AssetBundle
        /// </summary>
        /// <param name="assetBundlePath"></param>
        private async void LoadBundleFinish(string assetBundlePath, System.Action failCB = null)
        {
            if (_loadState == LoadState.Finish)
            {
                return;
            }
            //Debug.Log($"Load Assetbundle start:{assetBundlePath}");
            bool isSuccess = false;
            for (int redownloadCount = 0; redownloadCount <= AssetComponentConfig.ReDownLoadCount; redownloadCount++)
            {
                ETTask tcs = ETTask.Create(true);
                using UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(assetBundlePath, 0);
                var opt = request.SendWebRequest();
                opt.completed += (_) =>
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"assetBundlePath:{assetBundlePath} ReDownloadCount:{redownloadCount} error:{request.error}");
                        if (redownloadCount < AssetComponentConfig.ReDownLoadCount)
                        {
                            tcs.SetResult();
                            return;
                        }
                    }
                    else
                    {
                        isSuccess = true;
                    }

                    bool exitTrulyDownload = _loadFinishTasks.Count > _preDownloadFinishTasks.Count;
                    if (exitTrulyDownload && isSuccess)
                    {
                        //并非只有预加载请求，就加载AB到内存
                        AssetBundle = DownloadHandlerAssetBundle.GetContent(request);
                    }

                    //失败回调
                    if (!isSuccess && failCB != null)
                    {
                        failCB();
                    }

                    for (int i = 0; i < _loadFinishTasks.Count; i++)
                    {
                        _loadFinishTasks[i].SetResult();
                    }
                    _loadFinishTasks.Clear();
                    _preDownloadFinishTasks.Clear();

                    if (exitTrulyDownload && isSuccess)
                    {
                        _loadState = LoadState.Finish;
                        //判断是否还需要
                        if (_refCount <= 0)
                        {
                            AssetComponent.AddPreUnLoadPool(this);
                        }
                    }
                    else
                    {
                        _loadState = LoadState.NoLoad;
                    }
                    tcs.SetResult();
                };
                await tcs;
                request.Dispose();

                if (isSuccess)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 强制加载完成
        /// </summary>
        internal void ForceLoadFinish(string bundlePackageName)
        {
            //if (_loadState == LoadState.Finish)
            //{
            //    return;
            //}
            //if (_assetBundleCreateRequest != null)
            //{
            //    AssetLogHelper.LogError("触发强制加载, 打断异步加载资源会导致所有异步加载的资源都立刻同步加载出来。资源名: " + FilePath +
            //                           "\nAssetBundle包名: " + AssetBundleName);
            //    AssetBundle = _assetBundleCreateRequest.assetBundle;
            //    return;
            //}

            //if (AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].Encrypt)
            //{
            //    string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, true);
            //    byte[] data = VerifyHelper.GetDecryptData(assetBundlePath, AssetComponent.BundleNameToRuntimeInfo[bundlePackageName].SecretKey);
            //    AssetBundle = AssetBundle.LoadFromMemory(data);
            //}
            //else
            //{
            //    string assetBundlePath = AssetComponent.BundleFileExistPath(bundlePackageName, AssetBundleName, false);
            //    AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            //}
            //for (int i = 0; i < _loadFinishTasks.Count; i++)
            //{
            //    _loadFinishTasks[i].SetResult();
            //}
            //_loadFinishTasks.Clear();
            //_loadState = LoadState.Finish;
            ////判断是否还需要
            //if (_refCount <= 0)
            //{
            //    AssetComponent.AddPreUnLoadPool(this);
            //}
        }

        /// <summary>
        /// 打开进度统计
        /// </summary>
        internal void OpenProgress()
        {
            _loadProgress = new WebLoadProgress();
        }

        internal float GetProgress()
        {
            if (_loadProgress == null)
            {
                AssetLogHelper.LogError("未打开进度统计无法获取进度");
                return 0;
            }
            if (_loadState == LoadState.Finish)
            {
                return 1;
            }
            if (_loadState == LoadState.NoLoad)
            {
                return 0;
            }

            if (_loadProgress.WeqOperation == null)
            {
                if (_assetBundleCreateRequest == null)
                {
                    return _loadProgress.GetWebProgress() / 2;
                }
                return _assetBundleCreateRequest.progress;
            }
            if (_assetBundleCreateRequest == null)
            {
                return _loadProgress.GetWebProgress() / 2;
            }
            return (_assetBundleCreateRequest.progress + 1.0f) / 2;
        }

    }

    /// <summary>
    /// AssetBundle加载的状态
    /// </summary>
    internal enum LoadState
    {
        NoLoad = 0,
        Loading = 1,
        Finish = 2
    }
}
