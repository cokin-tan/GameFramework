using System.IO;
using UnityEngine;

public class DownloadTest : MonoBehaviour
{

	// Use this for initialization
	void Start () 
    {
        DownloadUtil.WriteFile(new byte[45], "E:/fuck/fuck.text");
        //File.WriteAllBytes("E:/fuck/fuck.text", new byte[45]);
	}
	
	// Update is called once per frame
	void Update () 
    {
		
	}
}
