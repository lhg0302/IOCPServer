using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SocketServer
{
    class DaemonThread : Object
    {
        private Thread m_thread;
        private AsyncSocketServer m_asyncSocketServer;

        public DaemonThread(AsyncSocketServer asyncSocketServer)
        {
            m_asyncSocketServer = asyncSocketServer;
            m_thread = new Thread(DaemonThreadStart);
            m_thread.IsBackground = true;
          //  m_thread.Start();
        }

        public void DaemonThreadStart()
        {
            while (m_thread.IsAlive)
            {
              var  userTokenArray= m_asyncSocketServer.AsyncSocketUserTokenList.CopyList();
                for (int i = 0; i < userTokenArray.Count; i++)
                {
                    if (!m_thread.IsAlive)
                        break;
                    try
                    {
                        if ((DateTime.Now - userTokenArray[i].ActiveDateTime).Milliseconds > m_asyncSocketServer.SocketTimeOutMS) //超时Socket断开
                        {
                            lock (userTokenArray[i])
                            {
                                m_asyncSocketServer.CloseClientSocket(userTokenArray[i]);
                            }
                        }
                    }                    
                    catch (Exception E)
                    {

                       // LogService.Instance.Error("Daemon thread check timeout socket error, message: ", E);
                      // LogService.Instance.Error(E.StackTrace);
                    }
                }

                for (int i = 0; i < 60 * 1000 / 100; i++) //每分钟检测一次
                {
                    if (!m_thread.IsAlive)
                        break;
                    Thread.Sleep(100);
                }
            }
        }

        public void Close()
        {
          //  m_thread.
            m_thread.Abort();
          //  m_thread.Join();
        }
        ~DaemonThread()
        {
            Close();
            GC.Collect();
        }
    }
}
