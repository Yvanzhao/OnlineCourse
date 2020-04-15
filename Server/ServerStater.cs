using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 开启服务器的函数
/// </summary>
namespace Server
{
    class ServerStater
    {
        static int Main(string[] args)
        {
            //注册通道
            TcpChannel chan = new TcpChannel(8086);
            ChannelServices.RegisterChannel(chan);
            string sshan = chan.ChannelName;
            System.Console.WriteLine(sshan);
            //注册远程对象,即激活.
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(ServerService), "OnlineCourseServer", WellKnownObjectMode.SingleCall);
            System.Console.WriteLine("Hit any key to exit...");
            System.Console.ReadLine();
            return 0;
        }
    }
}
