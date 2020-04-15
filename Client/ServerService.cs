﻿using System;
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
        public int logIn(string userName,string password) { return -1; }
        public Boolean createOrEnterRoom(string roomId) { return false; }
    }
}