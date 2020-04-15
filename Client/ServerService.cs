using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 这是客户端的“服务器端可调用函数”类，此处并不需要写出实现，实现在服务器端的同名类下
/// </summary>
namespace Server
{
    class ServerService : MarshalByRefObject
    {
        /// <summary>
        /// 登录函数，返回userID，返回-1表示用户名或密码不正确
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int logIn(string userName,string password) { return -1; }
        /// <summary>
        /// 创建或进入房间，返回房间状态，1表示房间号存在，0表示房间号不存在，2表示房间人数已满
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public int createOrEnterRoom(string roomId) { return 0; }
        /*一下是新添加的函数*/
        /// <summary>
        /// 创建用户函数，返回userID，返回-1表示用户名已存在
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int createUser(string userName, string password) { return -1; }
        
    }
}
