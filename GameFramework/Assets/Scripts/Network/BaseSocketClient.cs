using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FrameWork.TcpClient
{
    internal abstract class BaseSocketClient
    {
        internal sealed class SendData
        {
            public byte[] buffer = null;
            public int dataLength = 0;
            public int sendByteLength = 0;

            internal SendData(byte[] buf, int length)
            {
                buffer = buf;
                dataLength = length;
            }
        }

        private const int MAX_RECEIVE_BUFF_SIZE = 40960;
        private const int MAX_SEND_BUFF_SIZE = 20480;
        private const int RECEIVE_BUFF_SIZE = 4096;

        private Socket clientSocket = null;
        private IPEndPoint endPoint = null;

        private byte[] receiveBuffer = new byte[RECEIVE_BUFF_SIZE];
        
        private SocketAsyncEventArgs receiveArg = null;

        private object sendLockObj = new object();
        private Queue<SendData> needSendList = new Queue<SendData>();
        private SendData currentSendObj = null;
        private SocketAsyncEventArgs sendArg = null;

        public bool IsConnected
        {
            get
            {
                if (null == clientSocket)
                {
                    return false;
                }
                else
                {
                    return clientSocket.Connected;
                }
            }
        }

        internal BaseSocketClient(string ip, int port)
        {
            try
            {
                endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                
            }
            catch(Exception ex)
            {
                Logger.LogError(string.Format("parse ip address failed!!! ip = {0} message = {1}", ip, ex.Message));
            }
        }

        protected abstract void ParsePacket(byte[] buffer, int dataLen);

        public bool ConnectAsync()
        {
            try
            {
                InitSocket();
                SocketAsyncEventArgs connectArg = new SocketAsyncEventArgs();
                connectArg.UserToken = clientSocket;
                connectArg.RemoteEndPoint = endPoint;
                connectArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectCompleted);

                clientSocket.ConnectAsync(connectArg);
            }
            catch(Exception ex)
            {
                Logger.LogError(string.Format("connect to {0} failed exception = {1}", endPoint.ToString(), ex.Message));
                Close();
                return false;
            }

            return true;
        }

        public void Close()
        {
            try
            {
                Shutdown();
            }
            catch(Exception ex)
            {
                Logger.LogError("close socket failed!!!" + ex.Message.ToString());
            }
        }

        private void Shutdown()
        {
            if (null != clientSocket && IsConnected)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                clientSocket = null;
                currentSendObj = null;
                needSendList.Clear();
            }
        }

        private void InitSocket()
        {
            if (null != clientSocket)
            {
                Shutdown();
            }
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.SendBufferSize = MAX_SEND_BUFF_SIZE;
            clientSocket.ReceiveBufferSize = MAX_RECEIVE_BUFF_SIZE;
            clientSocket.NoDelay = true;
        }

        private void StartSendMessage()
        {
            try
            {
                if (null == sendArg)
                {
                    sendArg = new SocketAsyncEventArgs();
                    sendArg.UserToken = clientSocket;
                    sendArg.RemoteEndPoint = endPoint;
                    sendArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
                }
                sendArg.SetBuffer(currentSendObj.buffer, currentSendObj.sendByteLength, currentSendObj.dataLength - currentSendObj.sendByteLength);

                clientSocket.SendAsync(sendArg);
            }
            catch(Exception ex)
            {
                Logger.LogError("send message failed!!! " + ex.Message.ToString());
                Close();
            }
        }

        private void SendMessage(int sentLength)
        {
            if (sentLength < (currentSendObj.dataLength - currentSendObj.sendByteLength))
            {
                currentSendObj.sendByteLength += sentLength;
                StartSendMessage();
            }
            else
            {
                lock(sendLockObj)
                {
                    if (needSendList.Count > 0)
                    {
                        currentSendObj = needSendList.Dequeue();
                        StartSendMessage();
                    }
                    else
                    {
                        currentSendObj = null;
                    }
                }
            }
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs arg)
        {
            try
            {
                if (SocketAsyncOperation.Send != arg.LastOperation)
                {
                    Logger.LogError("last operation is not send");
                }
                else
                {
                    if (arg.BytesTransferred > 0 && arg.SocketError == SocketError.Success)
                    {
                        SendMessage(arg.BytesTransferred);
                    }
                    else
                    {
                        Logger.LogError("send data failed");
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.LogError("send message failed!!! " + ex.Message.ToString());
                ProcessSocketError(arg);
            }
        }

        protected void SendMessage(byte[] data, int length)
        {
            if (IsConnected)
            {
                SendData sendData = new SendData(data, length);
                lock(sendLockObj)
                {
                    if (null == currentSendObj)
                    {
                        currentSendObj = sendData;
                        StartSendMessage();
                    }
                    else
                    {
                        needSendList.Enqueue(sendData);
                        Logger.LogError("enqueue to need send message list");
                    }
                }
            }
            else
            {
                Logger.LogError("the socket is disconnected");
            }
        }

        private void StartReceiveData()
        {
            if (IsConnected)
            {
                if (null == receiveArg)
                {
                    receiveArg = new SocketAsyncEventArgs();
                    receiveArg.UserToken = clientSocket;
                    receiveArg.RemoteEndPoint = endPoint;
                    receiveArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
                    receiveArg.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                }

                clientSocket.ReceiveAsync(receiveArg);
            }
            else
            {
                Logger.LogError("the socket is not connected");
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs arg)
        {
            if (IsConnected)
            {
                try
                {
                    if (SocketAsyncOperation.Receive != arg.LastOperation)
                    {
                        Logger.LogError("the last option is not receive");
                    }
                    else
                    {
                        ParsePacket(arg.Buffer, arg.BytesTransferred);
                        StartReceiveData();
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogError("receive data failed!!! " + ex.Message);
                    ProcessSocketError(arg);
                }
            }
            else
            {
                Logger.LogError("the socket is not connected");
                ProcessSocketError(arg);
            }
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs connectArg)
        {
            Logger.LogError(string.Format("connect {0} over result = {1}", connectArg.RemoteEndPoint, connectArg.SocketError));
            Socket client = connectArg.UserToken as Socket;
            if (null != client && client.Connected)
            {
                StartReceiveData();
            }
        }

        private void ProcessSocketError(SocketAsyncEventArgs arg)
        {
            Socket socket = arg.UserToken as Socket;
            if (null != socket && socket.Connected)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                finally 
                {
                    if (socket.Connected)
                    {
                        socket.Close();
                    }
                }
            }
        }
    }
}
