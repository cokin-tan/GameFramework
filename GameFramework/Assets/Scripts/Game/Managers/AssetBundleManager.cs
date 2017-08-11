using System;
using UnityEngine;
using System.Collections.Generic;

namespace FrameWork.Assets
{
    public class AssetBundleManager : Singleton<AssetBundleManager>
    {
        private const int MAX_LOAD_NUM = 5;
        private int remainLoadNum = MAX_LOAD_NUM;
        private bool isLoading = false;
        private Queue<AssetBundleLoader> waiteLoadQueue = new Queue<AssetBundleLoader>();
        private List<AssetBundleLoader> currentLoadingList = new List<AssetBundleLoader>();
        private HashSet<AssetBundleLoader> unCompleteLoadSet = new HashSet<AssetBundleLoader>();
        private HashSet<AssetBundleLoader> allLoadSet = new HashSet<AssetBundleLoader>();
        private Dictionary<string, AssetBundleInfo> loadedAssetBundles = new Dictionary<string, AssetBundleInfo>();

        private Dictionary<string, AssetBundleLoader> loaderCacheDic = new Dictionary<string, AssetBundleLoader>();

        public override void Initialize()
        {
            InvokeRepeating("CheckUnusedAssetBundle", 1, 5);
        }

        public void LoadAssetSync(string assetPath, Action<AssetBundleInfo> OnComplete, bool isMulti = false, object param = null)
        {
            string filePath = AssetBundlePathResolver.GetAssetPath(assetPath.ToLower());

            AssetBundleLoader bundleLoader = CreateAssetBundleLoader(filePath, param, isMulti, false);
            if (bundleLoader.IsComplete)
            {
                if (null != OnComplete)
                {
                    OnComplete(bundleLoader.bundleInfo);
                }
            }
            else
            {
                if (null != OnComplete)
                {
                    bundleLoader.onLoadComplete += OnComplete;
                }

                bundleLoader.LoadSync();
            }
        }

        public AssetBundleLoader LoadAssetAsync(string assetPath, Action<AssetBundleInfo> OnComplete, bool isMulti = false, object param = null)
        {
            string filePath = AssetBundlePathResolver.GetAssetPath(assetPath.ToLower());
            AssetBundleLoader bundleLoader = CreateAssetBundleLoader(filePath, param, isMulti, true);
            if (null == bundleLoader)
            {
                if (null != OnComplete)
                {
                    OnComplete(null);
                }
            }
            else
            {
                allLoadSet.Add(bundleLoader);
                if (bundleLoader.IsComplete)
                {
                    if (null != OnComplete)
                    {
                        bundleLoader.bundleInfo.param = param;

                        OnComplete(bundleLoader.bundleInfo);
                    }
                }
                else
                {
                    if (null != OnComplete)
                    {
                        bundleLoader.onLoadComplete += OnComplete;
                    }

                    if (bundleLoader.state < EBundleLoadState.EState_Loading)
                    {
                        unCompleteLoadSet.Add(bundleLoader);
                    }
                    StartLoadBundle();
                }
            }

            return bundleLoader;
        }

        public AssetBundleLoader DownloadAssetAsync(string sourcePath, string targetPath, Action<AssetBundleInfo> OnComplete, object param = null)
        {
            AssetBundleLoader bundleLoader = CreateDownloadLoader(sourcePath, targetPath, param);
            if (null == bundleLoader)
            {
                if (null != OnComplete)
                {
                    OnComplete(null);
                }
            }
            else
            {
                allLoadSet.Add(bundleLoader);
                if (bundleLoader.IsComplete)
                {
                    if (null != OnComplete)
                    {
                        bundleLoader.bundleInfo.param = param;

                        OnComplete(bundleLoader.bundleInfo);
                    }
                }
                else
                {
                    if (null != OnComplete)
                    {
                        bundleLoader.onLoadComplete += OnComplete;
                    }

                    if (bundleLoader.state < EBundleLoadState.EState_Loading)
                    {
                        unCompleteLoadSet.Add(bundleLoader);
                    }
                    StartLoadBundle();
                }
            }

            return bundleLoader;
        }

        public AssetBundleLoader CreateAssetBundleLoader(string assetFileName, object param, bool loadMulti = false, bool isAsync = false)
        {
            AssetBundleLoader bundleLoader = null;

            string abFile = assetFileName.ToLower();
            if (loaderCacheDic.ContainsKey(abFile))
            {
                bundleLoader = loaderCacheDic[abFile];
                bundleLoader.isAsync = isAsync;
                if (null != bundleLoader.bundleInfo)
                {
                    bundleLoader.bundleInfo.param = param;
                }
            }
            else
            {
                ConfResourceItem resItem = null;
#if UNITY_EDITOR && !AB_MODE
                bundleLoader = new EditorModeAssetBundleLoader(loadMulti, OnLoadComplete, OnLoadError);
#else
                resItem = ConfigManager.Instance.resourceConfig.GetResourceItem(abFile);
                bundleLoader = new MobileAssetBundleLoader(OnLoadComplete, OnLoadError);
                bundleLoader.onDepLoadCompleted = OnDepLoadComplete;
#endif
                bundleLoader.bundleData = resItem;
                bundleLoader.bundleName = abFile;
                bundleLoader.isAsync = isAsync;
                bundleLoader.param = param;
                loaderCacheDic.Add(abFile, bundleLoader);
            }
            return bundleLoader;
        }

        public AssetBundleLoader CreateDownloadLoader(string sourcePath, string targetPath, object param = null)
        {
            DownloadAssetBundleLoader bundleLoader = new DownloadAssetBundleLoader(OnLoadComplete, OnLoadError);

            bundleLoader.bundleName = sourcePath;
            bundleLoader.bundleData = null;
            bundleLoader.SourcePath = sourcePath;
            bundleLoader.TargetPath = targetPath;
            bundleLoader.param = param;
            bundleLoader.onDepLoadCompleted = OnDepLoadComplete;

            loaderCacheDic[sourcePath] = bundleLoader;

            return bundleLoader;
        }

        public void RemoveAllLoader()
        {
            this.StopAllCoroutines();
            currentLoadingList.Clear();
            waiteLoadQueue.Clear();

            var iterator = loadedAssetBundles.GetEnumerator();
            while(iterator.MoveNext())
            {
                iterator.Current.Value.Dispose();
            }
            loadedAssetBundles.Clear();
        }
        
        public void UnLoadUnusedAssetBundle(bool bForce)
        {
            if (!isLoading || bForce)
            {
                List<string> bundleNames = ListPool<string>.Get();
                bundleNames.AddRange(loadedAssetBundles.Keys);

                int unloadMax = 20;
                int unloadedCount = 0;
                bool hasUnusedBundle = false;

                for (int index = bundleNames.Count - 1; index >= 0 && unloadedCount < unloadMax; --index)
                {
                    string bundleName = bundleNames[index];
                    AssetBundleInfo bundleInfo = loadedAssetBundles[bundleName];

                    if (null == bundleInfo)
                    {
                        Logger.LogError("the bundle is null " + bundleName);
                    }

                    if(bundleInfo.IsUnused)
                    {
                        RemoveAssetBundle(bundleInfo);
                        ++unloadedCount;
                        hasUnusedBundle = true;
                    }
                }

                ListPool<string>.Release(bundleNames);

#if UNITY_EDITOR
                if (hasUnusedBundle)
                {
                    Resources.UnloadUnusedAssets();
                }
#endif
            }
        }

        public void RemoveAssetBundle(AssetBundleInfo abInfo)
        {
            abInfo.Dispose();
            loadedAssetBundles.Remove(abInfo.bundleName);
            loaderCacheDic.Remove(abInfo.bundleName);
        }

        private void CheckUnusedAssetBundle()
        {
            UnLoadUnusedAssetBundle(false);
        }

        private void StartLoadBundle()
        {
            if (unCompleteLoadSet.Count > 0)
            {
                isLoading = true;
                List<AssetBundleLoader> loaders = new List<AssetBundleLoader>();
                loaders.AddRange(unCompleteLoadSet);
                unCompleteLoadSet.Clear();

                var iterator = loaders.GetEnumerator();
                while(iterator.MoveNext())
                {
                    AssetBundleLoader loader = iterator.Current;
                    currentLoadingList.Add(loader);
                    loader.LoadAsync();
                }
            }
        }

        private void OnLoadComplete(AssetBundleLoader abLoader)
        {
            loadedAssetBundles[abLoader.bundleName] = abLoader.bundleInfo;

            ++remainLoadNum;
            currentLoadingList.Remove(abLoader);

            if (currentLoadingList.Count <= 0 && unCompleteLoadSet.Count <= 0)
            {
                isLoading = false;

                var iterator = allLoadSet.GetEnumerator();
                while(iterator.MoveNext())
                {
                    AssetBundleLoader loader = iterator.Current;
                    if (null != loader.bundleInfo)
                    {
                        loader.bundleInfo.ResetLifeTime();
                    }
                }

                allLoadSet.Clear();
            }
            else
            {
                CheckWaiteList();
            }
        }

        private void CheckWaiteList()
        {
            while(remainLoadNum > 0 && waiteLoadQueue.Count > 0)
            {
                LoadAssetBundle(waiteLoadQueue.Dequeue());
            }
        }

        private void OnLoadError(AssetBundleLoader abLoader)
        {
            OnLoadComplete(abLoader);
        }

        private void OnDepLoadComplete(AssetBundleLoader abLoader)
        {
            if (abLoader.isAsync)
            {
                if (remainLoadNum <= 0)
                {
                    remainLoadNum = 0;
                    waiteLoadQueue.Enqueue(abLoader);
                }
                else
                {
                    LoadAssetBundle(abLoader);
                }
            }
            else
            {
                LoadAssetBundle(abLoader);
            }
        }

        private void LoadAssetBundle(AssetBundleLoader abLoader)
        {
            if (!abLoader.IsComplete)
            {
                if (abLoader.isAsync)
                {
                    abLoader.LoadBundleAsync();
                }
                else
                {
                    abLoader.LoadBundleSync();
                }
            }

            if (abLoader.isAsync)
            {
                --remainLoadNum;
            }
        }
    }
}
