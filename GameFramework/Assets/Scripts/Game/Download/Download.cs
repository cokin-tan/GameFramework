using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FrameWork.Assets;

public class Download : MonoBehaviour
{
    public string verfilePath
    {
        get
        {
            return GameConstant.AssetBundleBasePath + "data/conf/verfile.conf";
        }
    }

    private class ResourceItem
    {
        public string Url = string.Empty;
        public int Timeout = 10;
    }

    private class ClientVersionConfig
    {
        public string GameVersion = string.Empty;
        public List<ResourceItem> ResourceUrls = new List<ResourceItem>();
    }

    private class PlatformConfig
    {
        public string Platform = string.Empty;
        public int PackageSize = 0;
        public int AppType = 0;
        public string ApkDownloadUrl = string.Empty;
        public string WebDownloadUrl = string.Empty;
        public string LoginUrlRedirect = string.Empty;
        public bool ForceUpdate = false;

        public override string ToString()
        {
            return string.Format("Platform = {0} PackageSize = {1} AppType = {2} ApkDownloadUrl = {3} WebDownloadUrl = {4} LoginUrlRedirect = {5} ForceUpdate = {6}", 
                Platform, PackageSize, AppType, ApkDownloadUrl, WebDownloadUrl, LoginUrlRedirect, ForceUpdate);
        }
    }

    private class ServerVersionConfig
    {
        public List<PlatformConfig> Platforms = new List<PlatformConfig>();

        public PlatformConfig GetPlatformConfig(string platform)
        {
            return Platforms.Find(item => { return item.Platform == platform; });
        }
    }

    private PlatformConfig currentPlatform = null;

    private ClientVersionConfig clientVersionConfig = new ClientVersionConfig();
    private ServerVersionConfig serverVersionConfig = new ServerVersionConfig();

    private ResourceConfig clientResourceConfig = new ResourceConfig();
    private ResourceConfig serverResourceConfig = new ResourceConfig();

    private void Start()
    {
        StartCoroutine(StartCheckResource());
    }

    private IEnumerator StartCheckResource()
    {
        if (!Utils.IsNetAvailable)
        {
            Logger.LogError("The net work is not available!!!");
        }

        while(!Utils.IsNetAvailable)
        {
            yield return new WaitForSeconds(2.0f);
        }

        yield return StartCoroutine(StartRequestResource());
    }

    private IEnumerator StartRequestResource()
    {
        TextAsset text = Resources.Load<TextAsset>("version");
        if (null == text)
        {
            Logger.LogError("load versoin text failed!!!");
            yield break;
        }
        else
        {
            clientVersionConfig = Pathfinding.Serialization.JsonFx.JsonReader.Deserialize<ClientVersionConfig>(text.text);
            if (null == clientVersionConfig)
            {
                Logger.LogError("deserialize client version text failed!!!");
                yield break;
            }
        }

        DeviceInfo.SetGameVersion(clientVersionConfig.GameVersion);
        string versionFileName = string.Format("version_{0}.json", DeviceInfo.GameVersion);
        for (int index = 0; index < clientVersionConfig.ResourceUrls.Count; ++index)
        {
            ResourceItem item = clientVersionConfig.ResourceUrls[index];
            string requestUrl = string.Format("{0}{1}/{2}", item.Url, GameConstant.PlatformDirectory, versionFileName);
            string platformInfos = string.Empty;
            AssetBundleManager.Instance.DownloadAssetAsync(requestUrl, null, (abInfo) =>
            {
                platformInfos = abInfo.text;
            });
            float startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < item.Timeout)
            {
                if (!string.IsNullOrEmpty(platformInfos))
                {
                    try
                    {
                        serverVersionConfig = Pathfinding.Serialization.JsonFx.JsonReader.Deserialize<ServerVersionConfig>(platformInfos);
                        if (null != serverVersionConfig)
                        {
                            GameConstant.SetResourceUrl(item.Url);
                            break;
                        }
                    }
                    catch
                    {
                        Logger.LogError("deserialize server version config failed!!!");
                    }
                }
                yield return null;
            }

            if (null != serverVersionConfig)
            {
                break;
            }
        }

        yield return StartCoroutine(SetPlatformConfig());
    }

    private IEnumerator SetPlatformConfig()
    {
        if (null == serverVersionConfig)
        {
            yield break;
        }

        currentPlatform = serverVersionConfig.GetPlatformConfig(SDKManager.Instance.Platform);
        if (null == currentPlatform)
        {
            Logger.LogError("get current platform config failed platform = " + SDKManager.Instance.Platform);
            yield break;
        }
        GameConstant.RedirectLoginUrl(currentPlatform.LoginUrlRedirect);
        GameConstant.SetAppType(currentPlatform.AppType);

        yield return StartCoroutine(CheckApkUpdate());
    }

    private IEnumerator CheckApkUpdate()
    {
        bool needUpdate = !string.IsNullOrEmpty(currentPlatform.ApkDownloadUrl);
        if (needUpdate)
        {
            string apkPath = string.Format("{0}main_fuck_{1}_{2}.apk", GameConstant.PersistentDataPath, DeviceInfo.GameVersion, SDKManager.Instance.Platform);
            if (File.Exists(apkPath))
            {
                DownloadUtil.InstallAPK(apkPath);
            }
            else
            {
                if (!string.IsNullOrEmpty(currentPlatform.WebDownloadUrl))
                {
                    Application.OpenURL(currentPlatform.WebDownloadUrl);
                }
                else
                {
                    yield return StartCoroutine(DownLoadApk(currentPlatform.ApkDownloadUrl, apkPath));
                }
            }
        }
        else
        {
            yield return StartCoroutine(CheckAsset());
        }
    }

    private IEnumerator DownLoadApk(string sourcePath, string destPath)
    {
        using (WWW www = new WWW(sourcePath))
        {
            yield return www;
            if (null != www.error)
            {
                Logger.LogError("down load apk failed source path = " + sourcePath);
                yield break;
            }

            DownloadUtil.WriteFile(www.bytes, destPath);
        }

        DownloadUtil.InstallAPK(destPath);
    }

    private IEnumerator CheckAsset()
    {
        string verfileMD5 = string.Empty;
        using (WWW www = new WWW(verfilePath))
        {
            yield return www;
            if (null != www.error)
            {
                Logger.LogError("load verfile failed");
                yield break;
            }
            verfileMD5 = Utils.HashToMd5(www.bytes);
        }

        if (Utils.GetString("verfile_md5") != verfileMD5)
        {
            yield return StartCoroutine(StartUnzipAsset());
            Utils.SetString("verfile_md5", verfileMD5);
        }
        else
        {
            yield return StartCoroutine(CheckAssetUpdate());
        }
    }

    private IEnumerator StartUnzipAsset()
    {
        ResourceConfig resourceConfig = new ResourceConfig();
        using (WWW www = new WWW(verfilePath))
        {
            yield return www;
            if(null != www.error)
            {
                Logger.LogError("load verfile failed");
                yield break;
            }
            TextAsset verfileText = www.assetBundle.LoadAsset<TextAsset>("verfile");
            resourceConfig.Initialize(verfileText.text);
            www.assetBundle.Unload(false);
        }

        List<KeyValuePair<string, string>> downLoadList = new List<KeyValuePair<string, string>>();
        for (int index = 0; index < resourceConfig.resource.patchLst.Count; ++index)
        {
            ConfResourceItem item = resourceConfig.resource.patchLst[index];
            downLoadList.Add(new KeyValuePair<string, string>(GameConstant.AssetBundleBasePath + item.file, GameConstant.PersistentDataPath + item.file));
        }

        int fileCount = 0;
        for (int index = 0; index < downLoadList.Count; ++index)
        {
            KeyValuePair<string, string> item = downLoadList[index];
            AssetBundleManager.Instance.DownloadAssetAsync(item.Key, item.Value, (abInfo) =>
            {
                ++fileCount;
                //Logger.LogError("unzip progress = " + (float)fileCount / downLoadList.Count);
            });
        }

        while (fileCount < downLoadList.Count)
        {
            yield return null;
        }

        Logger.LogError("unzip complete");
        yield return StartCoroutine(CheckAssetUpdate());
    }

    private string GetAssetBundleText(AssetBundleInfo abInfo)
    {
        if (null == abInfo)
        {
            return string.Empty;
        }
        else
        {
            TextAsset asset = abInfo.Require(null) as TextAsset;
            if (null != asset && !string.IsNullOrEmpty(asset.text))
            {
                return asset.text;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    private string waiteString = null;
    private IEnumerator WaiteRequest()
    {
        waiteString = null;
        float startRequestTime = Time.realtimeSinceStartup;
        while (null == waiteString && (Time.realtimeSinceStartup - startRequestTime) < 5.0f)
        {
            yield return null;
        }
    }

    private IEnumerator CheckAssetUpdate()
    {
        #region load local update list config
        string clientList = null;
        AssetBundleInfo bundleInfo = null;
        AssetBundleManager.Instance.LoadAssetAsync("updatelist.conf", (abInfo) => 
        {
            waiteString = GetAssetBundleText(abInfo);
            bundleInfo = abInfo;
        });

        yield return StartCoroutine(WaiteRequest());
        if (null != bundleInfo)
        {
            AssetBundleManager.Instance.RemoveAssetBundle(bundleInfo);
        }
        clientList = waiteString;
        if (string.IsNullOrEmpty(clientList))
        {
            Logger.LogError("load client update list failed!!!");
            yield break;
        }
        clientResourceConfig.Initialize(clientList);
        #endregion

        #region load server update list config
        string serverList = null;
        byte[] serverBytes = null;
        AssetBundleManager.Instance.DownloadAssetAsync(GameConstant.RemoteResourcePath + GameResourceDefine.ASSET_UPDATE_FILE, null, (abInfo) => 
        {
            waiteString = GetAssetBundleText(abInfo);
            serverBytes = abInfo.bytes;
        });
        yield return StartCoroutine(WaiteRequest());
        serverList = waiteString;
        #endregion

        if (!string.IsNullOrEmpty(serverList))
        {
            serverResourceConfig.Initialize(serverList);
            List<ConfResourceItem> updateList = new List<ConfResourceItem>();
            int totalSize = 0;

            if (!clientResourceConfig.resource.Equals(serverResourceConfig.resource))
            {
                for (int index = 0; index < serverResourceConfig.resource.patchLst.Count; ++index)
                {
                    var item = serverResourceConfig.resource.patchLst[index];
                    var clientItem = clientResourceConfig.GetResourceItem(item.file);
                    if (null == clientItem || clientItem.md5 != item.md5)
                    {
                        if (Utils.FileToMd5(GameConstant.PersistentDataPath + item.file) != item.md5)
                        {
                            totalSize += item.size;
                            updateList.Add(item);
                        }
                    }
                    Logger.LogError("check resource is valid");
                }

                if (updateList.Count > 0 && totalSize > 0)
                {
                    yield return StartCoroutine(DownloadAsset(updateList, serverBytes));
                }
                else
                {
                    // todo check update finish
                }
            }
            else
            {
                Logger.LogError("the resouce config is equal");
            }
        }
        else
        {
            // todo check update finish
            Logger.LogError("don't has file to update");
        }
    }

    private IEnumerator DownloadAsset(List<ConfResourceItem> updatelist, byte[] serverBytes)
    {
        int totalCount = 0;
        bool isError = false;
        for(int index = 0; index < updatelist.Count; ++index)
        {
            var item = updatelist[index];
            AssetBundleManager.Instance.DownloadAssetAsync(GameConstant.RemoteResourcePath + item.file, GameConstant.PersistentDataPath + item.file, (abInfo) => 
            {
                ++totalCount;
                if (Utils.FileToMd5(GameConstant.PersistentDataPath + item.file) != item.md5)
                {
                    isError = true;
                    Logger.LogError("download file " + item.file + " md5 is not matched");
                }
            });
        }

        while(totalCount < updatelist.Count)
        {
            yield return null;
        }

        if (isError)
        {
            Logger.LogError("some file download failed. please restart application to redownload");
        }
        else
        {
            DownloadUtil.WriteFileToPersistent(serverBytes, GameResourceDefine.ASSET_UPDATE_FILE);
        }
    }
}
