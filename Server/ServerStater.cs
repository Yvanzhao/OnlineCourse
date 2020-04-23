using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 开启服务器的函数
/// </summary>
namespace Server
{
    class ServerStater
    {
        private static List<string[]> IPandRoomIDs;
        [Obsolete]
        static int Main(string[] args)
        {
            //注册通道
            TcpChannel chan = new TcpChannel(8086);
            ChannelServices.RegisterChannel(chan);
            string sshan = chan.ChannelName;
            System.Console.WriteLine(sshan);
            //注册远程对象,即激活.
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(ServerService), "OnlineCourseServer", WellKnownObjectMode.SingleCall);

            ServerSocket();

            System.Console.WriteLine("Hit any key to exit...");
            System.Console.ReadLine();
            return 0;
        }

        static void ServerSocket() {
            IPandRoomIDs = new List<string[]>();
            Thread serverThread = new Thread(new ThreadStart(serverSocketThread));
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        static void serverSocketThread()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdr = IPAddress.Parse("172.19.241.249");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8085);
            serverSocket.Bind(ipEp);
            serverSocket.Listen(0);
            Console.WriteLine("服务器端[服务器]启动成功");
            while (true)
            {
                Socket studentOrder = serverSocket.Accept();
                Console.WriteLine("服务器端[服务器]已接收客户端[命令]");
                orderAnalyze(studentOrder);
            }
        }

        static void orderAnalyze(Socket socketOrder)
        {
            byte[] readBuff = new byte[1024];
            int count = socketOrder.Receive(readBuff);
            string orders = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);

            string[] order = orders.Split('@');
            //与服务器第一次建立连接 格式"firstConnect@'userPosition'@'roomId'"
            if (order[0].Equals("firstConnect"))
            {
                if (order.Length < 3)
                    return;
                string ip = socketOrder.RemoteEndPoint.ToString().Split(':')[0];
                Console.WriteLine(ip);
                socketOrder.Send(System.Text.Encoding.Default.GetBytes(ip));
                if (int.Parse(order[1]) == 0)
                {
                    string[] IPandRoomId = new string[2];
                    IPandRoomId[0] = ip;
                    IPandRoomId[1] = order[2];
                    IPandRoomIDs.Add(IPandRoomId);
                }
            }
            //获得教师IP 格式"getTeacher@'roomId'"
            else if (order[0].Equals("getTeacher"))
            {
                if (order.Length < 2)
                    return;
                string roomId = order[1];
                for (int position = 0; position < IPandRoomIDs.Count; position++)
                {
                    if (IPandRoomIDs[position][1].Equals(roomId))
                    {
                        socketOrder.Send(System.Text.Encoding.Default.GetBytes(IPandRoomIDs[position][0]));
                    }
                }
            }
            //删除房间 格式"CloseRoom@'roomId'"
            else if (order[0].Equals("CloseRoom")) {
                if (order.Length < 2)
                    return;
                string roomId = order[1];
                for (int position = 0; position < IPandRoomIDs.Count; position++)
                {
                    if (IPandRoomIDs[position][1].Equals(roomId))
                    {
                        IPandRoomIDs.RemoveAt(position);
                    }
                }
            }

            socketOrder.Close();

        }
    }
}
