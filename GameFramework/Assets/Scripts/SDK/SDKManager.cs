using System;

public class SDKManager
{
    private static SDKManager instance = null;

    public static SDKManager Instance
    {
        get
        {
            if (null == instance)
            {
                instance = new SDKManager();
            }

            return instance;
        }
    }

    private string platform = "1";
    public string Platform
    {
        get
        {
            return platform;
        }
    }

    private SDKManager()
    {

    }
}
