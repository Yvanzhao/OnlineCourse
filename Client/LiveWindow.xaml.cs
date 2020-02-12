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
    /// LiveWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LiveWindow : Window
    {
        //用于记录鼠标落下时的位置，避免落下与抬起位置不同造成的bug
        int mouseClickedTag = 0;
        //起始位置
        Point startPoint;
        // 点集合
        List<Point> pointList = new List<Point>();
        // 绘画状态
        Boolean isDrawing = false;

        /// <summary>
        /// 原型中暂时利用Tag分辨老师与学生
        /// </summary>
        /// <param name="tag"></param>
        public LiveWindow(int tag)
        {
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void ComputerIcon_MouseDown(object sender, MouseButtonEventArgs e) {
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            mouseClickedTag = tagHead * 10;
        }

        private void ComputerIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            if (image != null) {
                //用tag表示不同按钮
                int tag = int.Parse(image.Tag.ToString());
                //tagTail表示状态
                int tagTail = tag % 10;
                //tagHead表示序号
                int tagHead = tag / 10;
                //检测鼠标落下与抬起是否相同。十位表征序号，个位为 0 表征是控制权按钮。
                if (mouseClickedTag != tagHead * 10) {
                    mouseClickedTag = 0;
                    return;
                }
                //根据状态不同进行切换
                if (tagTail == 0)
                {
                    image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivedIcon"]);
                    image.Tag = tagHead+""+1;
                }
                else {
                    image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]);
                    image.Tag = tagHead + "" + 0;
                }
                //重置状态值避免bug
                mouseClickedTag = 0;
            }        
        }

        private void RecordIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            mouseClickedTag = tagHead * 10 + 1;
        }

        private void RecordIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            if (image != null)
            {
                //用tag表示不同按钮
                int tag = int.Parse(image.Tag.ToString());
                //tagTail表示状态
                int tagTail = tag % 10;
                //tagHead表示序号
                int tagHead = tag / 10;
                //检测鼠标落下与抬起是否相同。十位表征序号，个位为 1 表征是录音按钮。
                if (mouseClickedTag != tagHead * 10 + 1)
                {
                    mouseClickedTag = 0;
                    return;
                }
                //根据状态不同进行切换
                if (tagTail == 0)
                {
                    image.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]);
                    image.Tag = tagHead + "" + 1;
                }
                else
                {
                    image.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]);
                    image.Tag = tagHead + "" + 0;
                }
                //重置状态值避免bug
                mouseClickedTag = 0;
            }
        }

        private void PrintCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            startPoint = e.GetPosition(printCanvas);
            pointList.Add(startPoint);
            isDrawing = true;
        }

        private void PrintCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed && isDrawing == true) {
                
                Point point = e.GetPosition(printCanvas);
                int count = pointList.Count(); // 总点数            

                // 去重复点
                if (count > 0) {
                    if (point.X - pointList[count - 1].X != 0 || point.Y - pointList[count - 1].Y != 0) {
                        pointList.Add(point);

                        var l = new Line();
                        l.Stroke = Brushes.Black;
                        l.StrokeThickness = 1;
                        if (count < 1)
                            return;
                        l.X1 = pointList[count - 1].X;  // count-2  保证 line的起始点为点集合中的倒数第二个点。
                        l.Y1 = pointList[count - 1].Y;
                        // 终点X,Y 为当前point的X,Y
                        l.X2 = point.X;
                        l.Y2 = point.Y;
                        printCanvas.Children.Add(l);
                    }
                }
            }
        }

        private void PrintCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
        }

        private void printCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private void DeleteIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseClickedTag = 1;
        }

        private void DeleteIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseClickedTag != 1) {
                mouseClickedTag = 0;
                return;
            }
            printCanvas.Children.Clear();
            mouseClickedTag = 0;
        }

        private void ExitIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseClickedTag = 100;
        }

        private void ExitIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseClickedTag != 100)
            {
                mouseClickedTag = 0;
                return;
            }
            this.Close();
        }

        private void StartIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseClickedTag = 90;
        }

        private void Starton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseClickedTag != 90)
            {
                mouseClickedTag = 0;
                return;
            }
            Image img = sender as Image;
            if (int.Parse(img.Tag.ToString()) == 0)
            {
                img.SetValue(Button.StyleProperty, Application.Current.Resources["StopIcon"]);
                img.Tag = "1";
            }
            else {
                img.SetValue(Button.StyleProperty, Application.Current.Resources["StartIcon"]);
                img.Tag = "0";
            }
            mouseClickedTag = 0;
        }
    }
}
