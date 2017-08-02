using System;
using System.Collections.Generic;

public class ObjectPool<T> where T : new() 
{
    private Stack<T>      poolStack = new Stack<T>();
    private Action<T>     onGetAction = null;
    private Action<T>     onReleaseAction = null;

    public int AllCount { get; private set; }
    public int UsedCount { get { return AllCount - poolStack.Count; } }
    public int UnUsedCount { get { return poolStack.Count; } }

    public ObjectPool(Action<T> getAction, Action<T> releaseAction)
    {
        onGetAction = getAction;
        onReleaseAction = releaseAction;
    }

    public T Get()
    {
        T element = default(T);
        if (poolStack.Count <= 0)
        {
            ++AllCount;
            element = new T();
        }
        else
        {
            element = poolStack.Pop();
        }

        if (null != onGetAction)
        {
            onGetAction(element);
        }

        return element;
    }

    public void Release(T element)
    {
        if (poolStack.Count > 0 && ReferenceEquals(element, poolStack.Peek()))
        {
            Logger.LogError("the try to release element is already in pool");
        }
        if (null != onReleaseAction)
        {
            onReleaseAction(element);
        }

        poolStack.Push(element);

    }
}
