using System.Collections;
using System.Collections.Generic;
using FrameWork.TcpClient;
using Test;

internal class GameMessenger : MessageClient<TestMessage>
{
    internal GameMessenger(string ip, int port) 
        : base(ip, port)
    {
    }

    public void SendMessage<T>(T message) where T : class
    {
        if (IsConnected)
        {
            TestMessage.ID msgId = TestMessage.GetID(typeof(T));
            TestMessage msg = TestMessage.Create<T>(message);
            SendMessage((uint)msgId, msg);
        }
    }

    public void Update()
    {
        if (IsConnected)
        {
            uint msgId = 0;
            object packet = null;
            PeekMessage(ref msgId, ref packet);
            if (null != packet)
            {
                GameManager.Instance.MessageProc(msgId, packet);
            }
        }
    }

    private void PeekMessage(ref uint msgId, ref object message)
    {
        msgId = 0;
        message = null;
        if (IsConnected)
        {
            MessagePacket packet = DequeueMessage();
            if (null != packet)
            {
                msgId = packet.MsgId;
                TestMessage.ID msgEnum = (TestMessage.ID)msgId;
                message = ((TestMessage)packet.Message).GetValue(msgEnum);
            }
        }
    }
}
