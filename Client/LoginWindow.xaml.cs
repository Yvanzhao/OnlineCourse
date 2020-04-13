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
        /// 点击登录后调用的方法。暂时为连接服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoginButtonClicked(object sender, RoutedEventArgs e) {
            successLogin();
        }

        /// <summary>
        /// 成功登陆后调用的方法，用于调出房间管理窗口并关闭本窗口
        /// </summary>
        private void successLogin() {
            RoomControlWindow roomControl = new RoomControlWindow(0);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            roomControl.Show();
        }
    }
}
