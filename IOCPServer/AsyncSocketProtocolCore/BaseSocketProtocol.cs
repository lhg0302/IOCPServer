using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace SocketServer
{
    public class BaseSocketProtocol : AsyncSocketInvokeElement
    {
        protected string m_socketFlag;
        public string SocketFlag { get { return m_socketFlag; } }

        public BaseSocketProtocol(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {

            m_socketFlag = "";
        }
        //接收消息
        public override bool ProcessReceive(byte[] buffer, int offset, int count)
        {
          //  base.ProcessReceive(buffer,offset,count);
            return true;
        }
    }
}
