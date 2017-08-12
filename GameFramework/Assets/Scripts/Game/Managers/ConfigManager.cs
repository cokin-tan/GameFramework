using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FrameWork.Assets;

public class ConfigManager : Singleton<ConfigManager>
{
    public ResourceConfig resourceConfig = new ResourceConfig();

    private Dictionary<string, IBaseConfig> configLst = new Dictionary<string, IBaseConfig>();

    public override void Initialize()
    {
        configLst.Add("verfile.conf", resourceConfig);
    }

    private void ReadConfData(string fileName, IBaseConfig conf)
    {
#if UNITY_EDITOR && !AB_MODE
        using (StreamReader sr = new StreamReader(Path.Combine(Application.dataPath, AssetBundlePathResolver.GetAssetPath(fileName))))
        {
            conf.Initialize(sr.ReadToEnd());
        }
#else
        AssetBundleManager.Instance.LoadAssetSync(fileName, (abInfo) => 
        {
            TextAsset asset = abInfo.Require(null) as TextAsset;
            if (null != asset)
            {
                conf.Initialize(asset.text);
            }
        });
#endif

    }

    public void InitConfData()
    {
        foreach(var item in configLst)
        {
            ReadConfData(item.Key, item.Value);
        }
    }
}
