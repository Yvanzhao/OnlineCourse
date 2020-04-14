using Emgu.CV;
using System;
using System.Collections.Generic;
using AForge.Video.DirectShow;
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
        //整张图的线集合
        List<List<Point>> lineList;
        //某条线的点集合
        List<Point> pointList;
        //整张图的线的颜色集合
        List<SolidColorBrush> colorList;
        // 绘画状态
        Boolean isDrawing = false;
        //区分老师与学生
        Boolean isStudent = false;
        //判断是否有权控制画板
        Boolean canControl = true;
        //摄像头
        Capture capture;
        //房间ID
        string roomId;
        //当前用户ID
        int userId;
        //当前用户摄像头位置。cameraPosition已经被整合进该变量，如果去除注释后报错请自行替换
        int userPosition;
        //当前画笔颜色
        SolidColorBrush currentColor;

        /// <summary>
        /// 暂时利用Tag分辨老师与学生
        /// </summary>
        /// <param name="tag"></param>
        public LiveWindow(int tag,string roomIdIn,int userIDIn)
        {
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
            roomId = roomIdIn;
            userId = userIDIn;
            currentColor = new SolidColorBrush(Colors.Black);
            if (tag == 1)
            {
                userPosition = 1;//需要去服务器获取
                StudentInitialization();
            }
            else {
                userPosition = 0;
                TeacherInitialization();
            }
                

        }

        /// <summary>
        /// 作为老师初始化窗口
        /// </summary>
        private void TeacherInitialization() {
            lineList = new List<List<Point>>();
            colorList = new List<SolidColorBrush>();
            isStudent = false;
            canControl = true;

            InitializeCameraArea();
        }

        /// <summary>
        /// 作为学生初始化窗口，主要包括大量禁用按钮
        /// </summary>
        private void StudentInitialization() {
            isStudent = true;
            canControl = false;

            InitializeCameraArea();

            DeactivateComputerIcons(0);
            DeactivateRecordIcons(userPosition);

            DeactivateCanvasIcons();
        }
        /// <summary>
        /// 启用全部的控制权移交按钮
        /// </summary>
        private void ActivateComputerIcons() {
            ActivateComputerIcon(computerIcon_1,false);
            ActivateComputerIcon(computerIcon_2, false);
            ActivateComputerIcon(computerIcon_3, false);
            ActivateComputerIcon(computerIcon_4, false);
            ActivateComputerIcon(computerIcon_5, false);
        }
        /// <summary>
        /// 批量禁用移交控制权按钮。0时为学生初始化，1~5为将控制权转交到对应学生时的教师端
        /// </summary>
        /// <param name="position"></param>
        private void DeactivateComputerIcons(int position) {
            DeactivateComputerIcon(computerIcon_1);
            DeactivateComputerIcon(computerIcon_2);
            DeactivateComputerIcon(computerIcon_3);
            DeactivateComputerIcon(computerIcon_4);
            DeactivateComputerIcon(computerIcon_5);
            switch (position) {
                case 1:
                    ActivateComputerIcon(computerIcon_1, true);
                    break;
                case 2:
                    ActivateComputerIcon(computerIcon_2, true);
                    break;
                case 3:
                    ActivateComputerIcon(computerIcon_3, true);
                    break;
                case 4:
                    ActivateComputerIcon(computerIcon_4, true);
                    break;
                case 5:
                    ActivateComputerIcon(computerIcon_5, true);
                    break;
            }
        }
        /// <summary>
        /// 启用移交控制权按钮，isActivated表示启用时按钮的状态是否为已激活
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isActivated"></param>
        private void ActivateComputerIcon(Image button,Boolean isActivated) {
            if (isActivated)
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivedIcon"]);
            }
            else {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]);
            }
            
            button.Cursor = Cursors.Hand;
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
        /// 批量禁用移交控制权按钮。0时为学生初始化，1~5为将控制权转交到对应学生时的教师端
        /// </summary>
        /// <param name="position"></param>
        private void DeactivateRecordIcons(int position)
        {
            DeactivateRecordIcon(recordIcon_1);
            DeactivateRecordIcon(recordIcon_2);
            DeactivateRecordIcon(recordIcon_3);
            DeactivateRecordIcon(recordIcon_4);
            DeactivateRecordIcon(recordIcon_5);
            switch (position)
            {
                case 1:
                    ActivateRecordIcon(recordIcon_1, true);
                    break;
                case 2:
                    ActivateRecordIcon(recordIcon_2, true);
                    break;
                case 3:
                    ActivateRecordIcon(recordIcon_3, true);
                    break;
                case 4:
                    ActivateRecordIcon(recordIcon_4, true);
                    break;
                case 5:
                    ActivateRecordIcon(recordIcon_5, true);
                    break;
            }
        }
        /// <summary>
        /// 启用禁音按钮，isActivated表示启用时按钮的状态是否为已激活
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isActivated"></param>
        private void ActivateRecordIcon(Image button, Boolean isActivated)
        {
            if (isActivated)
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]);
            }
            else
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]);
            }

            button.Cursor = Cursors.Hand;
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
        /// 启用画布相关按钮
        /// </summary>
        /// <param name="button"></param>
        private void ActivateCanvasIcons() {
            printCanvas.Cursor = Cursors.Cross;
            deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteIcon"]);
            deleteIcon.Cursor = Cursors.Hand;
            colorChooser.SetValue(Button.StyleProperty, Application.Current.Resources["ColorChoser"]);
            colorChooser.Fill = currentColor;
            colorChooser.Cursor = Cursors.Hand;
        }
        /// <summary>
        /// 禁用画布相关按钮
        /// </summary>
        /// <param name="button"></param>
        private void DeactivateCanvasIcons() {
            printCanvas.Cursor = Cursors.Arrow;
            deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteInactiveIcon"]);
            deleteIcon.Cursor = Cursors.Arrow;
            colorChooser.SetValue(Button.StyleProperty, Application.Current.Resources["ColorChoserDiabled"]);
            colorChooser.Cursor = Cursors.Arrow;
        }
        
        /// <summary>
        /// 初始化摄像头，根据参数设置不必要的控件隐藏
        /// </summary>
        /// <param name="selfCamera"></param>
        private void InitializeCameraArea()
        {
            teacherCameraArea.Visibility = Visibility.Collapsed;
            studentCameraArea_1.Visibility = Visibility.Collapsed;
            studentCameraArea_2.Visibility = Visibility.Collapsed;
            studentCameraArea_3.Visibility = Visibility.Collapsed;
            studentCameraArea_4.Visibility = Visibility.Collapsed;
            studentCameraArea_5.Visibility = Visibility.Collapsed;

            //模拟学生接入视频
            //VLC播放器的安装位置，我的VLC播放器安装在D:\Program Files (x86)\VideoLAN\VLC文件夹下。
            string currentDirectory = @"D:\Program Files\VideoLAN\VLC";
            var vlcLibDirectory = new System.IO.DirectoryInfo(currentDirectory);

            var options = new string[]
            {
                "--file-logging", "-vvv", "--logfile=Logs.log"
            };

            if (userPosition == 0) {
                teacherCameraArea.Visibility = Visibility.Visible;
                teacherVLC.Visibility = Visibility.Collapsed;

                capture = new Capture();
                capture.ImageGrabbed += Capture_ImageGrabbed;
                capture.Start();

                string audio = "";
                string video = "";//设备名称
                getDeviceName(ref audio, ref video);
                string offset_x = "40";//录屏的左上角坐标
                string offset_y = "20";//
                string videoSize = "175x175";//录屏的大小
                LiveCapture.Start(audio, video, offset_x, offset_y, videoSize, roomId);


                //初始化播放器
                studentVLC_1.SourceProvider.CreatePlayer(vlcLibDirectory, options);
                studentVLC_1.SourceProvider.MediaPlayer.Play(new Uri("rtmp://172.19.241.249:8082/stu1"));

            }
            else if (userPosition == 1)
            {
                studentCameraArea_1.Visibility = Visibility.Visible;
                studentVLC_1.Visibility = Visibility.Collapsed;

                capture = new Capture();
                capture.ImageGrabbed += Capture_ImageGrabbed;
                capture.Start();

                string audio = "";
                string video = "";//设备名称
                getDeviceName(ref audio, ref video);
                string offset_x = "240";//录屏的左上角坐标
                string offset_y = "220";//
                string videoSize = "175x175";//录屏的大小
                LiveCapture.Start(audio, video, offset_x, offset_y, videoSize, "stu1");


                //初始化播放器
                teacherVLC.SourceProvider.CreatePlayer(vlcLibDirectory, options);
                teacherVLC.SourceProvider.MediaPlayer.Play(new Uri("rtmp://172.19.241.249:8082/"+roomId));
            }
            else if (userPosition == 2)
            {
                studentCameraArea_2.Visibility = Visibility.Visible;
                studentVLC_2.Visibility = Visibility.Collapsed;
            }
            else if (userPosition == 3)
            {
                studentCameraArea_3.Visibility = Visibility.Visible;
                studentVLC_3.Visibility = Visibility.Collapsed;
            }
            else if (userPosition == 4)
            {
                studentCameraArea_4.Visibility = Visibility.Visible;
                studentVLC_4.Visibility = Visibility.Collapsed;
            }
            else if (userPosition == 5)
            {
                studentCameraArea_5.Visibility = Visibility.Visible;
                studentVLC_5.Visibility = Visibility.Collapsed;
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
            switch (userPosition) {
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
            if (canControl == false && isStudent == true)
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
                if (isStudent == true)
                {//学生主动交还控制权的情况
                    DeactivateComputerIcons(0);
                    DeactivateCanvasIcons();
                    canControl = false;
                    //此处添加学生交还控制权的方法
                }
                else {
                    //老师手动拿回控制权的时候
                    //根据状态不同进行切换
                    if (tagTail == 0)
                    {
                        image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivedIcon"]);
                        image.Tag = tagHead + "" + 1;
                        canControl = false;
                        DeactivateComputerIcons(tagHead);//暂时禁用老师向其他学生交出控制权并禁用老师的画板
                        DeactivateCanvasIcons();
                        //此处添加老师移交控制权的方法
                    }
                    else
                    {
                        image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]);
                        image.Tag = tagHead + "" + 0;
                        canControl = true;
                        ActivateComputerIcons();
                        ActivateCanvasIcons();
                        //此处添加老师拿回控制权的方法
                    }
                }
               
               //重置状态值避免bug
               mouseClickedTag = 0;
            }        
        }

        /// <summary>
        /// 鼠标按下事件，此按钮用于静音。点击确认变量数值：十位表征序号，个位为 1 表征是录音按钮。
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
                if (tagHead == 0)
                {
                     //此处添加禁用远端某学生录音的方法
                }
                else if (tagHead == userPosition)
                {
                    //此处添加学生禁用自己录音的方法
                }
                else
                    return;
                //根据状态不同进行切换，此处仅负责按钮的样式
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
            pointList = new List<Point>();
            pointList.Add(startPoint);
            lineList.Add(pointList);
            colorList.Add(currentColor);
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
                        l.Stroke = currentColor;
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
        /// 根据已有线集合与颜色集合重绘画板，用于学生端实时获取教师端画板
        /// </summary>
        private void Redraw()
        {
            if (lineList.Count > 0) {
                for (int numberOfLines = 0; numberOfLines < lineList.Count; numberOfLines++) {
                    if (lineList[numberOfLines].Count > 1) {
                        for (int numberOfPoints = 1; numberOfPoints < lineList[numberOfLines].Count; numberOfPoints++) {
                            var l = new Line();
                            l.Stroke = colorList[numberOfLines];
                            l.StrokeThickness = 1;
                            l.X1 = (lineList[numberOfLines])[numberOfPoints - 1].X;  // count-2  保证 line的起始点为点集合中的倒数第二个点。
                            l.Y1 = (lineList[numberOfLines])[numberOfPoints - 1].Y;
                            // 终点X,Y 为当前point的X,Y
                            l.X2 = (lineList[numberOfLines])[numberOfPoints].X;
                            l.Y2 = (lineList[numberOfLines])[numberOfPoints].Y;
                            printCanvas.Children.Add(l);
                        }
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
        /// 鼠标按下事件，此按钮用于清除Canvas。点击确认变量数值： 1 表征是删除按钮。
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
            colorList = new List<SolidColorBrush>();
            lineList = new List<List<Point>>();
            mouseClickedTag = 0;
        }

        /// <summary>
        /// 鼠标按下事件，此按钮用于打开颜色选择按钮。点击确认变量数值： 2 表征是颜色选择按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorChooser_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (canControl == false)
                return;
            mouseClickedTag = 2;
        }
        /// <summary>
        /// 鼠标抬起事件，此按钮用于打开颜色选择按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorChooser_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (canControl == false)
                return;
            if (mouseClickedTag != 2)
            {
                mouseClickedTag = 0;
                return;
            }
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            //允许使用该对话框的自定义颜色  
            colorDialog.AllowFullOpen = true;
            colorDialog.FullOpen = true;
            colorDialog.ShowHelp = true;
            //初始化当前文本框中的字体颜色，  
            colorDialog.Color = System.Drawing.Color.White;

            //当用户在ColorDialog对话框中点击"确定"按钮  
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                currentColor = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                colorChooser.Fill = currentColor;
            }
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
        /// 鼠标抬起事件，此按钮用于关闭此窗口并打开房间管理界面。
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
            RoomControlWindow roomControl = new RoomControlWindow(userId);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            roomControl.Show();
        }

        


       private void getDeviceName(ref string audio, ref string video)
       {
           FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
           FilterInfoCollection audeoDevices = new FilterInfoCollection(FilterCategory.AudioInputDevice);
           if (videoDevices.Count == 0)
               throw new ApplicationException();
           if (audeoDevices.Count == 0)
               throw new ApplicationException();
           audio = audeoDevices[0].Name;
           video = videoDevices[0].Name;
       }
       
    }
}
