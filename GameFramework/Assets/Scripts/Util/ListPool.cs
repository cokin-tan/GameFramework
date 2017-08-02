using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListPool<T> where T : class
{
    private static readonly ObjectPool<List<T>> listPool = new ObjectPool<List<T>>(null, (list) => { list.Clear(); });

    public static List<T> Get()
    {
        return listPool.Get();
    }

    public static void Release(List<T> list)
    {
        listPool.Release(list);
    }
}
