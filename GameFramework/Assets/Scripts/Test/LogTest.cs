using UnityEngine;

public class LogTest : MonoBehaviour 
{
    void Awake()
    {
        Logger.SetEnable(true);
    }

	// Use this for initialization
	void Start () 
    {
        Logger.Log("fafadfadf", this.gameObject);
        Logger.LogWarning("sdfsdfsdf", this.gameObject);
        Logger.LogError("sdfsdfsdfeeee", this.gameObject);
	}
	
	// Update is called once per frame
	void Update () 
    {
		
	}
}
