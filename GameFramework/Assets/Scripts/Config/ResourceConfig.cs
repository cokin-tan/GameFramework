using System.Collections.Generic;
using UnityEngine;

public class ConfResourceItem
{
    public string file = string.Empty;
    public int size = 0;
    public string md5 = string.Empty;
    public int assetType = 0;
    public List<string> dependencies = new List<string>();
}

public class ResourceConfig : IBaseConfig
{
    public Dictionary<string, ConfResourceItem> resourceDic = new Dictionary<string, ConfResourceItem>();

    public class ResourceData
    {
        public string hashValue = string.Empty;

        public List<ConfResourceItem> patchLst = new List<ConfResourceItem>();

        public override bool Equals(object obj)
        {
            ResourceData data = obj as ResourceData;
            if (null == data)
            {
                return false;
            }
            else
            {
                return hashValue == data.hashValue;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public ResourceData resource = new ResourceData();

    public void Initialize(string json)
    {
        resource = Pathfinding.Serialization.JsonFx.JsonReader.Deserialize<ResourceData>(json);
        InitData();
    }

    private void InitData()
    {
        resourceDic.Clear();
        for (int index = 0; index < resource.patchLst.Count; ++index)
        {
            ConfResourceItem item = resource.patchLst[index];
            if (string.IsNullOrEmpty(item.file))
            {
                Logger.LogError("the resource file name is empty");
                continue;
            }

            if (resourceDic.ContainsKey(item.file))
            {
                Logger.LogError("the file is exists file = " + item.file);
                continue;
            }

            resourceDic.Add(item.file, item);
        }
    }

    public bool ContainFile(string file)
    {
        return resourceDic.ContainsKey(file);
    }

    public ConfResourceItem GetResourceItem(string fileName)
    {
        ConfResourceItem resItem = null;
        resourceDic.TryGetValue(fileName, out resItem);
        return resItem;
    }
}
