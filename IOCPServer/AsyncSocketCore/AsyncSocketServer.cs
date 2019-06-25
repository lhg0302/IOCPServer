using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace SocketServer
{
    public class AsyncSocketServer : IAsyncSocketServer
    {
        #region 私有变量
        private Socket listenSocket;
        /// <summary>
        /// 限制访问接收连接的线程数，用来控制最大并发数
        /// </summary>
        private Semaphore m_maxNumberAcceptedClients;
        #endregion
        #region 属性
        private int _numConnections = 1000;
        /// <summary>
        ///服务支持最大连接数 默认为:1000
        /// </summary>
        public int NnumConnections { get { return _numConnections; } set { _numConnections = value; } }
        private int _receiveBufferSize;
        /// <summary>
        /// 每个连接接收缓存大小
        /// </summary>
        private int m_receiveBufferSize { get { return _receiveBufferSize; } set { _receiveBufferSize = value; } }

        private int _socketTimeOutMS = 1 * 1000 * 60; //
        /// <summary>
        /// Socket最大超时时间，单位为MS 默认 1*1000*60也就是一分钟
        /// </summary>
        public int SocketTimeOutMS { get { return _socketTimeOutMS; } set { _socketTimeOutMS = value; } }

        private AsyncSocketUserTokenPool m_asyncSocketUserTokenPool;
        private AsyncSocketUserTokenList m_asyncSocketUserTokenList;
        public AsyncSocketUserTokenList AsyncSocketUserTokenList { get { return m_asyncSocketUserTokenList; } }

        public Action<AsyncSocketServer, AsyncSocketUserToken> BuildingSocketProtocol = null;
        public Action<string,int?> SignOutAction = null;


        private DaemonThread m_daemonThread; //守护线程移除超时连接
        #endregion
      
        #region 实现接口

        public void Init(int NnumConnections = 100, string strIp = "0.0.0.0", int iPort = 6099, int TimeOutMS = 60000)
        {
            m_receiveBufferSize = ProtocolConst.ReceiveBufferSize;
            SocketTimeOutMS = TimeOutMS;
            m_asyncSocketUserTokenPool = new AsyncSocketUserTokenPool(NnumConnections);
            m_asyncSocketUserTokenList = new AsyncSocketUserTokenList();
            m_maxNumberAcceptedClients = new Semaphore(NnumConnections, NnumConnections);

            Start(strIp, iPort);
           
        }

        public void BuildingProtocol(Action<AsyncSocketServer, AsyncSocketUserToken> actionProtocol)
        {
            BuildingSocketProtocol = actionProtocol;
        }

        public void SingOut(Action<string, int?> SingOutEve)
        {
            SignOutAction = SingOutEve;
        }

        public List<AsyncSocketUserToken> GetSocketList()
        {
            return AsyncSocketUserTokenList.CopyList();
        }

        public AsyncSocketUserToken SendMessageByIP(string ip, byte[] Buffer)
        {
            var asyncSocket = GetAsyncSocketByIp(ip);
           
            asyncSocket?.AsyncSocketInvokeElement.DoSendBuffer(Buffer, 0, Buffer.Length);
            return asyncSocket;
        }
        public AsyncSocketUserToken SendMessageByFlag(Int32? flag, byte[] Buffer)
        {
            var asyncSocket = GetAsyncSocketByFlag(flag);
            asyncSocket?.AsyncSocketInvokeElement.DoSendBuffer(Buffer, 0, Buffer.Length);
            return asyncSocket;
        }


        public AsyncSocketUserToken GetAsyncSocketByIp(string Ip)
        {
            var asyncSocket = GetSocketList().Find((x) =>
            {
                return x.ConnectSocket.RemoteEndPoint.ToString().Split(':')[0].Contains(Ip);
            });
            return asyncSocket;
        }

        public AsyncSocketUserToken GetAsyncSocketByFlag(Int32? flag)
        {
            var asyncSocket = GetSocketList().Find((x) =>
            {
                return x.Flag==flag;
            });
            return asyncSocket;
        }

        public bool DisConnectByIp(string Ip)
        {
           
            var AsyncSocket = GetAsyncSocketByIp(Ip);
            if (AsyncSocket==null) return true;
            CloseClientSocket(AsyncSocket);
            return true;
        }

        public bool DisConnectByFlag(Int32? flag)
        {
            var AsyncSocket = GetAsyncSocketByFlag(flag);
            if (AsyncSocket == null) return true;
            CloseClientSocket(AsyncSocket);
            return true;
        }
        #endregion

        #region 主要代码
        /// <summary>
        /// 创建异步SocketServer
        /// </summary>
        /// <param name="numConnections">服务端最大连接数</param>
        public AsyncSocketServer()
        {
          
        }

        /// <summary>
        ///  创建监听的Socket
        /// </summary>
        /// <param name="ip">服务ip </param>
        /// <param name="port">服务端口号</param>
        public void Start(string ip = "0.0.0.0", int port = 6099)
        {
            Init(m_receiveBufferSize);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            listenSocket.Listen(NnumConnections);
           // LogService.Instance.InfoFormatted("Start listen socket {0} success", localEndPoint.ToString());
            StartAccept(null);
            m_daemonThread = new DaemonThread(this);//开启守护线程
        }
        /// <summary>
        /// 创建连接缓存
        /// </summary>
        /// <param name="receiveBufferSize">缓存字节数组的长度</param>
        public void Init(int receiveBufferSize)
        {
            AsyncSocketUserToken userToken;
            for (int i = 0; i < NnumConnections; i++) //按照连接数建立读写对象
            {
                userToken = new AsyncSocketUserToken(receiveBufferSize);
                userToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                userToken.SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                m_asyncSocketUserTokenPool.Push(userToken);
            }
        }

        /// <summary>
        /// 异步监听连接
        /// </summary>
        /// <param name="acceptEventArgs"></param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArgs.AcceptSocket = null; //释放上次绑定的Socket，等待下一个Socket连接
            }

            m_maxNumberAcceptedClients.WaitOne(); //获取信号量
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArgs);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArgs);
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                ProcessAccept(acceptEventArgs);
            }
            catch (Exception E)
            {
               // LogService.Instance.ErrorFormatted("Accept client {0} error, message: {1}", acceptEventArgs.AcceptSocket, E.Message);
               // LogService.Instance.Error(E.StackTrace);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
           // LogService.Instance.InfoFormatted("Client connection accepted. Local Address: {0}, Remote Address: {1}",
           //      acceptEventArgs.AcceptSocket.LocalEndPoint, acceptEventArgs.AcceptSocket.RemoteEndPoint);

            AsyncSocketUserToken userToken = m_asyncSocketUserTokenPool.Pop();
            m_asyncSocketUserTokenList.Add(userToken); //添加到正在连接列表
            userToken.ConnectSocket = acceptEventArgs.AcceptSocket;
            userToken.ConnectDateTime = DateTime.Now;

            try
            {
                bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                if (!willRaiseEvent)
                {
                    lock (userToken)
                    {
                        ProcessReceive(userToken.ReceiveEventArgs);
                    }
                }
            }
            catch (Exception E)
            {
               // LogService.Instance.ErrorFormatted("Accept client {0} error, message: {1}", userToken.ConnectSocket, E.Message);
               // LogService.Instance.Error(E.StackTrace);
            }

            StartAccept(acceptEventArgs); //把当前异步事件释放，等待下次连接
        }
        //收发消息
        void IO_Completed(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            AsyncSocketUserToken userToken = asyncEventArgs.UserToken as AsyncSocketUserToken;
            userToken.ActiveDateTime = DateTime.Now;
            try
            {
                lock (userToken)
                {
                    if (asyncEventArgs.LastOperation == SocketAsyncOperation.Receive)
                        ProcessReceive(asyncEventArgs);
                    else if (asyncEventArgs.LastOperation == SocketAsyncOperation.Send)
                        ProcessSend(asyncEventArgs);
                    else
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (Exception E)
            {
               // LogService.Instance.ErrorFormatted("IO_Completed {0} error, message: {1}", userToken.ConnectSocket, E.Message);
               // LogService.Instance.Error(E.StackTrace);
            }
        }
        /// <summary>
        /// 数据接收处理分发
        /// </summary>
        /// <param name="receiveEventArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs receiveEventArgs)
        {
            AsyncSocketUserToken userToken = receiveEventArgs.UserToken as AsyncSocketUserToken;
            if (userToken.ConnectSocket == null)
                return;
            userToken.ActiveDateTime = DateTime.Now;
            if (userToken.ReceiveEventArgs.BytesTransferred > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                int offset = userToken.ReceiveEventArgs.Offset;
                int count = userToken.ReceiveEventArgs.BytesTransferred;
                if ((userToken.AsyncSocketInvokeElement == null) & (userToken.ConnectSocket != null)) //存在Socket对象，并且没有绑定协议对象，则进行协议对象绑定
                {
                    BuildingSocketInvokeElement(userToken);///添加的解析类
                    //  offset = offset + 1;
                    // count = count - 1;
                }
                if (userToken.AsyncSocketInvokeElement == null) //如果没有解析对象，提示非法连接并关闭连接
                {
                    CloseClientSocket(userToken);
                   // LogService.Instance.WarnFormatted("Illegal client connection. Local Address: {0}, Remote Address: {1}", userToken.ConnectSocket.LocalEndPoint,
                    //    userToken.ConnectSocket.RemoteEndPoint);

                }
                else
                {
                    if (count > 0) //处理接收数据
                    {
                        if (!userToken.AsyncSocketInvokeElement.ProcessReceive(userToken.ReceiveEventArgs.Buffer, offset, count))
                        { //如果处理数据返回失败，则断开连接
                            CloseClientSocket(userToken);
                        }
                        else //否则投递下次介绍数据请求
                        {
                            bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                            if (!willRaiseEvent)
                                ProcessReceive(userToken.ReceiveEventArgs);
                        }
                    }
                    else
                    {
                        bool willRaiseEvent = userToken.ConnectSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                        if (!willRaiseEvent)
                            ProcessReceive(userToken.ReceiveEventArgs);
                    }
                }
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }
        /// <summary>
        /// 解析协议 指定消息解析方式
        /// </summary>
        /// <param name="userToken"></param>
        private void BuildingSocketInvokeElement(AsyncSocketUserToken userToken)
        {
            //byte flag = userToken.ReceiveEventArgs.Buffer[userToken.ReceiveEventArgs.Offset];
            //userToken.AsyncSocketInvokeElement = new BaseSocketProtocol(this, userToken);

            BuildingSocketProtocol?.Invoke(this, userToken);
            if (userToken.AsyncSocketInvokeElement != null)
            {
               // LogService.Instance.InfoFormatted("Building socket invoke element {0}.Local Address: {1}, Remote Address: {2}",
                 //    userToken.AsyncSocketInvokeElement, userToken.ConnectSocket.LocalEndPoint, userToken.ConnectSocket.RemoteEndPoint);
            }
        }

        private bool ProcessSend(SocketAsyncEventArgs sendEventArgs)
        {
            AsyncSocketUserToken userToken = sendEventArgs.UserToken as AsyncSocketUserToken;
            if (userToken.AsyncSocketInvokeElement == null)
                return false;
            userToken.ActiveDateTime = DateTime.Now;
            if (sendEventArgs.SocketError == SocketError.Success)
                return userToken.AsyncSocketInvokeElement.SendCompleted(); //调用子类回调函数
            else
            {
                CloseClientSocket(userToken);
                return false;
            }
        }

        public bool SendAsyncEvent(Socket connectSocket, SocketAsyncEventArgs sendEventArgs, byte[] buffer, int offset, int count)
        {
            if (connectSocket == null)
                return false;
            sendEventArgs.SetBuffer(buffer, offset, count);
            bool willRaiseEvent = connectSocket.SendAsync(sendEventArgs);
            if (!willRaiseEvent)
            {
                return ProcessSend(sendEventArgs);
            }
            else
                return true;
        }
        /// <summary>
        /// 客户端关闭连接释放
        /// </summary>
        /// <param name="userToken"></param>
        public void CloseClientSocket(AsyncSocketUserToken userToken)
        {
            if (userToken.ConnectSocket == null)
                return;
            string socketInfo = string.Format("Local Address: {0} Remote Address: {1}", userToken.ConnectSocket.LocalEndPoint,
                userToken.ConnectSocket.RemoteEndPoint);
            SignOutAction?.Invoke(userToken.ConnectSocket.RemoteEndPoint.ToString(), userToken.Flag);
           // LogService.Instance.InfoFormatted("Client connection disconnected. {0}", socketInfo);
            try
            {
                userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception E)
            {
               // LogService.Instance.ErrorFormatted("CloseClientSocket Disconnect client {0} error, message: {1}", socketInfo, E.Message);
            }
            userToken.ConnectSocket.Close();
            userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源
           
            m_maxNumberAcceptedClients.Release();
            m_asyncSocketUserTokenPool.Push(userToken);
            m_asyncSocketUserTokenList.Remove(userToken);
        }

        #endregion
    }
}
