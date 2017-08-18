using System;

namespace Test
{
    public partial class TestMessage
    {
        public static TestMessage Create<T>(T msg) where T : class
        {
            return Create<T>(typeof(T), msg);
        }

        private static TestMessage Create<T>(Type t, T msg)
        {
            ID msgId = GetID(t);
            if (msgId == ID.ID_UNKNOW)
            {
                Logger.LogError("parse message id failed!!!");
                return null;
            }
            else
            {
                TestMessage result = new TestMessage();
                try
                {
                    string propertyName = t.Name.ToLower();
                    typeof(TestMessage).GetProperty(propertyName).SetValue(result, msg, null);
                    return result;
                }
                catch(Exception ex)
                {
                    Logger.LogError("set message attribute failed!!!" + ex.Message.ToString());
                    return null;
                }
            }
        }

        public static ID GetID(Type t)
        {
            string idName = t.Name.ToUpper();
            return (ID)((int)Enum.Parse(typeof(ID), idName));
        }

        public T GetValue<T>() where T : class
        {
            return this.GetValue<T>(typeof(T));
        }

        public T GetValue<T>(Type t) where T : class
        {
            ID msgId = GetID(t);
            if (msgId == ID.ID_UNKNOW)
            {
                Logger.LogError("parse message id failed!!!");
                return null;
            }
            else
            {
                try
                {
                    string propertyName = t.Name.ToLower();
                    T obj = typeof(TestMessage).GetProperty(propertyName).GetValue(this, null) as T;
                    if (null == obj)
                    {
                        Logger.LogError("get value failed!!!" + propertyName);
                    }
                    return obj;
                }
                catch (Exception ex)
                {
                    Logger.LogError("set message attribute failed!!!" + ex.Message.ToString());
                    return null;
                }
            }
        }

        public object GetValue(ID msgId)
        {
            try
            {
                string propertyName = msgId.ToString().ToLower();
                object obj = typeof(TestMessage).GetProperty(propertyName).GetValue(this, null);
                if (null == obj)
                {
                    Logger.LogError("get value failed!!!" + propertyName);
                }
                return obj;
            }
            catch (Exception ex)
            {
                Logger.LogError("set message attribute failed!!!" + ex.Message.ToString());
                return null;
            }
        }
    }
}