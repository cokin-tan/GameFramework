using System;
using UnityEngine;

public class UIBaseWindow : MonoBehaviour 
{
    public bool IsDefaultOpen = false;

    public int PageSlib = 0;

    public bool IsShow
    {
        get
        {
            return this.gameObject.activeSelf;
        }
    }

    void Awake()
    {
        WindowAwake();
    }

    void Start()
    {
        WindowStart();
    }

    void Update()
    {
        WindowUpdate();
    }

    void OnEnable()
    {
        WindowEnable();
    }

    void OnDisable()
    {
        WindowDisable();
    }

    void OnDestroy()
    {
        WindowDestroy();
    }

    protected virtual void WindowAwake()
    {
    }

    protected virtual void WindowStart()
    {
    }

    protected virtual void WindowUpdate()
    {
    }

    protected virtual void WindowEnable()
    {
    }

    protected virtual void WindowDisable()
    {
    }

    protected virtual void WindowDestroy()
    {
    }

    public virtual void ShowWindow(EventParam param = null, Action OnComplete = null)
    {
        Utils.SetActive(this.transform, true);

        OnShowWindow(param);

        if (null != OnComplete)
        {
            OpenAnimation(OnComplete);
        }
    }

    protected virtual void OnShowWindow(EventParam param)
    {

    }

    protected virtual void OpenAnimation(Action OnComplete)
    {
        if (null != OnComplete)
        {
            OnComplete();
        }
    }

    public virtual void HideWindow(Action OnComplete = null)
    {
        Utils.SetActive(this.transform, false);

        GameObject.Destroy(this.gameObject);

        if (null != OnComplete)
        {
            OnComplete();
        }
    }

    public virtual void MessageProc(uint msgId, EventParam param)
    {

    }
}
