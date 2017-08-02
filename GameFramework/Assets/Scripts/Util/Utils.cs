using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class Utils
{
    public static string FileToMd5(string filePath)
    {
        try
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retValue = md5.ComputeHash(fileStream);

                StringBuilder sb = new StringBuilder();
                for (int index = 0; index < retValue.Length; ++index)
                {
                    sb.Append(retValue[index].ToString("x2"));
                }

                return sb.ToString();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
            return string.Empty;
        }
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
}
