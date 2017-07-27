using UnityEngine;

public static class UnityEngineEx
{
    public static void Reset(this Transform tran)
    {
        tran.position = Vector3.zero;
        tran.rotation = Quaternion.identity;
        tran.localScale = Vector3.one;
    }

    public static void SetParent(this Transform tran, Transform parent)
    {
        tran.parent = parent;
        tran.Reset();
    }

    public static void SetParent(this Transform tran, GameObject parent)
    {
        tran.SetParent(null != parent ? parent.transform : null);
    }
}
