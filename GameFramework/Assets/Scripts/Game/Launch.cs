using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FrameWork.TcpClient;

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

        StartCoroutine(TestIenum());
    }

    void OnDistroy()
    {
    }

    void OnDisable()
    {
        if (null != messenger)
        {
            messenger.Close();
        }
    }

    GameMessenger messenger = null;

    IEnumerator TestIenum()
    {
        yield return new WaitForSeconds(2.0f);

        messenger = new GameMessenger("192.168.1.158", 8192);
        messenger.ConnectAsync();

        while(!messenger.IsConnected)
        {
            yield return null;
        }

        Test.TestMessage.CS_TestMessage1 msg = new Test.TestMessage.CS_TestMessage1()
        {
            id = 1,
            context = "fuck"
        };
        messenger.SendMessage(msg);
        //UIManager.Instance.ChangeRootWindow(GameResourceDefine.UIROOT_DOWNLOAD);

    }

    void Update()
    {
        if (null != messenger)
        {
            messenger.Update();
        }
    }
}
