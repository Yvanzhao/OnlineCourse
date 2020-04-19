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
    public class ServerService : MarshalByRefObject
    {
        public int logIn(string userName, string password) { return -1; }
        public int createOrEnterRoom(string roomId) { return 0; }
        public int createUser(string userName, string password) { return -1; }
        public int getUserPosition(string roomId, int userId) { return 1; }
        public void updateCanvas(double[] point, byte[] color, int updateType) { }
        public Boolean[] checkStudent(string roomId)
        {
            Boolean[] hasStudent = new bool[5];
            for (int position = 0; position < 5; position++)
            {
                hasStudent[position] = true;
            }
            return hasStudent;
        }
        public Boolean[] checkControl(string roomId) {
            Boolean[] hasControl = new bool[5];
            for (int position = 0; position < 5; position++)
            {
                hasControl[position] = false;
            }
            return hasControl;
        }
        public Boolean[] checkSilenced(string roomId) {
            Boolean[] silenced = new bool[5];
            for (int position = 0; position < 5; position++) {
                silenced[position] = false;
            }
            return silenced;
        }
        public List<List<double[]>> getLines(string roomId){return null;}
        public List<byte[]> getColors(string roomId) { return null; }
        
    }
}
