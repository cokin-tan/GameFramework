using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launch : MonoBehaviour 
{
    public bool LogEnable = true;
    
    void Awake()
    {
        gameObject.AddComponent<DebugConsole>();
    }

    void Start()
    {
        Logger.SetEnable(LogEnable);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = GameConstant.TargetFrameRate;

        StartCoroutine(Test());
    }

    IEnumerator Test()
    {
        yield return new WaitForSeconds(2.0f);
        //ConfigManager.Instance.InitConfData();

        UIManager.Instance.ChangeRootWindow(GameResourceDefine.UIROOT_DOWNLOAD);

        yield return new WaitForSeconds(3.0f);
    }
}
