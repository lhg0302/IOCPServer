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
            //  var message = Encoding.UTF8.GetString(buffer, offset, count);
            var data = BitConverter.ToInt32(buffer, 0);
          //  var message= BitConverter.ToInt32(buffer,0);
         //  Console.WriteLine(message /*+ "__客户端_IP:__" + message*/);/* AsyncSocketUserToken.ReceiveEventArgs.AcceptSocket.RemoteEndPoint);*/
            Console.WriteLine(data);
            //发数据

            base.AsyncSocketUserToken.AsyncSocketInvokeElement.DoSendBuffer(buffer, 0, count);
            return true;
        }
    }
}
