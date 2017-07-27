using UnityEngine;

public static class Logger 
{
    private static bool IsEnable
    {
        get;
        set;
    }

    public static void SetEnable(bool isEnable)
    {
        IsEnable = isEnable;
    }

    public static void Log(string message, GameObject context = null)
    {
        if (IsEnable)
        {
            Debug.Log(message, context);
        }
    }

    public static void LogFormat(string format, params object[] args)
    {
        if (IsEnable)
        {
            Debug.LogFormat(format, args);
        }
    }

    public static void LogFormat(GameObject context, string format, params object[] args)
    {
        if (IsEnable)
        {
            Debug.LogFormat(context, format, args);
        }
    }

    public static void LogWarning(string message, GameObject context = null)
    {
        if (IsEnable)
        {
            Debug.LogWarning(message, context);
        }
    }

    public static void LogWarningFormat(string format, params object[] args)
    {
        if (IsEnable)
        {
            Debug.LogWarningFormat(format, args);
        }
    }

    public static void LogWarningFormat(GameObject context, string format, params object[] args)
    {
        if (IsEnable)
        {
            Debug.LogWarningFormat(context, format, args);
        }
    }

    public static void LogError(string message, GameObject context = null)
    {
        if (IsEnable)
        {
            Debug.LogError(message, context);
        }
    }

    public static void LogErrorFormat(string format, params object[] args)
    {
        if (IsEnable)
        {
            Debug.LogErrorFormat(format, args);
        }
    }

    public static void LogErrorFormat(GameObject context, string format, params object[] args)
    {
        if (IsEnable)
        {
            Debug.LogErrorFormat(context, format, args);
        }
    }
}
