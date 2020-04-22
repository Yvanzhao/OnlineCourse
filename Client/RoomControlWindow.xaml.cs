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
        //当前用户
        user user;
        //服务器端调用
        Server.ServerService server;
        public RoomControlWindow(user userIn, Server.ServerService server)
        {
            this.server = server;
            if (server == null)
            {
                Application.Current.Shutdown();
            }
            InitializeComponent();
            user = userIn;
            whichButtonClicked = 0;
            welcomeLabel.Content = "欢迎，" + userIn.userName;
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
                Application.Current.Shutdown();
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
            if (this.server.createOrEnterRoom(roomId) == 0)
            {

                LiveWindow liveWindow = new LiveWindow(0, roomId, user,this.server);
                Window thisWindow = Window.GetWindow(this);
                thisWindow.Close();
                liveWindow.Show();
            }
            else {
                CreateWarningLabel.Content = "该房间号已存在";
                CreateWarningLabel.Visibility = Visibility.Visible;
            }

        }
        /// <summary>
        /// 加入房间操作，并关闭本窗口
        /// </summary>
        /// <param name="roomId"></param>
        private void enterRoom(string roomId)
        {
            if (this.server.createOrEnterRoom(roomId) == 1)
            {

                LiveWindow liveWindow = new LiveWindow(1, roomId, user,this.server);
                Window thisWindow = Window.GetWindow(this);
                thisWindow.Close();
                liveWindow.Show();
            }
            else if (this.server.createOrEnterRoom(roomId) == 0)
            {
                JoinWarningLabel.Content = "该房间号不存在";
                JoinWarningLabel.Visibility = Visibility.Visible;
            }
            else if (this.server.createOrEnterRoom(roomId) == 2)
            {
                JoinWarningLabel.Content = "房间已满";
                JoinWarningLabel.Visibility = Visibility.Visible;
            }
        }
    }
}
