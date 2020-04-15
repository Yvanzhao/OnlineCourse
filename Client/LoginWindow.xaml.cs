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
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        int whichButtonClicked;
        public LoginWindow()
        {
            InitializeComponent();
            whichButtonClicked = 0;
        }

        /// <summary>
        /// 用于关闭程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonMouseDown(object sender, MouseButtonEventArgs e) {
            whichButtonClicked = 1;
        }

        /// <summary>
        /// 用于关闭程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButtonMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (whichButtonClicked == 1)
                Application.Current.Shutdown();
            else
                whichButtonClicked = 0;
        }

        /// <summary>
        /// 点击注册后调用的方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SignupButtonClicked(object sender, RoutedEventArgs e)
        {
           SignupWindow signup = new SignupWindow();
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            signup.Show();
        }

        /// <summary>
        /// 点击登录后调用的方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginButtonClicked(object sender, RoutedEventArgs e) {
            Server.ServerService server = ServerConnecter.connectToServer();
            if (server == null) {
                networkFailure();
                return;
            }
            string userName = userNameBox.Text;
            string password = passwordBox.Password;
            int userId = server.logIn(userName,password);
            if (userId != -1)
                successLogin(userId);
            else
                loginFailure();
        }

        /// <summary>
        /// 成功登陆后调用的方法，用于调出房间管理窗口并关闭本窗口
        /// </summary>
        private void successLogin(int userId) {
            user user;
            user.userId = userId;
            user.userName = userNameBox.Text;
            RoomControlWindow roomControl = new RoomControlWindow(user);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            roomControl.Show();
        }

        /// <summary>
        /// 登录失败后调用的方法，展示警示文字
        /// </summary>
        private void loginFailure() {
            WarningLabel.Content = "用户名或密码错误";
            WarningLabel.Visibility = Visibility.Visible;
        }

        private void networkFailure() {
            WarningLabel.Content = "请检查你的网络连接";
            WarningLabel.Visibility = Visibility.Visible;
        }
    }
}
