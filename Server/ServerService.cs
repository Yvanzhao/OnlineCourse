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
        public int getUserPosition(string roomId, int userId) { return 3; }

        /// <summary>
        /// 将该房间的第position号位置设置为空，如果position为0，则删除该房间
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="position"></param>
        public void setEmptyPosition(string roomId, int position) { }

        /// <summary>
        /// 用户在板上绘图，更新画板信息。
        /// point是double[2]，按顺序是点的X值与Y值
        /// color是byte[4]，按顺序对应 A R G B
        /// pointId取不同值的情况： 
        /// "0"表示传进来一个新的点,且这个点是一条新的线的初始点。此时color会有具体数值。 
        /// "大于0"表示传进来一个新的点，pointId表示这是第几个点。此时color为null。 
        /// "-1"表示清除整个画板。此时point与color都是null
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color"></param>
        /// <param name="updateType"></param>
        public void updateCanvas(double[] point, byte[] color, int pointId) { }

        /// <summary>
        /// 根据传进来的ID获取该房间是否有学生。按照userPosition排列
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public Boolean[] checkStudent(string roomId)
        {
            Boolean[] hasStudent = new Boolean[5];
            for (int position = 0; position < 5; position++)
            {
                hasStudent[position] = true;
            }
            return hasStudent;
        }

        /// <summary>
        /// 根据传进来的roomID获取该房间学生是否具有控制权。按照userPosition排列。拥有画板控制权为true
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Boolean[] checkControl(string roomId)
        {
            Boolean[] hasControl = new Boolean[5];
            for (int position = 0; position < 5; position++)
            {
                hasControl[position] = false;
            }
            hasControl[2] = true;
            return hasControl;
        }

        /// <summary>
        /// 根据传进来的roomID获取该房间学生是否被静音。按照userPosition排列。被静音为true
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public Boolean[] checkSilenced(string roomId)
        {
            Boolean[] silenced = new Boolean[5];
            for (int position = 0; position < 5; position++)
            {
                silenced[position] = false;
            }
            silenced[3] = true;
            return silenced;
        }

        /// <summary>
        /// 根据roomID获得所有线、所有点
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public List<List<double[]>> getLines(string roomId) { return null; }

        /// <summary>
        /// 根据roomID获得所有颜色
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public List<byte[]> getColors(string roomId) { return null; }

        /// <summary>
        /// 根据传进来的学生位置，roomId禁音或者解除静音某个学生。silence = true表示静音该学生，false表示解除静音
        /// </summary>
        /// <param name="userId"></param>
        public void silenceStudent(string roomId, int userPosition, Boolean silence) { }

        /// <summary>
        /// 根据传进来的学生位置，roomId更改控制权。studentControl = true 表示使得学生获得控制权，老师失去控制权；反之则为学生失去控制权，老师获得控制权
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="userPosition"></param>
        /// <param name="studentControl"></param>
        public void changeControl(string roomId, int userPosition, Boolean studentControl) { }
    }
}
