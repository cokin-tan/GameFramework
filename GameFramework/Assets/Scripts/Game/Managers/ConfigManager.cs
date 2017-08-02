using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConfigManager : Singleton<ConfigManager>
{
    public ResourceConfig resourceConfig = new ResourceConfig();

    private Dictionary<string, IBaseConfig> configLst = new Dictionary<string, IBaseConfig>();

    public override void Initialize()
    {
        configLst.Add("data/conf/verfile.json", resourceConfig);
    }

    private void ReadConfData(string fileName, IBaseConfig conf)
    {
        using (StreamReader sr = new StreamReader(Path.Combine(Application.dataPath, fileName)))
        {
            conf.Initialize(sr.ReadToEnd());
        }
    }

    public void InitConfData()
    {
        foreach(var item in configLst)
        {
            ReadConfData(item.Key, item.Value);
        }
    }
}
