using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launch : MonoBehaviour 
{
    public bool LogEnable = true;
    
    void Start()
    {
        Logger.SetEnable(LogEnable);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = GameConstant.TargetFrameRate;

        ConfigManager.Instance.InitConfData();

        UIManager.Instance.ChangeRootWindow(GameResourceDefine.UIROOT_LOGIN);

        StartCoroutine(Test());
    }

    IEnumerator Test()
    {
        yield return new WaitForSeconds(2.0f);

        GameObject obj = null;
        
        FrameWork.Assets.AssetBundleManager.Instance.LoadAssetAsync(GameResourceDefine.UI_PAGE_TEST + ".ui", (o) => 
        {
            obj = o.Instantiate();
        });

        yield return new WaitForSeconds(3.0f);

        GameObject.Destroy(obj);
    }
}
