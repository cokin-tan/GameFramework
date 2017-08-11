using System;
using UnityEngine;

public class GameConstant
{
#if UNITY_IPHONE
    public const int TargetFrameRate = 45;
#else
    public const int TargetFrameRate = 30;
#endif

    public static string PersistentDataPath
    {
        get
        {
            return Application.persistentDataPath + "/";
        }
    }

    public static string StreamingAssetsPath
    {
        get
        {
            return Application.streamingAssetsPath + "/";
        }
    }

    public static string AssetBundleBasePath
    {
        get
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "jar:file://" + Application.dataPath + "!/assets/";
                case RuntimePlatform.IPhonePlayer:
                    return "file://" + Application.dataPath + "/Raw/";
                default:
                    return "file://" + Application.streamingAssetsPath + "/";
            }
        }
    }

    public static string PlatformDirectory
    {
        get
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                default:
                    return "Windows";
            }
        }
    }

    #region login url
    private static string loginUrl = string.Empty;
    public static string LoginUrl
    {
        get
        {
            return loginUrl;
        }
    }

    public static void RedirectLoginUrl(string url)
    {
        if(!string.IsNullOrEmpty(url))
        {
            loginUrl = url;
        }
    }
    #endregion

    #region app type
    public enum EAppType
    {
        EType_None,
        EType_Normal,
        EType_Test,
    }
    private static EAppType appType = EAppType.EType_Normal;
    public static EAppType AppType
    {
        get
        {
            return appType;
        }
    }

    public static void SetAppType(int type)
    {
        appType = (EAppType)type;
    }
    #endregion

    #region resource url
    public static string RemoteResourcePath
    {
        get
        {
            return string.Format("{0}{1}", RemotePlatformRootPath, ResourceVersion);
        }
    }

    public static string RemotePlatformRootPath
    {
        get
        {
            return string.Format("{0}{1}/", resourceUrl, PlatformDirectory);
        }
    }

    public static string ResourceVersion
    {
        get
        {
            return string.Format("v{0}.{1}.{2}/", DeviceInfo.MajorVersion, DeviceInfo.MinorVersion, DeviceInfo.BuildVersion);
        }
    }

    private static string resourceUrl = string.Empty;
    public static string ResourceUrl
    {
        get
        {
            return resourceUrl;
        }
    }

    public static void SetResourceUrl(string url)
    {
        Debug.Assert(!string.IsNullOrEmpty(url));
        resourceUrl = url;
    }
    #endregion
}
