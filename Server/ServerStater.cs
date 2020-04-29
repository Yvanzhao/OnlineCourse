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
        private static List<Socket[]> Sockets;
        private static List<string> RoomIds;
        private static List<Boolean> isClosing;
        [Obsolete]
        static int Main(string[] args)
        {
            //注册通道
            TcpChannel chan = new TcpChannel(8084);
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

        /// <summary>
        /// 开启服务器端的Socket
        /// </summary>
        static void ServerSocket() {
            Sockets = new List<Socket[]>();
            RoomIds = new List<string>();
            isClosing = new List<Boolean>();
            Thread serverThread = new Thread(new ThreadStart(serverSocketThread));
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        /// <summary>
        /// 开启服务器端的线程，对于客户端连接进行监听
        /// </summary>
        static void serverSocketThread()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdr = IPAddress.Parse("0.0.0.0");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8085);
            serverSocket.Bind(ipEp);
            serverSocket.Listen(0);
            Console.WriteLine("[服务器]启动成功");
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                Console.WriteLine("[服务器]已接收客户端[连接]");
                Thread clientThread = new Thread(new ParameterizedThreadStart(ClientSocketThread));
                clientThread.Start(clientSocket);
                
            }
        }

        /// <summary>
        /// 客户端连接后的线程，监听客户端的请求
        /// </summary>
        /// <param name="clientSocketObject"></param>
        static void ClientSocketThread(Object clientSocketObject) {
            Socket client = clientSocketObject as Socket;
            while (true) {
                byte[] readBuff = new byte[3072];
                int count = client.Receive(readBuff);
                string orders = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);

                Boolean canExit = orderAnalyze(orders, client);
                if (canExit)
                    break;
            }
        }

        /// <summary>
        /// 对于Socket命令进行解析，返回值表示是否可以退出，为true时表示Socket将关闭
        /// </summary>
        /// <param name="orders">客户端指令集</param>
        /// <param name="clientSocket">客户端Socket</param>
        /// <returns></returns>
        static Boolean orderAnalyze(string orders,Socket clientSocket)
        {
            string[] order = orders.Split('@');
            int properPosition = getProperPosition(order);
            if (properPosition < 0)
                return false;
            if (order.Length >= 3) {
                //所有指令的第二位必须是roomId,第三位必须是userPosition
                string roomId = order[properPosition+1];
                int userPosition = int.Parse(order[properPosition+2]);

                if (isClosing[getPosition(roomId)])
                    return true;

                //与服务器第一次建立连接 格式"FirstConnect@'roomId'@'userPosition'@"
                if (order[0].Equals("FirstConnect"))
                {
                    if (order.Length < 3)
                        return false;

                    //老师与服务器连接，需要建立房间
                    if (userPosition == 0)
                    {
                        RoomIds.Add(roomId);
                        Socket[] newSockets = new Socket[6];
                        newSockets[0] = clientSocket;
                        Sockets.Add(newSockets);
                        isClosing.Add(false);

                        clientSocket.Send(System.Text.Encoding.Default.GetBytes("Success"));
                        return false;
                    }
                    //学生与服务器连接，加入房间，并且需要将“现有房间内哪些位置”有人通知房间内其他人，
                    else if (userPosition < 6)
                    {
                        int roomPosition = getPosition(roomId);
                        if (roomPosition < 0)
                        {
                            clientSocket.Send(System.Text.Encoding.Default.GetBytes("Fail"));
                            return true;
                        }
                        Sockets[roomPosition][userPosition] = clientSocket;
                        //生成新的命令，用于通知房间内所有人，哪些学生有人  格式"StudentIn@'hasStudent_1'@'hasStudent_2'@'hasStudent_3'@'hasStudent_4'@'hasStudent_5'@"
                        string newOrder = "StudentIn@";
                        for (int position = 1; position < 6; position++)
                        {
                            if (Sockets[roomPosition][position] == null)
                                newOrder = newOrder + "0@";
                            else
                                newOrder = newOrder + "1@";
                        }

                        broadcastOrder(roomPosition, newOrder, -1);

                        return false;
                    }

                    clientSocket.Send(System.Text.Encoding.Default.GetBytes("Fail"));
                    return true;
                }
                //用户退出房间 格式"Quit@'roomId'@'userPosition'@"
                else if (order[0].Equals("Quit"))
                {
                    if (order.Length < 3)
                        return false;

                    //教师退出房间，需要关闭整个房间
                    if (userPosition == 0)
                    {
                        int roomPosition = getPosition(roomId);
                        if (roomPosition < 0)
                        {
                            return true;
                        }
                        isClosing[roomPosition] = true;

                        broadcastOrder(roomPosition, orders, userPosition);

                        RoomIds.RemoveAt(roomPosition);
                        Sockets.RemoveAt(roomPosition);
                        isClosing.RemoveAt(roomPosition);
                        return true;
                    }
                    //学生退出房间，需要通知房间内其他人
                    else if (userPosition < 6)
                    {
                        int roomPosition = getPosition(roomId);
                        if (roomPosition < 0)
                        {
                            return true;
                        }
                        Sockets[roomPosition][userPosition] = null;
                        if (isClosing[roomPosition] == false)//房间不是正在关闭，则表示是单个学生退出，需要通知其他所有人
                            broadcastOrder(roomPosition, orders, userPosition);
                        return true;
                    }

                    return true;
                }
                //其他指令，服务器不作处理，直接广播到房间内其他客户端处
                else
                {
                    int roomPosition = getPosition(roomId);
                    if (roomPosition < 0)
                    {
                        return false;
                    }
                    if(isClosing[roomPosition] == false)
                        broadcastOrder(roomPosition, orders, userPosition);
                }
            }
            
            return false;
        }

        static int getProperPosition(string[] order) {
            int position;
            for (position = 0; position < order.Length; position++) {
                if (checkOrder(order[position]))
                    return position;
            }
            return -1;
        }

        static Boolean checkOrder(string order) {
            Boolean check = false;
            check = check || order.Equals("BanVoice");
            check = check || order.Equals("EnableVoice");
            check = check || order.Equals("EnableControl");
            check = check || order.Equals("DisableControl");
            check = check || order.Equals("AskControl");
            check = check || order.Equals("CancelAskControl");
            check = check || order.Equals("Color");
            check = check || order.Equals("ClearCanvas");
            check = check || order.Equals("Quit");
            check = check || order.Equals("StudentIn");
            check = check || order.Equals("EndDraw");
            check = check || order.Equals("Point");
            check = check || order.Equals("FirstConnect");
            return check;
        }

        /// <summary>
        /// 根据roomId获取该房间的房间号
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        static private int getPosition(string roomId) {
            for (int position = 0; position < RoomIds.Count; position++) {
                if (RoomIds[position].Equals(roomId))
                    return position;
            }
            return -1;
        }

        /// <summary>
        /// 将命令进行广播
        /// </summary>
        /// <param name="roomPosition"></param>
        /// <param name="orders"></param>
        /// <param name="notToBroadcast"></param>
        static private void broadcastOrder(int roomPosition,string orders,int notToBroadcast) {
            Socket[] roomSockets = Sockets[roomPosition];
            for (int position = 0; position < roomSockets.Length; position++) {
                if (position != notToBroadcast) {
                    if (roomSockets[position] != null) {
                        roomSockets[position].Send(System.Text.Encoding.Default.GetBytes(orders));
                    }
                }
            }
        }
    }
}
