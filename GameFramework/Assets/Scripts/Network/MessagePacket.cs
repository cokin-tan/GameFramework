using System;
using ProtoBuf;
using System.IO;
using System.Collections.Generic;
using System.Net;

namespace FrameWork.TcpClient
{
    internal sealed class MessagePacket
    {
        internal uint MsgId
        {
            get;
            private set;
        }

        internal object Message
        {
            get;
            private set;
        }

        internal MessagePacket()
        {

        }

        internal MessagePacket(uint msgId, object msg)
        {
            MsgId = msgId;
            Message = msg;
        }

        internal bool Decode<T>(byte[] buf, int offset, int length)
        {
            MemoryStream ms = new MemoryStream(buf, offset, length);
            BinaryReader br = new BinaryReader(ms);
            try
            {
                MsgId = (uint)IPAddress.NetworkToHostOrder(br.ReadInt16());
                Message = Serializer.Deserialize<T>(ms);
            }
            catch(Exception ex)
            {
                Logger.LogError("failed " + ex.Message.ToString());
            }
            finally
            {
                br.Close();
                ms.Close();
            }

            return null != Message;
        }

        internal MemoryStream Encode<T>() where T : class
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            using (MemoryStream msgStream = new MemoryStream())
            {
                T msg = Message as T;
                Serializer.Serialize<T>(msgStream, msg);
                int msgLength = (int)msgStream.Length + NetUtil.MESSAGE_HEAD_SIZE;
                bw.Write(IPAddress.HostToNetworkOrder(msgLength));
                bw.Write(IPAddress.HostToNetworkOrder((short)MsgId));
                bw.Write(msgStream.GetBuffer(), 0, (int)msgStream.Length);
                Logger.LogError("data length = " + ms.Length + " msgLength = " + msgLength);
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }

    internal sealed class PacketQueue
    {
        private List<MessagePacket> messages = new List<MessagePacket>(64);

        private object queueLock = new object();

        public int Count
        {
            get
            {
                lock(queueLock)
                {
                    return messages.Count;
                }
            }
        }

        public void Enqueue(MessagePacket packet)
        {
            if (null != packet)
            {
                lock(queueLock)
                {
                    messages.Add(packet);
                }
            }
        }

        public void Clear()
        {
            lock (queueLock)
            {
                messages.Clear();
            }
        }

        public MessagePacket Dequeue()
        {
            MessagePacket packet = null;
            lock(queueLock)
            {
                if (messages.Count > 0)
                {
                    packet = messages[0];
                    messages.RemoveAt(0);
                }
            }

            return packet;
        }

        public MessagePacket Dequeue(uint msgid)
        {
            MessagePacket packet = null;
            lock(queueLock)
            {
                packet = messages.Find(item => { return item.MsgId == msgid; });
            }
            return packet;
        }
    }
}
