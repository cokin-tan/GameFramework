using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launch : MonoBehaviour 
{
    public bool LogEnable = true;
    
    void Start()
    {
        Logger.SetEnable(LogEnable);
    }
}
