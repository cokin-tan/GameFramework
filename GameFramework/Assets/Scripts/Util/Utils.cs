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
}
