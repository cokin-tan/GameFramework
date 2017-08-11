using System.Collections.Generic;
using UnityEngine;
using FrameWork.Assets;
using System;

public class UIManager : Singleton<UIManager>
{
    private Dictionary<string, UIBaseWindow> windowDic = new Dictionary<string, UIBaseWindow>();
    private List<UIBaseWindow> windowLst = new List<UIBaseWindow>();
    private GameObject uiRoot = null;
    private Transform windowsParent = null;
    private Camera uiCamera = null;
    public Camera UICamera
    {
        get
        {
            if (null == uiCamera)
            {
                uiCamera = Utils.FindChild<Camera>(uiRoot, "UICamera");
            }

            return uiCamera;
        }
    }

    private GameObject LoadGameObject(string objName)
    {
        GameObject obj = null;

        if (objName == GameResourceDefine.UIROOT_DOWNLOAD)
        {
            obj = Resources.Load<GameObject>("ui/" + objName.Substring(0, objName.LastIndexOf(".")));
            obj = GameObject.Instantiate<GameObject>(obj);
        }
        else
        {
            AssetBundleManager.Instance.LoadAssetSync(objName, (item) =>
            {
                obj = item.Instantiate();
            });
        }

        return obj;
    }

    private bool LoadRootWindow(string rootName)
    {
        if (null != uiRoot)
        {
            GameObject.DestroyImmediate(uiRoot);
            uiRoot = null;
            uiCamera = null;
            windowsParent = null;
        }

        uiRoot = LoadGameObject(rootName);

        return null != uiRoot;
    }

    private void InitRootWindowConfig()
    {
        windowsParent = Utils.FindChild(uiRoot, "Canvas/windows_parent");
        if (null == windowsParent)
        {
            Logger.LogError("find windows parent node failed!!! uiroot name = " + uiRoot.name);
        }

        UIBaseWindow[] windows = windowsParent.GetComponentsInChildren<UIBaseWindow>(true);
        for (int index = 0; index < windows.Length; ++index)
        {
            UIBaseWindow window = windows[index];

            windowDic[window.name] = window;
            windowLst.Add(window);

            if (window.IsDefaultOpen)
            {
                window.ShowWindow();
            }
        }
    }
    
    public void ChangeRootWindow(string rootName)
    {
        windowDic.Clear();
        windowLst.Clear();

        if (!LoadRootWindow(rootName))
        {
            Logger.LogErrorFormat("load ui window failed !!! root name = {0}", rootName);
            return;
        }

        InitRootWindowConfig();
    }

    private UIBaseWindow GetWindow(string windowName)
    {
        UIBaseWindow window = null;
        windowDic.TryGetValue(windowName, out window);
        return window;
    }

    private void HandleWindowRenderOrder()
    {
        windowLst.Sort((lhs, rhs) => { return lhs.PageSlib.CompareTo(rhs.PageSlib); });
        for (int index = 0; index < windowLst.Count; ++index)
        {
            UIBaseWindow window = windowLst[index];
            window.transform.SetSiblingIndex(index);
        }
    }

    private UIBaseWindow LoadWindow(string windowName)
    {
        UIBaseWindow window = GetWindow(windowName);
        if (null == window)
        {
            GameObject uiObj = LoadGameObject(windowName + ".ui");
            if (null == uiObj)
            {
                Logger.LogError("load ui object failed!!! name = " + windowName);
                return null;
            }

            window = uiObj.GetComponent<UIBaseWindow>();
            windowDic[windowName] = window;
            windowLst.Add(window);

            if (null != windowsParent)
            {
                uiObj.SetParent(windowsParent);

                RectTransform rect = uiObj.transform as RectTransform;
                if (null != rect)
                {
                    rect.offsetMax = Vector2.zero;
                    rect.offsetMin = Vector2.zero;
                }
            }
            else
            {
                Debug.LogError("the window parent node is null");
            }

            HandleWindowRenderOrder();
        }

        return window;
    }

    public void ShowWindow(string windowName, EventParam param, Action OnComplete)
    {
        UIBaseWindow window = LoadWindow(windowName);
        if (null != window)
        {
            window.ShowWindow(param, OnComplete);
        }
    }

    public void hideWindow(string windowName, Action OnComplete)
    {
        UIBaseWindow window = GetWindow(windowName);
        if (null == window)
        {
            return;
        }

        window.HideWindow(() => 
        {
            windowDic.Remove(windowName);
            windowLst.Remove(window);
            if (null != OnComplete)
            {
                OnComplete();
            }
        });
    }

    public bool IsWindowOpen(string windowName)
    {
        UIBaseWindow window = null;
        if (windowDic.TryGetValue(windowName, out window))
        {
            return window.IsShow;
        }
        return false;
    }

    public void MessageProc(uint msgId, EventParam param)
    {
        for (int index = 0; index < windowLst.Count; ++index)
        {
            if (windowLst[index].IsShow)
            {
                windowLst[index].MessageProc(msgId, param);
            }
        }
    }
}
