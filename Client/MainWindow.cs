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
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CourseDetailWindow : Window
    {
        public CourseDetailWindow()
        {
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void CourseType_MouseEnter(object sender, MouseEventArgs e)
        {
            CourseTypeChooseLabel.Foreground = Brushes.Aqua;
            CourseTypeChooseLabel.FontWeight = FontWeights.Bold;
        }

        private void CourseType_MouseLeave(object sender, MouseEventArgs e)
        {
            CourseTypeChooseLabel.Foreground = Brushes.MidnightBlue;
            CourseTypeChooseLabel.FontWeight = FontWeights.Normal;
        }
    }
}
