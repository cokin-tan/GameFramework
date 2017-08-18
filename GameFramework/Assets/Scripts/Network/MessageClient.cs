using System;
using System.IO;
using System.Collections.Generic;
using System.Net;

namespace FrameWork.TcpClient
{
    internal class MessageClient<T> : BaseSocketClient where T : class
    {
        private byte[] receivedBuffer = new byte[NetUtil.RECEIVED_BUFF_SIZE];
        private int receivedDataLen = 0;
        private PacketQueue messages = new PacketQueue();

        internal MessageClient(string ip, int port):
            base(ip, port)
        {
        }

        private bool EnqueueMessage(MessagePacket packet)
        {
            messages.Enqueue(packet);
            return true;
        }

        protected override void ParsePacket(byte[] buffer, int dataLen)
        {
            if (dataLen <= 0)
            {
                return;
            }

            int nowDataLen = receivedDataLen + dataLen;
            if (receivedBuffer.Length < nowDataLen)
            {
                byte[] buf = new byte[NetUtil.RECEIVED_BUFF_SIZE * 2 > nowDataLen ? NetUtil.RECEIVED_BUFF_SIZE * 2 : nowDataLen];
                Array.Copy(receivedBuffer, 0, buf, 0, receivedDataLen);
                receivedBuffer = buf;
            }
            Array.Copy(buffer, 0, receivedBuffer, receivedDataLen, dataLen);
            receivedDataLen = nowDataLen;

            int offset = 0;
            while (nowDataLen >= NetUtil.MESSAGE_LENGTH_SIZE)
            {
                int messageLen = IPAddress.NetworkToHostOrder(NetUtil.BytesToInt(receivedBuffer, offset));
                if ((messageLen + NetUtil.MESSAGE_HEAD_SIZE) > nowDataLen)
                {
                    Logger.LogError("data length is overflow");
                    break;
                }

                MessagePacket packet = new MessagePacket();
                if (!packet.Decode<T>(buffer, offset + NetUtil.MESSAGE_LENGTH_SIZE, messageLen))
                {
                    return;
                }

                messages.Enqueue(packet);
                Logger.LogError("receive packet id = " + packet.MsgId.ToString());

                messageLen += NetUtil.MESSAGE_LENGTH_SIZE;
                offset += messageLen;
                nowDataLen -= messageLen;
            }

            if (nowDataLen > 0)
            {
                Array.Copy(receivedBuffer, offset, receivedBuffer, 0, nowDataLen);
            }

            receivedDataLen = nowDataLen;
        }

        public void SendMessage(uint msgId, T message)
        {
            if (IsConnected)
            {
                MessagePacket packet = new MessagePacket(msgId, message);
                MemoryStream ms = packet.Encode<T>();
                SendMessage(ms.GetBuffer(), (int)ms.Length);
            }
            else
            {
                Console.WriteLine("the socket is not connected");
            }
        }

        public int MessageNum
        {
            get
            {
                return messages.Count;
            }
        }

        public MessagePacket DequeueMessage()
        {
            return messages.Dequeue();
        }
    }
}