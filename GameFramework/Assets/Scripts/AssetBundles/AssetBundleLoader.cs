using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FrameWork.Assets
{
    public enum EBundleLoadState
    {
        EState_None,
        EState_Loading,
        EState_Complete,
        EState_Error
    }

    public abstract class AssetBundleLoader
    {
        internal Action<AssetBundleInfo> onLoadComplete = null;
        internal Action<AssetBundleLoader> onManagerLoadComplete = null;
        internal Action<AssetBundleLoader> onManagerLoadError = null;
        internal Action<AssetBundleLoader> onDepLoadCompleted = null;

        public string bundleName = string.Empty;
        public ConfResourceItem bundleData = null;
        public AssetBundleInfo bundleInfo;
        public EBundleLoadState state = EBundleLoadState.EState_None;
        public List<AssetBundleLoader> dependLoaders = new List<AssetBundleLoader>();

        public object param = null;

        public bool isAsync = true;

        public virtual void LoadAsync() { }

        public virtual void LoadBundleAsync() { }

        public virtual void LoadSync() { }

        public virtual void LoadBundleSync() { }

        public virtual bool IsComplete
        {
            get
            {
                return state == EBundleLoadState.EState_Complete || state == EBundleLoadState.EState_Error;
            }
        }

        public AssetBundleLoader(Action<AssetBundleLoader> onManagerComplete, Action<AssetBundleLoader> onManagerError)
        {
            this.onManagerLoadComplete = onManagerComplete;
            this.onManagerLoadError = onManagerError;
        }

        protected AssetBundleInfo CreateBundleInfo(AssetBundleInfo abInfo = null, AssetBundle assetBundle = null, byte[] bytes = null, string text = null)
        {
            if (null == abInfo)
            {
                abInfo = new AssetBundleInfo();
            }

            abInfo.bundleName = this.bundleName;
            abInfo.bundle = assetBundle;
            abInfo.bundleData = this.bundleData;
            abInfo.bytes = bytes;
            abInfo.text = string.IsNullOrEmpty(text) ? string.Empty : text;
            abInfo.param = this.param;

            return abInfo;
        }

        public virtual void OnComplete()
        {
            if (null != onLoadComplete)
            {
                try
                {
                    onLoadComplete(bundleInfo);
                }
                catch(Exception e)
                {
                    Logger.LogError(e.ToString());
                }
                finally
                {
                    onLoadComplete = null;
                }
            }

            if (null != onManagerLoadComplete)
            {
                try
                {
                    onManagerLoadComplete(this);
                }
                finally
                {
                    onManagerLoadComplete = null;
                }
            }
        }

        public virtual void OnError()
        {
            if (null != onLoadComplete)
            {
                try
                {
                    onLoadComplete(bundleInfo);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString());
                }
                finally
                {
                    onLoadComplete = null;
                }
            }

            if (null != onManagerLoadError)
            {
                try
                {
                    onManagerLoadError(this);
                }
                finally
                {
                    onManagerLoadError = null;
                }
            }
        }
    }
#if UNITY_EDITOR
    public class EditorModeAssetBundleLoader : AssetBundleLoader
    {
        private bool loadMulti = false;
        public EditorModeAssetBundleLoader(bool loadMulti, Action<AssetBundleLoader> onManagerComplete, Action<AssetBundleLoader> onManagerError):
            base(onManagerComplete, onManagerError)
        {
            this.loadMulti = loadMulti;
        }

        private sealed class EditorSingleAssetBundleInfo : AssetBundleInfo
        {
            public override UnityEngine.Object MainObject
            {
                get
                {
                    if (null == mainObject)
                    {
                        string path = string.Format("Assets/{0}", bundleName);
                        mainObject = AssetDatabase.LoadMainAssetAtPath(path);
                        if (null == mainObject)
                        {
                            Logger.LogError("EditorAssetBundleLoader load main object failed!!! bundlename = " + bundleName);
                        }
                    }

                    return mainObject;
                }
            }
        }

        private sealed class EditorMultiAssetBundleInfo : AssetBundleInfo
        {
            public override UnityEngine.Object[] mainObjects
            {
                get
                {
                    List<Object> list = new List<Object>();
                    string assetName = bundleName.Substring(0, bundleName.LastIndexOf("."));
                    string[] paths = System.IO.Directory.GetFiles(System.IO.Path.Combine(Application.dataPath, assetName));
                    for (int index = 0; index < paths.Length; ++index)
                    {
                        string path = paths[index];
                        if (path.EndsWith(".meta") || path.EndsWith(".DS_Store"))
                        {
                            continue;
                        }
                        string fileName = path.Replace("\\", "/").Substring(Application.dataPath.Length + 1);
                        list.Add(AssetDatabase.LoadMainAssetAtPath("Assets/" + fileName));
                    }

                    return list.ToArray();
                }
            }
        }

        private void InitAssetBundleInfo()
        {
            AssetBundleInfo abInfo = null;
            if (loadMulti)
            {
                abInfo = new EditorMultiAssetBundleInfo();
            }
            else
            {
                abInfo = new EditorSingleAssetBundleInfo();
            }

            this.state = EBundleLoadState.EState_Complete;
            this.bundleInfo = CreateBundleInfo(abInfo);
            this.bundleInfo.IsReady = true;
            this.bundleInfo.onUnloaded = OnBundleUnload;
        }

        public override void LoadAsync()
        {
            if (null == bundleInfo)
            {
                InitAssetBundleInfo();
            }

            AssetBundleManager.Instance.StartCoroutine(LoadResource());
        }

        public override void LoadSync()
        {
            if (null == bundleInfo)
            {
                InitAssetBundleInfo();
            }

            this.OnComplete();
        }

        private void OnBundleUnload(AssetBundleInfo abInfo)
        {
            this.bundleInfo = null;
            this.state = EBundleLoadState.EState_None;
        }

        private IEnumerator LoadResource()
        {
            yield return new WaitForEndOfFrame();
            this.OnComplete();
        }
    }
#endif

    public class MobileAssetBundleLoader : AssetBundleLoader
    {
        protected int currentLoadingDepCount = 0;
        protected AssetBundle bundle = null;
        protected bool isError = false;
        protected byte[] bytes = null;
        protected string text = string.Empty;

        public string AssetBundleCachedFile = string.Empty;
        public string AssetBundleBuildInFile = string.Empty;

        public MobileAssetBundleLoader(Action<AssetBundleLoader> onManagerComplete, Action<AssetBundleLoader> onManagerError):
            base(onManagerComplete, onManagerError)
        {

        }

        public override void LoadAsync()
        {
            if (isError)
            {
                state = EBundleLoadState.EState_Error;
            }

            if (EBundleLoadState.EState_None == state)
            {
                state = EBundleLoadState.EState_Loading;
                LoadDependencies();
            }
            else if (EBundleLoadState.EState_Error == state)
            {
                OnError();
            }
            else if (EBundleLoadState.EState_Complete == state)
            {
                OnComplete();
            }
        }

        private void CreateDependenciesLoader()
        {
            dependLoaders.Clear();
            for (int index = 0; index < bundleData.dependencies.Count; ++index)
            {
                dependLoaders.Add(AssetBundleManager.Instance.CreateAssetBundleLoader(bundleData.dependencies[index], null, false , isAsync));
            }
        }

        public override void LoadSync()
        {
            if (null != bundleData)
            {
                CreateDependenciesLoader();

                currentLoadingDepCount = 0;
                for (int index = 0; index < dependLoaders.Count; ++index)
                {
                    AssetBundleLoader depLoader = dependLoaders[index];
                    if (!depLoader.IsComplete)
                    {
                        depLoader.LoadSync();
                    }
                }
            }

            state = EBundleLoadState.EState_Loading;
            CheckDepLoadCompleted();
        }

        public override void LoadBundleSync()
        {
            AssetBundleCachedFile = GameConstant.PersistentDataPath + bundleName;
            AssetBundleBuildInFile = GameConstant.StreamingAssetsPath + bundleName;

            if (File.Exists(AssetBundleCachedFile))
            {
                bundle = AssetBundle.LoadFromFile(AssetBundleCachedFile);
            }
            else
            {
                Logger.LogError(AssetBundleBuildInFile);
                bundle = AssetBundle.LoadFromFile(AssetBundleBuildInFile);
            }

            this.OnComplete();
        }

        public override void LoadBundleAsync()
        {
            AssetBundleCachedFile = GameConstant.PersistentDataPath + bundleName;
            AssetBundleBuildInFile = GameConstant.AssetBundleBasePath + bundleName;

            if (File.Exists(AssetBundleCachedFile))
            {
                AssetBundleManager.Instance.StartCoroutine(LoadFromCached());
            }
            else
            {
                AssetBundleManager.Instance.StartCoroutine(LoadFromBuild());
            }
        }

        protected virtual IEnumerator LoadFromCached()
        {
            if (EBundleLoadState.EState_Error != state)
            {
                AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(AssetBundleCachedFile);
                yield return req;
                if (null == req)
                {
                    Logger.LogError("Load from cached file failed!!! AssetBundleCachedFile = " + AssetBundleCachedFile);
                }
                else
                {
                    bundle = req.assetBundle;
                }

                this.OnComplete();
            }
        }

        protected virtual IEnumerator LoadFromBuild()
        {
            if (EBundleLoadState.EState_Error != state)
            {
                using (WWW www = new WWW(AssetBundleBuildInFile))
                {
                    yield return www;

                    if (null != www.error)
                    {
                        Logger.LogError("Load from build file failed AssetBundleBuildInFile = " + AssetBundleBuildInFile + "  " + www.error.ToString());
                    }
                    else
                    {
                        bundle = www.assetBundle;
                    }
                }

                if (null != bundle)
                {
                    this.OnComplete();
                }
            }
        }

        private void CheckDepLoadCompleted()
        {
            if (currentLoadingDepCount <= 0 && null != onDepLoadCompleted)
            {
                onDepLoadCompleted(this);
            }
        }

        private void LoadDependencies()
        {
            if (null != bundleData)
            {
                CreateDependenciesLoader();
                currentLoadingDepCount = 0;
                for (int index = 0; index < dependLoaders.Count; ++index)
                {
                    AssetBundleLoader depLoader = dependLoaders[index];
                    if (!depLoader.IsComplete)
                    {
                        ++currentLoadingDepCount;
                        depLoader.onLoadComplete += OnDepLoadCompleted;
                        depLoader.LoadAsync();
                    }
                }
            }

            CheckDepLoadCompleted();
        }

        private void OnDepLoadCompleted(AssetBundleInfo abInfo)
        {
            --currentLoadingDepCount;
            CheckDepLoadCompleted();
        }

        public override void OnComplete()
        {
            if (null == bundleInfo)
            {
                this.state = EBundleLoadState.EState_Complete;

                this.bundleInfo = CreateBundleInfo(null, bundle, bytes, text);
                this.bundleInfo.IsReady = true;
                this.bundleInfo.onUnloaded = OnBundleUnLoad;

                if (null != dependLoaders)
                {
                    for (int index = 0; index < dependLoaders.Count; ++index)
                    {
                        this.bundleInfo.AddDependency(dependLoaders[index].bundleInfo);
                    }
                }

                bundle = null;
            }

            base.OnComplete();
        }

        public override void OnError()
        {
            isError = true;
            this.state = EBundleLoadState.EState_Error;
            bundleInfo = null;
            base.OnError();
        }

        private void OnBundleUnLoad(AssetBundleInfo abInfo)
        {
            bundleInfo = null;
            this.state = EBundleLoadState.EState_None;
        }
    }

    public class DownloadAssetBundleLoader : MobileAssetBundleLoader
    {
        public string SourcePath = string.Empty;
        public string TargetPath = string.Empty;

        public DownloadAssetBundleLoader(Action<AssetBundleLoader> onManagerComplete, Action<AssetBundleLoader> onManagerError):
            base(onManagerComplete, onManagerError)
        {
        }

        public override void LoadBundleAsync()
        {
            AssetBundleManager.Instance.StartCoroutine(DownloadFile());
        }

        private IEnumerator DownloadFile()
        {
            if (EBundleLoadState.EState_Error != state)
            {
                using (WWW www = new WWW(SourcePath))
                {
                    yield return www;

                    if (null != www.error)
                    {
                        Logger.LogError("download file failed!!! source file = " + SourcePath);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(TargetPath))
                        {
                            DownloadUtil.WriteFile(www, TargetPath);
                        }
                        else
                        {
                            if (null == www.assetBundle)
                            {
                                text = www.text;
                            }

                            bytes = www.bytes;
                            bundle = www.assetBundle;
                        }
                    }
                }
                this.OnComplete();
            }
        }
    }
}