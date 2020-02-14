using Emgu.CV;
using System;
using System.Collections.Generic;
using System.IO;
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
        //区分老师与学生
        Boolean isStudent = false;
        //判断是否有权控制画板
        Boolean canControl = true;
        //摄像头
        Capture capture;
        //本地摄像头显示位置
        int cameraPosition;

        /// <summary>
        /// 原型中暂时利用Tag分辨老师与学生
        /// </summary>
        /// <param name="tag"></param>
        public LiveWindow(int tag)
        {
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
            if (tag == 1)
                StudentInitialization();
            else
                TeacherInitialization();

        }

        /// <summary>
        /// 作为老师初始化窗口
        /// </summary>
        private void TeacherInitialization() {
            isStudent = false;
            canControl = true;

            InitializeCameraArea(0);

            canvasVLC.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 作为学生初始化窗口，主要包括大量禁用按钮
        /// </summary>
        private void StudentInitialization() {
            isStudent = true;
            canControl = false;

            InitializeCameraArea(1);

            DeactivateComputerIcon(computerIcon_1);
            DeactivateComputerIcon(computerIcon_2);
            DeactivateComputerIcon(computerIcon_3);
            DeactivateComputerIcon(computerIcon_4);
            DeactivateComputerIcon(computerIcon_5);
            DeactivateRecordIcon(recordIcon_1);
            DeactivateRecordIcon(recordIcon_2);
            DeactivateRecordIcon(recordIcon_3);
            DeactivateRecordIcon(recordIcon_4);
            DeactivateRecordIcon(recordIcon_5);

            deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteInactiveIcon"]);
            deleteIcon.Cursor = Cursors.Arrow;

            printCanvas.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// 禁用移交控制权按钮
        /// </summary>
        /// <param name="button"></param>
        private void DeactivateComputerIcon(Image button) { 
            button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerInactiveIcon"]);
            button.Cursor = Cursors.Arrow;
        }
        /// <summary>
        /// 禁用静音按钮
        /// </summary>
        /// <param name="button"></param>
        private void DeactivateRecordIcon(Image button) {
            button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordInactiveIcon"]);
            button.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// 初始化摄像头，根据参数设置不必要的控件隐藏
        /// </summary>
        /// <param name="selfCamera"></param>
        private void InitializeCameraArea(int selfCamera)
        {
            capture = new Capture();
            if (selfCamera == 0) {
                teacherVLC.Visibility = Visibility.Collapsed;
                studentCameraArea_1.Visibility = Visibility.Collapsed;
                studentCameraArea_2.Visibility = Visibility.Collapsed;
                studentCameraArea_3.Visibility = Visibility.Collapsed;
                studentCameraArea_4.Visibility = Visibility.Collapsed;
                studentCameraArea_5.Visibility = Visibility.Collapsed;

                cameraPosition = 0;
                capture.ImageGrabbed += Capture_ImageGrabbed;
            }
            else if (selfCamera == 1)
            {
                teacherCameraArea.Visibility = Visibility.Collapsed;
                studentVLC_1.Visibility = Visibility.Collapsed;
                studentCameraArea_2.Visibility = Visibility.Collapsed;
                studentCameraArea_3.Visibility = Visibility.Collapsed;
                studentCameraArea_4.Visibility = Visibility.Collapsed;
                studentCameraArea_5.Visibility = Visibility.Collapsed;

                cameraPosition = 1;
                capture.ImageGrabbed += Capture_ImageGrabbed;
            }
            else if (selfCamera == 2)
            {
                teacherCameraArea.Visibility = Visibility.Collapsed;
                studentCameraArea_1.Visibility = Visibility.Collapsed;
                studentVLC_2.Visibility = Visibility.Collapsed;
                studentCameraArea_3.Visibility = Visibility.Collapsed;
                studentCameraArea_4.Visibility = Visibility.Collapsed;
                studentCameraArea_5.Visibility = Visibility.Collapsed;

                cameraPosition = 2;
                capture.ImageGrabbed += Capture_ImageGrabbed;
            }
            else if (selfCamera == 3)
            {
                teacherCameraArea.Visibility = Visibility.Collapsed;
                studentCameraArea_1.Visibility = Visibility.Collapsed;
                studentCameraArea_2.Visibility = Visibility.Collapsed;
                studentVLC_3.Visibility = Visibility.Collapsed;
                studentCameraArea_4.Visibility = Visibility.Collapsed;
                studentCameraArea_5.Visibility = Visibility.Collapsed;

                cameraPosition = 3;
                capture.ImageGrabbed += Capture_ImageGrabbed;
            }
            else if (selfCamera == 4)
            {
                teacherCameraArea.Visibility = Visibility.Collapsed;
                studentCameraArea_1.Visibility = Visibility.Collapsed;
                studentCameraArea_2.Visibility = Visibility.Collapsed;
                studentCameraArea_3.Visibility = Visibility.Collapsed;
                studentVLC_4.Visibility = Visibility.Collapsed;
                studentCameraArea_5.Visibility = Visibility.Collapsed;

                cameraPosition = 4;
                capture.ImageGrabbed += Capture_ImageGrabbed;
            }
            else if (selfCamera == 5)
            {
                teacherCameraArea.Visibility = Visibility.Collapsed;
                studentCameraArea_1.Visibility = Visibility.Collapsed;
                studentCameraArea_2.Visibility = Visibility.Collapsed;
                studentCameraArea_3.Visibility = Visibility.Collapsed;
                studentCameraArea_4.Visibility = Visibility.Collapsed;
                studentVLC_5.Visibility = Visibility.Collapsed;

                cameraPosition = 5;
                capture.ImageGrabbed += Capture_ImageGrabbed;
            }

        }

        /// <summary>
        /// 将摄像头内容显示到窗口上的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Capture_ImageGrabbed(object sender, EventArgs e)
        {
            //新建一个Mat
            Mat frame = new Mat();
            //将得到的图像检索到frame中
            capture.Retrieve(frame, 0);
            //将图像赋值到IBShow的Image中完成显示
            switch (cameraPosition) {
                case 0:
                    teacherCamera.Image = frame;
                    break;
                case 1:
                    studentCamera_1.Image = frame;
                    break;
                case 2:
                    studentCamera_2.Image = frame;
                    break;
                case 3:
                    studentCamera_3.Image = frame;
                    break;
                case 4:
                    studentCamera_4.Image = frame;
                    break;
                case 5:
                    studentCamera_5.Image = frame;
                    break;
            }
        }


        /// <summary>
        /// 鼠标按下事件，此按钮用于移交控制权。点击确认变量数值：十位表征序号，个位为 0 表征是控制权按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComputerIcon_MouseDown(object sender, MouseButtonEventArgs e) {
            if (isStudent == true)
                return;
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            mouseClickedTag = tagHead * 10;
        }

        /// <summary>
        /// 鼠标抬起事件，此按钮用于移交控制权
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComputerIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isStudent == true)
                return;
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

        /// <summary>
        /// 鼠标按下事件，此按钮用于静音。点击确认变量数值：十位表征序号，个位为 1 表征是控制权按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isStudent == true)
                return;
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            mouseClickedTag = tagHead * 10 + 1;
        }

        /// <summary>
        /// 鼠标抬起事件，此按钮用于静音
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isStudent == true)
                return;
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

        /// <summary>
        /// 鼠标落下事件，此事件用于控制初始化画图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            if (canControl == false)
                return;
            startPoint = e.GetPosition(printCanvas);
            pointList.Add(startPoint);
            isDrawing = true;
        }

        /// <summary>
        /// 鼠标移动事件，此事件用于绘图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (canControl == false)
                return;
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

        /// <summary>
        /// 鼠标抬起事件，此事件用于终止画图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (canControl == false)
                return;
            isDrawing = false;
        }

        /// <summary>
        /// 鼠标移动出Canvas事件，此事件用于终止画图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void printCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (canControl == false)
                return;
            isDrawing = false;
        }

        /// <summary>
        /// 鼠标按下事件，此按钮用于清楚Canvas。点击确认变量数值： 1 表征是删除按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (canControl == false)
                return;
            mouseClickedTag = 1;
        }

        /// <summary>
        /// 鼠标抬起事件，此按钮用于清除Canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (canControl == false)
                return;
            if (mouseClickedTag != 1) {
                mouseClickedTag = 0;
                return;
            }
            printCanvas.Children.Clear();
            mouseClickedTag = 0;
        }

        /// <summary>
        /// 鼠标落下事件，此按钮用于关闭此窗口。点击确认变量数值： 100 表征是关闭按钮。 
        /// /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseClickedTag = 100;
        }

        /// <summary>
        /// 鼠标抬起事件，此按钮用于关闭此窗口。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseClickedTag != 100)
            {
                mouseClickedTag = 0;
                return;
            }
            capture.Dispose();
            this.Close();
        }

        /// <summary>
        /// 鼠标落下事件，此按钮用于终止或开始直播。点击确认变量数值： 90 表征是删除按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseClickedTag = 90;
        }

        /// <summary>
        /// 鼠标抬起事件，此按钮用于终止或开始直播。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                capture.Start();
                img.SetValue(Button.StyleProperty, Application.Current.Resources["StopIcon"]);
                img.Tag = "1";
            }
            else {
                capture.Stop();
                img.SetValue(Button.StyleProperty, Application.Current.Resources["StartIcon"]);
                img.Tag = "0";
            }
            mouseClickedTag = 0;
        }
    }
}
