using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance = null;
    private static bool isDestroy = false;

    static Singleton()
    {
        isDestroy = false;
    }

    void OnDestroy()
    {
        isDestroy = true;
    }

    public static T Instance
    {
        get
        {
            if (isDestroy)
            {
                return null;
            }

            if (null == instance)
            {
                instance = CreateSingleton();
            }

            return instance;
        }
    }

    private static T CreateSingleton()
    {
        T[] instances = Object.FindObjectsOfType<T>();
        if (instances.Length > 1)
        {
            Logger.LogError("Singleton error, must only one");
        }

        T inst = null;
        if (instances.Length <= 0)
        {
            GameObject root = GameObject.Find("Singletons");
            if (null == root)
            {
                root = new GameObject("Singletons");
                root.transform.Reset();
                DontDestroyOnLoad(root);
            }
            GameObject obj = new GameObject(typeof(T).ToString());
            obj.transform.SetParent(root);
            inst = obj.AddComponent<T>();
        }
        else
        {
            inst = instances[0];
        }

        inst.Initialize();

        return inst;
    }

    public virtual void Initialize()
    {

    }
}
