using System.Collections.Generic;

public class EventParam
{
    private Dictionary<string, object> argsDic = new Dictionary<string, object>();

    public EventParam(params object[] args)
    {
        if (0 != args.Length % 2)
        {
            Logger.LogError("the args length is invalid!!!");
            return;
        }

        for (int index = 0; index < args.Length; index += 2)
        {
            argsDic[(string)args[index]] = args[index + 1];
        }
    }

    public void ModifyParam(EventParam param)
    {
        List<string> keys = new List<string>(param.argsDic.Keys);
        for (int index = 0; index < keys.Count; ++index)
        {
            argsDic[keys[index]] = param.argsDic[keys[index]];
        }
    }

    public bool IsExists(string key)
    {
        return argsDic.ContainsKey(key);
    }

    public object GetValue(string key)
    {
        object obj = null;
        argsDic.TryGetValue(key, out obj);
        return obj;
    }

    public void Clear()
    {
        argsDic.Clear();
    }

    public object this[string key]
    {
        get
        {
            return GetValue(key);
        }
    }
}
