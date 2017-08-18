using System;

namespace FrameWork.TcpClient
{
    public static class NetUtil
    {
        public const int RECEIVED_BUFF_SIZE = 10240;
        public const int MESSAGE_LENGTH_SIZE = 4;
        public const int MESSAGE_HEAD_SIZE = 2;

        public static int BytesToInt(byte[] buffer, int offset)
        {
            int result = 0;
            result = (buffer[offset] & 0xff) |
                     ((buffer[offset + 1] & 0xff) << 8) |
                     ((buffer[offset + 2] & 0xff) << 16) |
                     ((buffer[offset + 3] & 0xff) << 24);
            return result;
        }

        public static byte[] IntToBytes(int data)
        {
            byte[] result = new byte[4];
            result[0] = (byte)(data & 0xff);
            result[1] = (byte)((data >>= 8) & 0xff);
            result[2] = (byte)((data >>= 8) & 0xff);
            result[3] = (byte)((data >>= 8) & 0xff);

            return result;
        }

        public static string DebugBytes(byte[] data, int len)
        {
            string s = "[ ";
            for (int i = 0; i < len; i++)
            {
                s += (int)data[i] + " ";
            }
            s += "]";
            return s;
        }
    }
}
