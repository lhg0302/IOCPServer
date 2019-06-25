using SocketServer;

namespace IOCPDemo
{
    class Tcpservert
    {
        public static IAsyncSocketServer AsyncSocketSvr;
        public void start()
        {

            AsyncSocketSvr = new AsyncSocketServer();

            AsyncSocketSvr.Init(200, "0.0.0.0", 6099, 60 * 1000);
            //添加解析方式
            AsyncSocketSvr.BuildingProtocol((asyncSocketServer, userToken) =>
            {
                var Point = userToken.ConnectSocket.RemoteEndPoint.ToString().Split(':');
                
                //    userToken.Flag = MsgUtil.ParseToInt(mesEquip.adapterID);
                  userToken.AsyncSocketInvokeElement = new MyTestSocketProtocol(asyncSocketServer, userToken);
                //    adapterStateEvent?.Invoke(mesEquip.adapterID, 1);//发送MES连接状态 设备已连接状态  
               
            });
        }
    }
}
