using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketServer
{
    public class ProtocolConst
    {
        public static int InitBufferSize = 1024; //解析命令初始缓存大小        
        public static int ReceiveBufferSize = 1024; //IOCP接收数据缓存大小，设置过小会造成事件响应增多，设置过大会造成内存占用偏多
        public static int SocketTimeOutMS = 60 * 1000; //Socket超时设置为60秒
    }
}
