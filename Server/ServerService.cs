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
        /// 创建或进入房间，false表示房间号存在，true表示房间号不存在
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public Boolean createOrEnterRoom(string roomId) {
            if (roomId.Equals("room"))
                return true; 
            else 
                return false; 
        }
    }
}
