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
using Unosquare.FFME.Common;
using System.Threading;

namespace OnlineCourse
{
    /// <summary>
    /// LiveWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LiveWindow : Window
    {
        //用于记录鼠标落下时的位置，避免落下与抬起位置不同造成的bug
        int mouseClickedTag = 0;
        //整张图的线集合
        List<List<double[]>> linesList;
        //某条线的点集合
        List<double[]> pointsList;
        //整张图的线的颜色集合
        List<byte[]> colorList;
        //List<int[]> colorList;
        // 绘画状态
        Boolean isDrawing = false;
        //区分老师与学生
        Boolean isStudent = false;
        //判断是否有权控制画板
        Boolean canControl = true;
        //房间ID
        string roomId;
        //当前用户
        user user;
        //当前用户摄像头位置。cameraPosition已经被整合进该变量，如果去除注释后报错请自行替换
        int userPosition;
        //当前画笔颜色
        byte[] currentColor;
        //连接服务器
        Server.ServerService server;
        //推流工具
        LiveCapture pushTool;
        //对应位置是否有学生
        Boolean[] hasStudent;

        /// <summary>
        /// 暂时利用Tag分辨老师与学生
        /// </summary>
        /// <param name="tag"></param>
        public LiveWindow(int tag,string roomIdIn,user userIn, Server.ServerService server)
        {

            pushTool = new LiveCapture();

            this.server = server;
            if (server == null) {
                RoomControlWindow roomControl = new RoomControlWindow(userIn,this.server);
                Window thisWindow = Window.GetWindow(this);
                thisWindow.Close();
                roomControl.Show();
            }
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
            
            roomId = roomIdIn;
            user = userIn;

            currentColor = new byte[4];
            currentColor[0] = 0xFF;
            currentColor[1] = 0x00;
            currentColor[2] = 0x00;
            currentColor[3] = 0x00;

            

            if (tag == 1)
            {
                userPosition = this.server.getUserPosition(roomId, user.userId);
                StudentInitialization();
                startStudentThread();
            }
            else {
                userPosition = 0;
                TeacherInitialization();
                startTeacherThread();
            }

            // 开始推流  暂时只有老师的是对的
            pushTool.StartCamera(roomId + userPosition);
            // pushTool.StartDesktop("0", "0", "100x100", "" + userPosition);

        }

        /// <summary>
        /// 作为老师初始化窗口
        /// </summary>
        private void TeacherInitialization() {
            linesList = new List<List<double[]>>();
            colorList = new List<byte[]>();
            isStudent = false;
            hasStudent = server.checkStudent(roomId);
            canControl = true;
        }

        /// <summary>
        /// 确认特定位置是否有学生。
        /// </summary>
        private void checkStudent() {
            Boolean[] newHasStudent = server.checkStudent(roomId);
            for (int position = 0; position < 5; position++) {
                if (newHasStudent[position] != hasStudent[position]) {
                    if (newHasStudent[position])
                    {
                        ActivateComputerIcon(position + 1, false);
                        ActivateRecordIcon(position + 1, true);
                    }
                    else {
                        DeactivateComputerIcon(position + 1);
                        DeactivateRecordIcon(position + 1, true);
                    }
                }
            }
        }

        /// <summary>
        /// 作为学生初始化窗口，主要包括大量禁用按钮
        /// </summary>
        private void StudentInitialization() {
            isStudent = true;
            canControl = false;

            hasStudent = server.checkStudent(roomId);

            DeactivateComputerIcons(0);
            DeactivateRecordIcons();

            DeactivateCanvasIcons();
        }

        /// <summary>
        /// 根据userPosition获取对应的控制权按钮
        /// </summary>
        /// <param name="userPosition"></param>
        /// <returns></returns>
        private Image getComputerIcon(int position) {
            switch (position) {
                case 1:
                    return computerIcon_1;
                case 2:
                    return computerIcon_2;
                case 3:
                    return computerIcon_3;
                case 4:
                    return computerIcon_4;
                case 5:
                    return computerIcon_5;
            }
            return null;
        }
        /// <summary>
        /// 启用全部的控制权移交按钮
        /// </summary>
        private void ActivateComputerIcons() {
            for (int position = 1; position < 6; position++) {
                getComputerIcon(position).Tag = (int)((userPosition * 10) + 0);
                getComputerIcon(position).Visibility = Visibility.Hidden;
                if (hasStudent[position - 1])
                    ActivateComputerIcon(position, false);
            }
        }
        /// <summary>
        /// 批量禁用移交控制权按钮。0时为学生初始化，1~5为将控制权转交到对应学生时的教师端
        /// </summary>
        /// <param name="position"></param>
        private void DeactivateComputerIcons(int position) {
            for (int deactivatePosition = 1; deactivatePosition < 6; deactivatePosition++) {
                if (deactivatePosition != position) { 
                    DeactivateComputerIcon(deactivatePosition);
                    getComputerIcon(deactivatePosition).Tag = (int)((userPosition * 10) + 0);
                    getComputerIcon(deactivatePosition).Visibility = Visibility.Hidden;
                }
                    
                else {
                    if (position > 0) {
                        if (hasStudent[position - 1])
                            ActivateComputerIcon(deactivatePosition, true);
                    }
                }
                    
            }
        }
        /// <summary>
        /// 启用移交控制权按钮，isActivated表示启用时按钮的状态是否为已获得控制权
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isActivated"></param>
        private void ActivateComputerIcon(int position,Boolean isActivated) {
            Image button = getComputerIcon(position);
            if (button == null)
                return;
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
        private void DeactivateComputerIcon(int position) {
            Image button = getComputerIcon(position);
            if (button == null)
                return;
            button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerInactiveIcon"]);
            button.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// 检查是否具有控制权。
        /// </summary>
        private void checkControl() {
            Boolean[] hasControl = server.checkControl(roomId);
            for (int position = 0; position < 5; position++) {
                if (hasControl[position]) {
                    if (userPosition == position + 1)
                    {
                        //该学生自己获得控制权
                        if (canControl == false) {
                            canControl = true;
                            DeactivateComputerIcons(userPosition);
                            ActivateCanvasIcons();
                            getComputerIcon(userPosition).Tag = (int)((userPosition * 10) + 1);
                            getComputerIcon(userPosition).Visibility = Visibility.Visible;
                        }                      
                        return;
                    }
                    else {
                        //其他学生获得控制权
                        DeactivateComputerIcons(0);
                        getComputerIcon(position + 1).SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivatedWhenInactiveIcon"]);
                        getComputerIcon(position + 1).Tag = (int)((userPosition * 10) + 1);
                        getComputerIcon(position + 1).Visibility = Visibility.Visible;
                        return;
                    }
                }
            }
            if (isStudent) {
                //教师强制取回控制权
                DeactivateComputerIcons(0);
                if (canControl == true) {
                    canControl = false;
                    isDrawing = false;
                    DeactivateCanvasIcons();
                }              
            }
                
            else {
                //教师端学生主动交还控制权
                if (canControl == false)
                {
                    canControl = true;
                    ActivateComputerIcons();
                    ActivateCanvasIcons();
                    startTeacherThread();
                }
            }
                
        }

        /// <summary>
        /// 根据position获得对应静音按钮
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Image getRecordIcon(int position) {
            switch (position)
            {
                case 1:
                    return recordIcon_1;
                case 2:
                    return recordIcon_2;
                case 3:
                    return recordIcon_3;
                case 4:
                    return recordIcon_4;
                case 5:
                    return recordIcon_5;
            }
            return null;
        }
        /// <summary>
        /// 批量禁用静音按钮。1~5表示各个学生自己永远允许静音自己
        /// </summary>
        /// <param name="position"></param>
        private void DeactivateRecordIcons()
        {
            for (int deactivatePosition = 1; deactivatePosition < 6; deactivatePosition++)
            {
                if (deactivatePosition != userPosition)
                    DeactivateRecordIcon(deactivatePosition,true);
                else
                    ActivateRecordIcon(deactivatePosition, true);
            }
        }
        /// <summary>
        /// 启用禁音按钮，notSilenced表示启用时按钮的状态是否为已经被静音
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isActivated"></param>
        private void ActivateRecordIcon(int position, Boolean notSilenced)
        {
            Image button = getRecordIcon(position);
            if (button == null)
                return;
            if (notSilenced)
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]);
            }
            else
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]);
            }

            button.Cursor = Cursors.Hand;
        }
        /// <summary>
        /// 禁用静音按钮，notSilenced表示启用时按钮的状态是否为已经被静音
        /// </summary>
        /// <param name="button"></param>
        private void DeactivateRecordIcon(int position, Boolean notSilenced) {
            Image button = getRecordIcon(position);
            if (button == null)
                return;
            if (notSilenced)
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordInactiveIcon"]);
            }
            else
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedWhenInactiveIcon"]);
            }
            button.Cursor = Cursors.Arrow;
        }
        /// <summary>
        /// 将某一位置的按钮变为被静音状态。isActivated表示静音时该按钮是否可用
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isActivated"></param>
        private void BanRecord(int position,Boolean isActivated) {
            Image button = getRecordIcon(position);
            if (button == null)
                return;
            if (isActivated)
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]);
            }
            else
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedWhenInactiveIcon"]);
            }
        }
        /// <summary>
        /// 将某一位置的按钮取消被静音状态。isActivated表示静音时该按钮是否可用
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isActivated"></param>
        private void EnableRecord(int position, Boolean isActivated)
        {
            Image button = getRecordIcon(position);
            if (button == null)
                return;
            if (isActivated)
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]);
            }
            else
            {
                button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordInactiveIcon"]);
            }
        }

        /// <summary>
        /// 检查房间静音情况
        /// </summary>
        private void checkSilenced() {
            Boolean[] silenced = server.checkSilenced(roomId);
            for (int position = 0; position < 5; position++) {
                if (silenced[position])
                {
                    if (isStudent == false  || userPosition == position + 1)
                        BanRecord(position + 1, true);
                    else
                        BanRecord(position + 1, false);
                }
                else {
                    if (isStudent == false || userPosition == position + 1)
                        EnableRecord(position + 1, true);
                    else
                        EnableRecord(position + 1, false);
                }
            }
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
            colorChooser.Fill = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
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
        /// 初始化播放器并拉流
        private async void teaMedia_Loaded(object sender, RoutedEventArgs e)
        {
            teacherMedia.MediaInitializing += OnMediaInitializing;
            await teacherMedia.Open(new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "0"));
        }

        private async void stu1Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia1.MediaInitializing += OnMediaInitializing;
            await studentMedia1.Open(new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "1"));
        }

        private async void stu2Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia2.MediaInitializing += OnMediaInitializing;
            await studentMedia2.Open(new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "2"));
        }

        private async void stu3Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia3.MediaInitializing += OnMediaInitializing;
            await studentMedia3.Open(new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "3"));
        }

        private async void stu4Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia4.MediaInitializing += OnMediaInitializing;
            await studentMedia4.Open(new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "4"));
        }

        private async void stu5Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia5.MediaInitializing += OnMediaInitializing;
            await studentMedia5.Open(new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "5"));
        }

        // 修改播放器缓冲
        private void OnMediaInitializing(object sender, MediaInitializingEventArgs e)
        {
            e.Configuration.GlobalOptions.FlagNoBuffer = true;
            //e.Configuration.PrivateOptions["flags"] = "low_delay";
            //e.Configuration.PrivateOptions["crf"] = "0";
            //e.Configuration.GlobalOptions.ProbeSize = 8192;
            //e.Configuration.GlobalOptions.MaxAnalyzeDuration = TimeSpan.FromMilliseconds(500);
        }

        // 静音自己
        private void mute_MediaOpening(object sender, MediaOpeningEventArgs e)
        {
            //switch (userPosition)
            //{
            //    case 0:
            //        try { teacherMedia.Dispatcher.Invoke(() => { teacherMedia.Volume = 0; }); } catch (Exception ex) { };break;
            //    case 1:
            //        try { studentMedia1.Dispatcher.Invoke(() => { studentMedia1.Volume = 0; }); } catch (Exception ex) { }; break;
            //    case 2:
            //        try { studentMedia2.Dispatcher.Invoke(() => { studentMedia2.Volume = 0; }); } catch (Exception ex) { }; break;
            //    case 3:
            //        try { studentMedia3.Dispatcher.Invoke(() => { studentMedia3.Volume = 0; }); } catch (Exception ex) { }; break;
            //    case 4:
            //        try { studentMedia4.Dispatcher.Invoke(() => { studentMedia4.Volume = 0; }); } catch (Exception ex) { }; break;
            //    case 5:
            //        try { studentMedia5.Dispatcher.Invoke(() => { studentMedia5.Volume = 0; }); } catch (Exception ex) { }; break;
            //}

        }

        /// <summary>
        /// 鼠标按下事件，此按钮用于移交控制权。点击确认变量数值：十位表征序号，个位为 0 表征是控制权按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComputerIcon_MouseDown(object sender, MouseButtonEventArgs e) {
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            if (isStudent == false)
            {
                if (hasStudent[tagHead - 1] == false)//该学生不存在
                    return;
            }
            else if (canControl == false)//是学生并且不具备控制权
                return;
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
                    server.changeControl(roomId, userPosition, false);
                }
                else {
                    //老师手动拿回控制权的时候
                    //根据状态不同进行切换
                    if (tagTail == 0)
                    {
                        if (canControl == true)
                        {
                            image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivedIcon"]);
                            image.Tag = tagHead + "" + 1;
                            canControl = false;
                            DeactivateComputerIcons(tagHead);//暂时禁用老师向其他学生交出控制权并禁用老师的画板
                            DeactivateCanvasIcons();
                            //此处添加老师移交控制权的方法
                            server.changeControl(roomId, tagHead, true);
                            startStudentThread();
                        }
                    }
                    else
                    {
                        image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]);
                        image.Tag = tagHead + "" + 0;
                        canControl = true;
                        ActivateComputerIcons();
                        ActivateCanvasIcons();
                        //此处添加老师拿回控制权的方法
                        server.changeControl(roomId, tagHead, false);
                        startTeacherThread();
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
            
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            if (isStudent == false)
            {
                if (hasStudent[tagHead - 1] == false)//该学生不存在
                    return;
            }
            else if (tagHead != userPosition)//是学生并且点击的不是自己的按钮
                return;
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
                Console.WriteLine(tag + ":" + tagHead + ":" + tagTail);
                if (mouseClickedTag != tagHead * 10 + 1)
                {
                     mouseClickedTag = 0;
                     return;
                }
                if (isStudent == false)
                {
                    //此处添加禁用远端某学生录音的方法
                    if (tagTail == 0)
                        server.silenceStudent(tagHead, true);
                    else
                        server.silenceStudent(tagHead, false);
                }
                else if (tagHead == userPosition)
                {
                    //此处添加学生禁用自己录音的方法
                    if (tagTail == 0)
                        mute(tagHead);
                    else
                        unMute(tagHead);
                }
                else
                {
                    return;
                }
                    
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

        // 静音第number号
        private void mute(int number)
        {
            switch (number)
            {
                case 0:
                    try { teacherMedia.Dispatcher.Invoke(() => { teacherMedia.Volume = 0; }); } catch (Exception ex) { }; break;
                case 1:
                    try { studentMedia1.Dispatcher.Invoke(() => { studentMedia1.Volume = 0; }); } catch (Exception ex) { }; break;
                case 2:
                    try { studentMedia2.Dispatcher.Invoke(() => { studentMedia2.Volume = 0; }); } catch (Exception ex) { }; break;
                case 3:
                    try { studentMedia3.Dispatcher.Invoke(() => { studentMedia3.Volume = 0; }); } catch (Exception ex) { }; break;
                case 4:
                    try { studentMedia4.Dispatcher.Invoke(() => { studentMedia4.Volume = 0; }); } catch (Exception ex) { }; break;
                case 5:
                    try { studentMedia5.Dispatcher.Invoke(() => { studentMedia5.Volume = 0; }); } catch (Exception ex) { }; break;
            }
        }

        // 恢复第number号
        private void unMute(int number)
        {
            switch (number)
            {
                case 0:
                    try { teacherMedia.Dispatcher.Invoke(() => { teacherMedia.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 1:
                    try { studentMedia1.Dispatcher.Invoke(() => { studentMedia1.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 2:
                    try { studentMedia2.Dispatcher.Invoke(() => { studentMedia2.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 3:
                    try { studentMedia3.Dispatcher.Invoke(() => { studentMedia3.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 4:
                    try { studentMedia4.Dispatcher.Invoke(() => { studentMedia4.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 5:
                    try { studentMedia5.Dispatcher.Invoke(() => { studentMedia5.Volume = 0.7; }); } catch (Exception ex) { }; break;
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
            Point startPoint = e.GetPosition(printCanvas);
            pointsList = new List<double[]>();
            double[] startPointPosition = new double[2];
            startPointPosition[0] = startPoint.X;
            startPointPosition[1] = startPoint.Y;
            pointsList.Add(startPointPosition);

            linesList.Add(pointsList);
            colorList.Add(currentColor);
            isDrawing = true;

            //以下是将画板变化更新到服务器上
            server.updateCanvas(startPointPosition, currentColor, 1);
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
                int count = pointsList.Count(); // 总点数  
                double[] pointPosition = new double[2];
                pointPosition[0] = point.X;
                pointPosition[1] = point.Y;

                // 去重复点
                if (count > 0) {
                    if (point.X - pointsList[count - 1][0] != 0 || point.Y - pointsList[count - 1][1] != 0) {
                        pointsList.Add(pointPosition);

                        var l = new Line();
                        l.Stroke = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
                        l.StrokeThickness = 1;
                        if (count < 1)
                            return;
                        l.X1 = pointsList[count - 1][0];  // count-2  保证 line的起始点为点集合中的倒数第二个点。
                        l.Y1 = pointsList[count - 1][1];
                        // 终点X,Y 为当前point的X,Y
                        l.X2 = point.X;
                        l.Y2 = point.Y;
                        printCanvas.Children.Add(l);
                        //以下是将画板变化更新到服务器上
                        server.updateCanvas(pointPosition, null, 0);
                    }
                }
            }
        }

        /// <summary>
        /// 根据已有线集合与颜色集合重绘画板，用于学生端实时获取教师端画板
        /// </summary>
        private void Redraw()
        {
            if (linesList.Count > 0) {
                for (int numberOfLines = 0; numberOfLines < linesList.Count; numberOfLines++) {
                    if (linesList[numberOfLines].Count > 1) {
                        for (int numberOfPoints = 1; numberOfPoints < linesList[numberOfLines].Count; numberOfPoints++) {
                            var l = new Line();
                            l.Stroke = new SolidColorBrush(Color.FromArgb(colorList[numberOfLines][0], colorList[numberOfLines][1], colorList[numberOfLines][2], colorList[numberOfLines][3])); 
                            l.StrokeThickness = 1;
                            // count-1  保证 line的起始点为点集合中的倒数第二个点。
                            l.X1 = (linesList[numberOfLines])[numberOfPoints - 1][0];  
                            l.Y1 = (linesList[numberOfLines])[numberOfPoints - 1][1];
                            // 终点X,Y 为当前point的X,Y
                            l.X2 = (linesList[numberOfLines])[numberOfPoints][0];
                            l.Y2 = (linesList[numberOfLines])[numberOfPoints][1];
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
            colorList = new List<byte[]>();
            linesList = new List<List<double[]>>();
            mouseClickedTag = 0;

            //以下是将画板变化更新到服务器上
            server.updateCanvas(null, null, 2);
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
                currentColor = new byte[4];
                currentColor[0] = colorDialog.Color.A;
                currentColor[1] = colorDialog.Color.R;
                currentColor[2] = colorDialog.Color.G;
                currentColor[3] = colorDialog.Color.B;
                colorChooser.Fill = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
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

            pushTool.Quit();
            server.setEmptyPosition(roomId,userPosition);

            RoomControlWindow roomControl = new RoomControlWindow(user,this.server);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            roomControl.Show();
        }
        

        /// <summary>
        /// 鼠标移入镜头区域的函数，用于使隐藏的两个按钮展示出来
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraMouseEnter(object sender, MouseEventArgs e)
        {
            int tag = int.Parse((sender as Grid).Tag.ToString());
            SetButtonsVisibility(tag, true);
        }

        /// <summary>
        /// 鼠标移处镜头区域的函数，用于隐藏两个按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraMouseLeave(object sender, MouseEventArgs e)
        {
            int tag = int.Parse((sender as Grid).Tag.ToString());
            SetButtonsVisibility(tag, false);

        }

        /// <summary>
        /// 进行隐藏或展示的实际逻辑。隐藏时对于已经激活的按钮不进行隐藏
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="setVisible"></param>
        private void SetButtonsVisibility(int tag, Boolean setVisible)
        {
            if (setVisible)
            {
                switch (tag)
                {
                    case 1:
                        computerIcon_1.Visibility = Visibility.Visible;
                        recordIcon_1.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        computerIcon_2.Visibility = Visibility.Visible;
                        recordIcon_2.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        computerIcon_3.Visibility = Visibility.Visible;
                        recordIcon_3.Visibility = Visibility.Visible;
                        break;
                    case 4:
                        computerIcon_4.Visibility = Visibility.Visible;
                        recordIcon_4.Visibility = Visibility.Visible;
                        break;
                    case 5:
                        computerIcon_5.Visibility = Visibility.Visible;
                        recordIcon_5.Visibility = Visibility.Visible;
                        break;
                }
            }
            else
            {
                switch (tag)
                {
                    case 1:
                        if (int.Parse(computerIcon_1.Tag.ToString()) % 10 == 0)
                            computerIcon_1.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_1.Tag.ToString()) % 10 == 0)
                            recordIcon_1.Visibility = Visibility.Hidden;
                        break;
                    case 2:
                        if (int.Parse(computerIcon_2.Tag.ToString()) % 10 == 0)
                            computerIcon_2.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_2.Tag.ToString()) % 10 == 0)
                            recordIcon_2.Visibility = Visibility.Hidden;
                        break;
                    case 3:
                        if (int.Parse(computerIcon_3.Tag.ToString()) % 10 == 0)
                            computerIcon_3.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_3.Tag.ToString()) % 10 == 0)
                            recordIcon_3.Visibility = Visibility.Hidden;
                        break;
                    case 4:
                        if (int.Parse(computerIcon_4.Tag.ToString()) % 10 == 0)
                            computerIcon_4.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_4.Tag.ToString()) % 10 == 0)
                            recordIcon_4.Visibility = Visibility.Hidden;
                        break;
                    case 5:
                        if (int.Parse(computerIcon_5.Tag.ToString()) % 10 == 0)
                            computerIcon_5.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_5.Tag.ToString()) % 10 == 0)
                            recordIcon_5.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        /// <summary>
        /// 开启学生端线程
        /// </summary>
        private void startStudentThread()
        {
            Thread studentThread = new Thread(new ThreadStart(this.studentThread));
            studentThread.Start();
        }
        /// <summary>
        /// 未获得画板控制权时的线程，学生端即使获得画板控制权依然使用本线程
        /// </summary>
        private void studentThread() {
            while (true) {
                if (canControl == false)
                {
                    linesList = server.getLines(roomId);
                    colorList = server.getColors(roomId);
                    Redraw();
                }
                checkControl();
                checkSilenced();
                if (canControl && (isStudent == false))
                    break;
                Thread.Sleep(250);
            }
            
        }

        /// <summary>
        /// 开启老师端已控制线程
        /// </summary>
        private void startTeacherThread() {
            Thread teacherThread = new Thread(new ThreadStart(this.teacherThread));
            teacherThread.Start();
        }
        /// <summary>
        /// 教师端拥有画板控制权时的线程
        /// </summary>
        private void teacherThread() {
            while (true) {
                checkSilenced();
                if (canControl == false)
                    break;
                Thread.Sleep(250);
            }
        }
        
    }
}
