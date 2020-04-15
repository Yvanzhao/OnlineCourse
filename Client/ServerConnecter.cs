using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 用于连接到服务器端的函数
/// </summary>
namespace OnlineCourse
{
    class ServerConnecter
    {
        [Obsolete]
        public static Server.ServerService connectToServer() {
            TcpChannel chan = new TcpChannel();
            ChannelServices.RegisterChannel(chan);
            Server.ServerService server = (Server.ServerService)Activator.GetObject(typeof(Server.ServerService), "tcp://localhost:8085/OnlineCourseServer");
            if (server == null)
            {
                System.Console.WriteLine("Could not connect to server");
                return null;
            }
            else {
                System.Console.WriteLine("Connected to server");
                return server;
            } 
        }
    }
}
