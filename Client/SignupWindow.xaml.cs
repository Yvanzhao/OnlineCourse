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
    /// SignupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SignupWindow : Window
    {
        int whichButtonClicked;
        public SignupWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 用于关闭程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            whichButtonClicked = 1;
        }

        /// <summary>
        /// 用于关闭本窗口并返回登录窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (whichButtonClicked == 1) {
                LoginWindow roomControl = new LoginWindow();
                Window thisWindow = Window.GetWindow(this);
                thisWindow.Close();
                roomControl.Show();
            }
            else
                whichButtonClicked = 0;
        }

        /// <summary>
        /// 点击登录后调用的方法。暂时为连接服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SignupButtonClicked(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password.Equals(passwordCheckBox.Password) == false) {
                WarningLabel.Content = "两次输入密码不一致";
                WarningLabel.Visibility = Visibility.Visible;
                return;
            }
            Server.ServerService server = ServerConnecter.connectToServer();
            if (server == null) {
                WarningLabel.Content = "请检查您的网络连接";
                WarningLabel.Visibility = Visibility.Visible;
                return;
            }
            int userId = server.createUser(userNameBox.Text, passwordBox.Password);
            if (userId == -1) {
                WarningLabel.Content = "用户名已存在";
                WarningLabel.Visibility = Visibility.Visible;
                return;
            }
            successSignup(userId);
        }

        /// <summary>
        /// 成功登陆后调用的方法，用于调出房间管理窗口并关闭本窗口
        /// </summary>
        private void successSignup(int userId)
        {
            user user;
            user.userId = userId;
            user.userName = userNameBox.Text;
            RoomControlWindow roomControl = new RoomControlWindow(user);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            roomControl.Show();
        }
    }
}
