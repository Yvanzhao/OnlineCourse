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
        /*static MySqlConnection connection;
        public static void OpenDB()
        {
            string connectionStr = "server=172.19.241.249;database=OnlineCourse";
            connection = new MySqlConnection(connectionStr);
            connection.Open();
        }
        public static void CloseDB()
        {

            connection.Close();
        }*/
        /// <summary>
        /// 登录函数，返回userID，返回-1表示用户名或密码不正确
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int logIn(string userName, string password)
        {
            /* OpenDB();
             string sql = "select userid,username from users where userid='"+userName+"';";
            MySqlDataAdapter adapter = new MySqlDataAdapter(sql, connection);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);
            int id;
            string pass;
            foreach (DataRow r in dataTable.Rows)
            {
               id=(int)r["userid"];
               pass=(string)r["password"];    
            }
            CloseDB();
            if(!password.Equals(pass))
                return -1;
            else
                return id;
          */

            if (userName.Equals("a"))
                return 0;
            else if (userName.Equals("b"))
                return 1;
            else if (userName.Equals("c"))
                return 2;
            return -1;
        }
        /* <summary>
         创建或进入房间，1表示房间号存在，0表示房间号不存在，2表示房间已满
         </summary>
         <param name="roomId"></param>
         <returns></returns>*/
        public int createOrEnterRoom(string roomId)
        {
            if (roomId.Equals("room"))
                return 1;
            else if (roomId.Equals("roomFull"))
                return 2;
            else
                return 0;
        }

        /// <summary>
        /// 创建用户函数，返回userID，返回-1表示用户名已存在
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int createUser(string userName, string password)
        {
            if (userName.Equals("a"))
                return -1;
            else if (userName.Equals("b"))
                return -1;
            else if (userName.Equals("c"))
                return -1;
            return 3;
        }

        /// <summary>
        /// 获取用户摄像头的排序，1~5
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public int getUserPosition(string roomId, int userId) { return 1; }
    }
}
