using System.IO;
using UnityEngine;

public class DownloadUtil
{
    public static void CreateDirectory(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public static void WriteFile(WWW www, string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || null == www)
        {
            Logger.LogError("WriteFile input arguments is invalid");
            return;
        }

        WriteFile(www.bytes, filePath);
    }

    public static void WriteFile(byte[] bytes, string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || null == bytes)
        {
            Logger.LogError("WriteFile input arguments is invalid");
            return;
        }

        CreateDirectory(filePath);

        File.WriteAllBytes(filePath, bytes);
    }

    public static void WriteFileToPersistent(byte[] bytes, string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Logger.LogError("the file path is invalid");
            return;
        }

        Logger.LogError(Application.persistentDataPath + filePath);

        WriteFile(bytes, GameConstant.PersistentDataPath + filePath);
    }

    public static bool InstallAPK(string path)
    {
#if UNITY_ANDROID
        try
        {
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            string actionView = intentClass.GetStatic<string>("ACTION_VIEW");
            int flagActivityNewTask = intentClass.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent", actionView);

            AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", path);
            AndroidJavaClass Uri = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject UriObject = Uri.CallStatic<AndroidJavaObject>("fromFile", fileObject);

            intentObject.Call<AndroidJavaObject>("setDataAndType", UriObject, "application/vnd.android.package-archive");
            intentObject.Call<AndroidJavaObject>("addFlags", flagActivityNewTask);
            //intentObject.Call<AndroidJavaObject>("setClassName", "com.android.packageinstaller", "com.android.packageinstaller.PackageInstallerActivity");

            AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            currentActivity.Call("startActivity", intentObject);

            Logger.LogError("install new apk ok");
        }
        catch(System.Exception e)
        {
            Logger.LogError("install apk failed!!!" + e.Message + " ===== " + e.StackTrace);
            return false;
        }
#endif

        return true;
    }
}
