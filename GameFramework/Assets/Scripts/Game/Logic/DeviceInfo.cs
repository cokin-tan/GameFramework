using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviceInfo
{
    public static int MajorVersion
    {
        get;
        private set;
    }
    public static int MinorVersion
    {
        get;
        private set;
    }
    public static int BuildVersion
    {
        get;
        private set;
    }

    public static void SetGameVersion(string version)
    {
        string[] versionArr = version.Split(new char[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (versionArr.Length != 3)
        {
            Logger.LogError("The game version is invalid!!! version = " + version);
            return;
        }

        try
        {
            MajorVersion = int.Parse(versionArr[0]);
            MinorVersion = int.Parse(versionArr[1]);
            BuildVersion = int.Parse(versionArr[2]);
        }
        catch
        {
            Logger.LogError("try to parse version code failed!");
        }
    }

    public static string GameVersion
    {
        get
        {
            return string.Format("V{0}.{1}.{2}", MajorVersion, MinorVersion, BuildVersion);
        }
    }
}
