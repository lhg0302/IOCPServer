using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketServer
{/// <summary>
 /// 异步Socket接口
 /// </summary>
    public interface IAsyncSocketServer
    {
        /// <summary>
        /// 创建异步Socket服务 
        /// </summary>
        /// <param name="NnumConnections">允许最大连接数</param>
        /// <param name="strIp">本机IP</param>
        /// <param name="iPort">端口号</param>
        /// <param name="SocketTimeOutMS">超时时间</param>
        void Init(int NnumConnections = 100, string strIp = "0.0.0.0", int iPort = 6099, int timeOutMS = 60 * 1000);
        /// <summary>
        /// 绑定解析解析方式
        /// </summary>
        /// <param name="actionProtocol"></param>
        void BuildingProtocol(Action<AsyncSocketServer, AsyncSocketUserToken> actionProtocol);
        /// <summary>
        /// 设备退出回调 返回 设备ID 和 IP
        /// </summary>
        void SingOut(Action<string ,int?> SingOutEve);
        /// <summary>
        /// 获取所有连接的Socket
        /// </summary>
        List<AsyncSocketUserToken> GetSocketList();
        /// <summary>
        /// 根据连接的IP 发送给客户端消息
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        AsyncSocketUserToken SendMessageByIP(string Ip, byte[] Buffer);
        /// <summary>
        /// 根据客户端连接的标记发送消息 标记不可重复！！！
        /// 标记需在分配客户端消息解析时指定
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        AsyncSocketUserToken SendMessageByFlag(Int32? flag, byte[] Buffer);
        /// <summary>
        /// 根据IP 获取连接的Socket
        /// </summary>
        /// <param name="Ip">设备IP</param>
        /// <returns></returns>
        AsyncSocketUserToken GetAsyncSocketByIp(string Ip);
        /// <summary>
        /// 根据Flag 获取连接的Socket
        /// </summary>
        /// <param name="flag">设备标记</param>
        /// <returns></returns>
        AsyncSocketUserToken GetAsyncSocketByFlag(Int32? flag);

        bool DisConnectByIp(string Ip);

        bool DisConnectByFlag(Int32? flag);
    }
}
