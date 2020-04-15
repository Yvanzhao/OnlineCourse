using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OnlineCourse
{
    /// <summary>
    /// RoomControlWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RoomControlWindow : Window
    {
        //用于记录鼠标落下时的位置，避免落下与抬起位置不同造成的bug
        int whichButtonClicked;
        //当前用户ID
        int userId;
        public RoomControlWindow(int userIdIn)
        {
            InitializeComponent();
            userId = userIdIn;
            whichButtonClicked = 0;
        }

        /// <summary>
        /// 用于退出登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            whichButtonClicked = 1;
        }

        /// <summary>
        /// 用于退出登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (whichButtonClicked == 1)
            {
                LoginWindow roomControl = new LoginWindow();
                Window thisWindow = Window.GetWindow(this);
                thisWindow.Close();
                roomControl.Show();
            }
            else
                whichButtonClicked = 0;
        }

        /// <summary>
        /// 房间按钮点击后进行对应操作。暂时未连接服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RoomButtonClick(object sender, RoutedEventArgs e) {
            if (int.Parse((sender as Button).Tag.ToString()) == 0)
            {
                createRoom(roomIdOfCreate.Text);
            }
            else if (int.Parse((sender as Button).Tag.ToString()) == 1) {
                enterRoom(roomIdOfEnter.Text);
            }
        }
        /// <summary>
        /// 创建房间操作，并关闭本窗口
        /// </summary>
        /// <param name="roomId"></param>
        private void createRoom(string roomId) {
            LiveWindow liveWindow = new LiveWindow(0,roomId,0);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            liveWindow.Show();
        }
        /// <summary>
        /// 加入房间操作，并关闭本窗口
        /// </summary>
        /// <param name="roomId"></param>
        private void enterRoom(string roomId)
        {
            LiveWindow liveWindow = new LiveWindow(1, roomId,1);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            liveWindow.Show();
        }
    }
}
