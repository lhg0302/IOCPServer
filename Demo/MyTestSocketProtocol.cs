using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using SocketServer;
//using Mesnac.Log;
//using Mesnac.AsyncSocketServer;

namespace IOCPDemo
{
    public class MyTestSocketProtocol : BaseSocketProtocol
    {
        public MyTestSocketProtocol(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {

            m_socketFlag = "";
        }
        //接收消息
        public override bool ProcessReceive(byte[] buffer, int offset, int count)
        {
            var message = Encoding.UTF8.GetString(buffer, offset, count);
            Console.WriteLine(message + "__客户端_IP:__" + AsyncSocketUserToken.ReceiveEventArgs.AcceptSocket.RemoteEndPoint);
         
            //发数据

            base.AsyncSocketUserToken.AsyncSocketInvokeElement.DoSendBuffer(buffer, 0, count);
            return true;
        }
    }
}
