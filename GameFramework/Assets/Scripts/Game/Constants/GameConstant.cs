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
}
