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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OnlineCourse
{
    /// <summary>
    /// CourseDetail.xaml 的交互逻辑
    /// </summary>
    public partial class CourseDetail : Page
    {
        public CourseDetail()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 打开直播窗口，原型中暂时用tag分辨学神与教师
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LiveButton_Click(object sender, RoutedEventArgs e)
        {
            int tag = int.Parse((sender as Button).Tag.ToString());
            LiveWindow liveWindow = new LiveWindow(tag);
            liveWindow.Show();
        }
    }
}
