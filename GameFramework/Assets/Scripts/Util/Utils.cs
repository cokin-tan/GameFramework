using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class Utils
{
    #region local storage
    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }
    public static string GetString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }
    #endregion

    public static string FileToMd5(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    using (System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                    {
                        byte[] retValue = md5.ComputeHash(fileStream);

                        StringBuilder sb = new StringBuilder();
                        for (int index = 0; index < retValue.Length; ++index)
                        {
                            sb.Append(retValue[index].ToString("x2"));
                        }

                        return sb.ToString();
                    }
                }
            }
            else
            {
                return string.Empty;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
            return string.Empty;
        }
    }

    public static string HashToMd5(byte[] bytes)
    {
        using (System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
        {
            byte[] retValue = md5.ComputeHash(bytes);

            StringBuilder sb = new StringBuilder();
            for (int index = 0; index < retValue.Length; ++index)
            {
                sb.Append(retValue[index].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    public static string HashToMd5(string str)
    {
        return HashToMd5(Encoding.UTF8.GetBytes(str));
    }

    public static T FindChild<T>(Transform tran, string name) where T : Component
    {
        if (null != tran)
        {
            Transform child = tran.Find(name);
            if (null != child)
            {
                return child.gameObject.GetComponent<T>();
            }
            else
            {
                Logger.LogError("find child failed!!! name = " + name);
            }
        }
        else
        {
            Logger.LogError("the parent transform is null");
        }

        return null;
    }

    public static T FindChild<T>(GameObject obj, string name) where T : Component
    {
        if (null != obj)
        {
            return FindChild<T>(obj.transform, name);
        }
        else
        {
            Logger.LogError("the parent object is null");
            return null;
        }
    }


    public static Transform FindChild(Transform tran, string name)
    {
        return FindChild<Transform>(tran, name);
    }

    public static Transform FindChild(GameObject obj, string name)
    {
        return FindChild<Transform>(obj, name);
    }

    public static void SetActive<T>(T obj, bool isActive) where T : Component
    {
        if (null != obj)
        {
            obj.gameObject.SetActive(isActive);
        }
    }

    public static bool IsNetAvailable
    {
        get
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }

    public static bool IsWifi
    {
        get
        {
            return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
        }
    }

    public static bool IsGPRS
    {
        get
        {
            return Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;
        }
    }
}
