using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 服务器端提供的所有函数及其实现
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
        public int logIn(string userName, string password) {
            if (userName.Equals("a"))
                return 0;
            else if (userName.Equals("b"))
                return 1;
            else if (userName.Equals("c"))
                return 2;
            return -1; 
        }

        /// <summary>
        /// 创建用户函数，返回userID，返回-1表示用户名已存在
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int createUser(string userName, string password) {
            if (userName.Equals("a") || userName.Equals("b") || userName.Equals("c"))
                return -1;
            return 3;
        }
        /// <summary>
        /// 创建或进入房间，返回房间状态，1表示房间号存在，0表示房间号不存在，2表示房间人数已满
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public int createOrEnterRoom(string roomId) {
            if (roomId.Equals("room"))
                return 1;
            else if (roomId.Equals("fullRoom"))
                return 2;
            else
                return 0;
        }
    }
}
