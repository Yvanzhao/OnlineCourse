using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Unosquare.FFME.Common;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;

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
        //当前画笔颜色
        byte[] currentColor;
        // 绘画状态
        Boolean isDrawing = false;
        
        //区分老师与学生
        Boolean isStudent = false;
        //判断是否有权控制画板
        int hasControl = 0;
        //房间ID
        string roomId;
        //当前用户
        user user;
        //当前用户摄像头位置。cameraPosition已经被整合进该变量，如果去除注释后报错请自行替换
        int userPosition;
        
        
        //连接服务器
        Server.ServerService server;
        
        //推流工具
        LiveCapture pushTool;
        
        //是否存在学生
        Boolean[] hasStudent;
        //服务器端的Socket
        Socket socketServer;
        //监听线程
        Thread serverThread;

        //绘图时用于保存所有命令的string
        string drawOrder;

        //服务器地址
        static string serverIP = "172.19.241.249";
        //服务器地址
        static string streamIP = "172.19.241.249:1935";
        //服务器Socket端口
        static int serverPort = 8085;
        //老师与学生的地址
        Uri teacherAddress;
        Uri studentAddress1;
        Uri studentAddress2;
        Uri studentAddress3;
        Uri studentAddress4;
        Uri studentAddress5;
        Uri studentAudio1;
        Uri studentAudio2;
        Uri studentAudio3;
        Uri studentAudio4;
        Uri studentAudio5;

        Boolean isClosing;


        /// <summary>
        /// 利用tag 分辨老师与学生
        /// </summary>
        /// <param name="tag"></param>
        public LiveWindow(int tag,string roomIdIn,user userIn, Server.ServerService server)
        {
            pushTool = new LiveCapture();

            this.server = server;

            if (server == null) {
                RoomControlWindow roomControl = new RoomControlWindow(userIn,this.server);
                Window thiswindow = Window.GetWindow(this);
                thiswindow.Close();
                roomControl.Show();
            }
            InitializeComponent();
            this.WindowState = System.Windows.WindowState.Maximized;
            Window thisWindow = Window.GetWindow(this);
            thisWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

            //变量赋值与初始化
            hasStudent = new Boolean[6] { true, false, false, false, false, false };
            roomId = roomIdIn;
            teacherAddress = new Uri("rtmp://" + streamIP + "/live/" + roomId + "0");
            studentAddress1 = new Uri("rtmp://" + streamIP + "/live/" + roomId + "1");
            studentAddress2 = new Uri("rtmp://" + streamIP + "/live/" + roomId + "2");
            studentAddress3 = new Uri("rtmp://" + streamIP + "/live/" + roomId + "3");
            studentAddress4 = new Uri("rtmp://" + streamIP + "/live/" + roomId + "4");
            studentAddress5 = new Uri("rtmp://" + streamIP + "/live/" + roomId + "5");
            studentAudio1 = new Uri("rtmp://" + streamIP + "/onlyaudio/" + roomId + "1");
            studentAudio2 = new Uri("rtmp://" + streamIP + "/onlyaudio/" + roomId + "2");
            studentAudio3 = new Uri("rtmp://" + streamIP + "/onlyaudio/" + roomId + "3");
            studentAudio4 = new Uri("rtmp://" + streamIP + "/onlyaudio/" + roomId + "4");
            studentAudio5 = new Uri("rtmp://" + streamIP + "/onlyaudio/" + roomId + "5");
            user = userIn;
            currentColor = new byte[4];
            currentColor[0] = 0xFF;
            currentColor[1] = 0x00;
            currentColor[2] = 0x00;
            currentColor[3] = 0x00;

            linesList = new List<List<double[]>>();
            colorList = new List<byte[]>();

            isClosing = false;

            if (tag == 1)
            {
                hasControl = 0;
                userPosition = server.getUserPosition(roomIdIn, userIn.userId);
                StudentInitialization();
                connectToServer();
            }
            else {
                hasControl = 0;
                userPosition = 0;
                TeacherInitialization();
                connectToServer();
            }

            // 开始推流
            pushTool.StartCamera(roomId + userPosition,userPosition);

            teacherMedia.Open(teacherAddress);
            // 播放自己
            switch (userPosition)
            {
                case 1:
                    studentMedia1.Open(studentAudio1);break;
                case 2:
                    studentMedia2.Open(studentAudio2); break;
                case 3:
                    studentMedia3.Open(studentAudio3); break;
                case 4:
                    studentMedia4.Open(studentAudio4); break;
                case 5:
                    studentMedia5.Open(studentAudio5); break;
            }
            mute(userPosition);
        }

        /// <summary>
        /// 作为老师初始化窗口
        /// </summary>
        private void TeacherInitialization() {
            
            DeactivateComputerIcons(0);
            DeactivateRecordIcons();
            
            isStudent = false;
        }


        /// <summary>
        /// 作为学生初始化窗口
        /// </summary>
        private void StudentInitialization() {
            isStudent = true;

            hasStudent[userPosition] = true;

            recoverControlIcon.Visibility = Visibility.Collapsed;
            silenceAllIcon.Visibility = Visibility.Collapsed;

            DeactivateComputerIcons(0);
            DisableComputerIcon(userPosition, true);
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
        /// 教师端启用全部的控制权移交按钮
        /// </summary>
        private void ActivateComputerIcons() {
            for (int position = 1; position < 6; position++) {
                Image button = getComputerIcon(position);

                if (hasStudent[position] == false)
                    DisableComputerIcon(position, false);//关闭控制权按钮，初始状态为未获得控制权
                else
                    DisableComputerIcon(position, true);//激活控制权按钮，初始状态为未获得控制权
            }
        }
        /// <summary>
        /// 批量禁用移交控制权按钮。0时为禁用所有，用于教师初始化;
        /// 1~5为对应位置设置为激活，并禁用其他，用于将控制权转交到对应学生时的教师端与学生端初始化。
        /// </summary>
        /// <param name="position"></param>
        private void DeactivateComputerIcons(int position) {
            for (int deactivatePosition = 1; deactivatePosition < 6; deactivatePosition++) {
                if (deactivatePosition != position) {
                    DisableComputerIcon(position, false);//禁用控制权按钮，初始状态为未获得控制权                   
                }
                    
                else {
                    if (position > 0) {
                        if (hasStudent[position])
                            EnableComputerIcon(position, true);//启用控制权按钮，初始状态为已获得控制权
                    }
                }
                    
            }
        }
        /// <summary>
        /// 将按钮置为已经按过的状态。（即获得控制权）isActivated表示此时按钮是否可用
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isActivated"></param>
        private void EnableComputerIcon(int position, Boolean isActivated) {
            Image button = getComputerIcon(position);
            if (button == null)
                return;
            if (isActivated)
            {
                try
                {
                    button.Dispatcher.Invoke(() => {
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivedIcon"]);
                        button.Cursor = Cursors.Hand;
                        button.Tag = position + "" + 1;
                        button.Visibility = Visibility.Visible;
                    });
                }
                catch (Exception ex) { };
            }
            else
            {
                try
                {
                    button.Dispatcher.Invoke(() => {
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivatedWhenInactiveIcon"]);
                        button.Cursor = Cursors.Arrow;
                        button.Tag = position + "" + 1;
                        button.Visibility = Visibility.Visible;
                    });
                }
                catch (Exception ex) { };

            }
        }
        /// <summary>
        /// 将按钮置为未按过的状态。（即失去控制权）isActivated表示此时按钮是否可用
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isActivated"></param>
        private void DisableComputerIcon(int position, Boolean isActivated)
        {
            Image button = getComputerIcon(position);
            if (button == null)
                return;
            if (isActivated)
            {
                try
                {
                    button.Dispatcher.Invoke(() => {
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]);
                        button.Cursor = Cursors.Hand;
                        button.Tag = position + "" + 0;
                        button.Visibility = Visibility.Hidden;
                    });
                }
                catch (Exception ex) { };
            }
            else
            {
                try
                {
                    button.Dispatcher.Invoke(() => {
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerInactiveIcon"]);
                        button.Cursor = Cursors.Arrow;
                        button.Tag = position + "" + 0;
                        button.Visibility = Visibility.Hidden;
                    });
                }
                catch (Exception ex) { };

            }
        }
        /// <summary>
        /// 学生要求控制权时的按钮样式
        /// </summary>
        /// <param name="position"></param>
        private void askControlIcon(int position) {
            Image button = getComputerIcon(position);
            if (button == null)
                return;
            try
            {
                button.Dispatcher.Invoke(() => {
                    button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerAskControlIcon"]);
                    button.Tag = position + "" + 2;
                    button.Visibility = Visibility.Visible;
                });
            }
            catch (Exception ex) { };
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
                BanRecord(deactivatePosition, false);
            }
        }
        /// <summary>
        /// 将某一位置的按钮变为被静音状态。isActivated表示静音时该按钮是否可用
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isActivated"></param>
        private void BanRecord(int position,Boolean isActivated) {
            Image button = getRecordIcon(position);
            mute(position);
            Console.WriteLine("本地静音了第" + position + "位用户");
            if (button == null)
                return;
            if (isActivated)
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]);
                        button.Tag = position + "" + 1;
                        button.Visibility = Visibility.Visible;
                    }); } catch (Exception ex) { };
                
            }
            else
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedWhenInactiveIcon"]);
                        button.Tag = position + "" + 1;
                        button.Visibility = Visibility.Hidden;
                    }); } catch (Exception ex) { };
                
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
            unMute(position);
            Console.WriteLine("本地恢复了第" + position + "位用户");
            if (button == null)
                return;
            if (isActivated)
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]);
                        button.Tag = position + "" + 0;
                        button.Visibility = Visibility.Hidden;
                    }); } catch (Exception ex) { };
                
            }
            else
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordInactiveIcon"]);
                        button.Tag = position + "" + 0;
                        button.Visibility = Visibility.Hidden;
                    }); 
                } catch (Exception ex) { };
                
            }
        }

        /// <summary>
        /// 启用画布相关按钮
        /// </summary>
        /// <param name="button"></param>
        private void ActivateCanvasIcons() {
            //修改Canvas指针
            try
            {
                printCanvas.Dispatcher.Invoke(() => {
                    printCanvas.Cursor = Cursors.Cross;
                });
            }
            catch (Exception ex) { };
            if (isStudent == false) {//学生不可以清楚（虽然我不知道为什么会有这个要求）
                //修改清除画板按钮状态
                try
                {
                    deleteIcon.Dispatcher.Invoke(() => {
                        deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteIcon"]);
                        deleteIcon.Cursor = Cursors.Hand;
                    });
                }
                catch (Exception ex) { };
            }
            
            //修改颜色选择按钮状态
            try
            {
                colorChooser.Dispatcher.Invoke(() => {
                    colorChooser.SetValue(Button.StyleProperty, Application.Current.Resources["ColorChoser"]);
                    colorChooser.Fill = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
                    colorChooser.Cursor = Cursors.Hand;
                });
            }
            catch (Exception ex) { };
            
        }
        /// <summary>
        /// 禁用画布相关按钮
        /// </summary>
        /// <param name="button"></param>
        private void DeactivateCanvasIcons() {
            //修改Canvas指针
            try
            {
                printCanvas.Dispatcher.Invoke(() => {
                    printCanvas.Cursor = Cursors.Arrow;
                });
            }
            catch (Exception ex) { };
            try
            {
                //修改清除画板按钮状态
                deleteIcon.Dispatcher.Invoke(() => {
                    // 学生清空不可见
                    if (userPosition != 0)
                    {
                        deleteIcon.Visibility = Visibility.Hidden;
                    }
                    deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteInactiveIcon"]);
                    deleteIcon.Cursor = Cursors.Arrow;
                });
                //修改颜色选择按钮状态
                colorChooser.Dispatcher.Invoke(() => {
                    colorChooser.SetValue(Button.StyleProperty, Application.Current.Resources["ColorChoserDiabled"]);
                    colorChooser.Cursor = Cursors.Arrow;
                });
            }
            catch (Exception ex) { };
            
        }


        /// <summary>
        /// 初始化播放器并拉流
        ///<summary>
        private void teaMedia_Loaded(object sender, RoutedEventArgs e)
        {
            teacherMedia.MediaInitializing += OnMediaInitializing;
        }
        private void stu1Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia1.MediaInitializing += OnMediaInitializing;
        }
        private void stu2Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia2.MediaInitializing += OnMediaInitializing;
        }
        private void stu3Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia3.MediaInitializing += OnMediaInitializing;
        }
        private void stu4Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia4.MediaInitializing += OnMediaInitializing;
        }
        private void stu5Media_Loaded(object sender, RoutedEventArgs e)
        {
            studentMedia5.MediaInitializing += OnMediaInitializing;
        }

        /// <summary>
        /// 修改播放器缓冲
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMediaInitializing(object sender, MediaInitializingEventArgs e)
        {
            e.Configuration.GlobalOptions.FlagNoBuffer = true;
            //e.Configuration.PrivateOptions["flags"] = "low_delay";
            //e.Configuration.PrivateOptions["crf"] = "0";
            //e.Configuration.GlobalOptions.ProbeSize = 8192;
            //e.Configuration.GlobalOptions.MaxAnalyzeDuration = TimeSpan.FromMilliseconds(500);
        }

        /// <summary>
        /// 静音自己
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mute_MediaOpening(object sender, MediaOpeningEventArgs e)
        {
            switch (userPosition)
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

        /// <summary>
        /// 鼠标按下事件，此按钮用于移交控制权。点击确认变量数值：十位表征序号，个位为 0 表征是控制权按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComputerIcon_MouseDown(object sender, MouseButtonEventArgs e) {
            mouseClickedTag = 0;
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            if (isStudent == false)
            {
                //该学生不存在
                if (hasStudent[tagHead] == false)
                    return;
            }
            // 是学生并且不是自己的对应按钮
            else if (tagHead != userPosition)
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
                if (isStudent == true && userPosition == tagHead)
                {
                    //学生主动交还控制权的情况
                    if (hasControl == userPosition)
                    {
                        //禁用老师向其他学生交出控制权并禁用老师的画板
                        ///这里将一堆控制权按钮改掉
                        for (int position = 1; position < 6; position++)
                        {
                            if (position != userPosition)
                                DisableComputerIcon(position, false);
                            else
                                DisableComputerIcon(position, true);
                        }
                        DeactivateCanvasIcons();
                        hasControl = 0;
                        //此处添加学生交还控制权的方法
                        string order = "DisableControl@" +roomId+"@"+userPosition+"@"+ hasControl+"@";
                        sendOrder(order);
                    }
                    //教师获得控制权时学生可以举手
                    else if (hasControl == 0) {
                        //学生举手
                        if (tagTail == 0)
                        {
                            askControlIcon(userPosition);
                            //此处是学生举手
                            string order = "AskControl@" +roomId+"@"+ userPosition+"@";
                            sendOrder(order);
                        }
                        else if (tagTail == 2) {
                            DisableComputerIcon(userPosition,true);
                            //此处是学生取消举手
                            string order = "CancelAskControl@" + roomId + "@" + userPosition + "@";
                            sendOrder(order);
                        }
                        
                    }
                    
                }
                else if(isStudent == false){
                    if (hasStudent[tagHead] == false)
                        return;
                    //教师主动移交控制权
                    if (tagTail == 0 || tagTail == 2)
                    {
                        if (hasControl == 0)
                        {
                            hasControl = tagHead;
                            //禁用老师向其他学生交出控制权并禁用老师的画板
                            for (int position = 1; position < 6; position++) {
                                if (position != tagHead)
                                    DisableComputerIcon(position, false);
                                else
                                    EnableComputerIcon(position, true);
                            }
                            DeactivateCanvasIcons();
                            enableRecoverControl();//启用一键收回控制权按钮
                            //老师移交控制权的Socket函数
                            string order = "EnableControl@" + roomId + "@" + userPosition + "@" + tagHead + "@";
                            sendOrder(order);
                        }
                    }
                    //教师主动收回控制权
                    else
                    {                       
                        //恢复教师端的画板与移交控制权按钮
                        ActivateComputerIcons(); 
                        ActivateCanvasIcons();
                        disableRecoverControl();//禁用一键收回控制权按钮
                        //老师拿回控制权的Socket函数
                        string order = "DisableControl@" + roomId + "@" + userPosition + "@" + hasControl + "@";
                        sendOrder(order);
                        hasControl = 0;
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
                if (hasStudent[tagHead] == false)//该学生不存在
                    return;
            }
            else//是学生
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
                    //根据状态不同进行切换，BanRecord与EnableRecor自带Style切换与实际音量控制，故进行了合并
                    if (tagTail == 0)
                    {
                        BanRecord(tagHead,true);

                        //Socket网络通信
                        string order = "BanVoice@"+ roomId+"@"+userPosition+"@"+ tagHead+"@";
                        sendOrder(order);
                    }
                    else
                    {
                        EnableRecord(tagHead, true);

                        //Socket网络通信
                        string order = "EnableVoice@" + roomId + "@" + userPosition + "@" + tagHead + "@";
                        sendOrder(order);
                    }

                }
                else
                {
                    return;
                }                                  
                
                //重置状态值避免bug
                mouseClickedTag = 0;
            }
        }

        /// <summary>
        /// 静音第number号
        /// </summary>
        /// <param name="number"></param>
        private void mute(int number)
        {
            switch (number)
            {
                case 0:
                    try { teacherMedia.Dispatcher.Invoke(() => { if(teacherMedia.Volume!=0) teacherMedia.Volume = 0; }); } catch (Exception ex) { }; break;
                case 1:
                    try { studentMedia1.Dispatcher.Invoke(() => { if (studentMedia1.Volume != 0) studentMedia1.Volume = 0; }); } catch (Exception ex) { }; break;
                case 2:
                    try { studentMedia2.Dispatcher.Invoke(() => { if (studentMedia2.Volume != 0) studentMedia2.Volume = 0; }); } catch (Exception ex) { }; break;
                case 3:
                    try { studentMedia3.Dispatcher.Invoke(() => { if (studentMedia3.Volume != 0) studentMedia3.Volume = 0; }); } catch (Exception ex) { }; break;
                case 4:
                    try { studentMedia4.Dispatcher.Invoke(() => { if (studentMedia4.Volume != 0) studentMedia4.Volume = 0; }); } catch (Exception ex) { }; break;
                case 5:
                    try { studentMedia5.Dispatcher.Invoke(() => { if (studentMedia5.Volume != 0) studentMedia5.Volume = 0; }); } catch (Exception ex) { }; break;
            }
        }

        /// <summary>
        /// 老师恢复第number号。自己永远不能恢复自己
        /// </summary>
        /// <param name="number"></param>
        private void unMute(int number)
        {
            if (number == userPosition)
                return;
            switch (number)
            {
                case 0:
                    try { teacherMedia.Dispatcher.Invoke(() => { if (teacherMedia.Volume == 0) teacherMedia.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 1:
                    try { studentMedia1.Dispatcher.Invoke(() => { if (studentMedia1.Volume == 0) studentMedia1.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 2:
                    try { studentMedia2.Dispatcher.Invoke(() => { if (studentMedia2.Volume == 0) studentMedia2.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 3:
                    try { studentMedia3.Dispatcher.Invoke(() => { if (studentMedia3.Volume == 0) studentMedia3.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 4:
                    try { studentMedia4.Dispatcher.Invoke(() => { if (studentMedia4.Volume == 0) studentMedia4.Volume = 0.7; }); } catch (Exception ex) { }; break;
                case 5:
                    try { studentMedia5.Dispatcher.Invoke(() => { if (studentMedia5.Volume == 0) studentMedia5.Volume = 0.7; }); } catch (Exception ex) { }; break;
            }
        }

        /// <summary>
        /// 鼠标落下事件，此事件用于控制初始化画图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            if (hasControl != userPosition)
                return;
            Point startPoint = e.GetPosition(printCanvas);
            newLines(startPoint);

            //Socket网络通信
            string order = "Point@" + roomId + "@" + userPosition + "@0@" + startPoint.X + "@" + startPoint.Y + "@#";
            sendOrder(order);
            drawOrder = "";

            isDrawing = true;
        }
        

        /// <summary>
        /// 实际初始化画图
        /// </summary>
        /// <param name="startPoint"></param>
        private void newLines(Point startPoint) {
            pointsList = new List<double[]>();
            double[] startPointPosition = new double[2];
            startPointPosition[0] = startPoint.X;
            startPointPosition[1] = startPoint.Y;
            pointsList.Add(startPointPosition);

            linesList.Add(pointsList);
            colorList.Add(currentColor);                       
               
        }

       /// <summary>
       /// 鼠标移动事件，此事件用于绘图
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void PrintCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (hasControl != userPosition)
                return;
            if (e.LeftButton == MouseButtonState.Pressed && isDrawing == true) {

                Point point = e.GetPosition(printCanvas);
                int count = pointsList.Count(); // 总点数  
                

                // 去重复点
                if (count > 0) {
                    if (point.X - pointsList[count - 1][0] != 0 || point.Y - pointsList[count - 1][1] != 0) {
                        drawLines(point,count);

                        //Socket网络通信
                        if(drawOrder.Length < 1)
                            drawOrder = "Point@" +roomId+"@"+ userPosition +"@";
                        drawOrder = drawOrder + "1@" + point.X + "@" + point.Y + "@";
                        if (drawOrder.Length >= 1000) {
                            drawOrder = drawOrder + "#";
                            drawSend();
                        }                           
                    }
                }
            }
        }

        
        /// <summary>
        /// 实际绘制直线的方法。
        /// </summary>
        /// <param name="newPoint"></param>
        /// <param name="count"></param>
        private void drawLines(Point newPoint,int count) {
            App.Current.Dispatcher.Invoke((Action)(() =>
            {
                var l = new Line();
                l.Stroke = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
                l.StrokeThickness = 1;
                if (count < 1)
                    return;

                double difference = pointsList[count - 1][0] + pointsList[count - 1][1] - newPoint.X - newPoint.Y;
                if (difference > 60 || difference < -60)
                    return;

                // count-1  保证 line的起始点为点集合中的倒数第二个点。
                l.X1 = pointsList[count - 1][0];
                l.Y1 = pointsList[count - 1][1];
                // 终点X,Y 为当前point的X,Y
                l.X2 = newPoint.X;
                l.Y2 = newPoint.Y;
                printCanvas.Children.Add(l);
            }));
            
            //将点的坐标添加至List中
            double[] pointPosition = new double[2];
            pointPosition[0] = newPoint.X;
            pointPosition[1] = newPoint.Y;
            pointsList.Add(pointPosition);
        }
        /// <summary>
        /// 将缓存在drawOrder中的所有点数据发送到服务器
        /// </summary>
        /// <param name="painterPosition"></param>
        private void drawSend() {
            sendOrder(drawOrder);
            drawOrder = "";
        }

        /// <summary>
        /// 鼠标抬起事件，此事件用于终止画图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (hasControl != userPosition || isDrawing == false)
                return;
            isDrawing = false;
            endDrawing(userPosition);
        }
        /// <summary>
        /// 鼠标移动出Canvas事件，此事件用于终止画图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void printCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (hasControl != userPosition || isDrawing == false)
                return;
            isDrawing = false;
            endDrawing(userPosition);
        }

        /// <summary>
        /// 停止绘图调用的函数，将仍然留在缓存中的drawOrder命令全部发送
        /// </summary>
        /// <param name="painterPosition"></param>
        private void endDrawing(int painterPosition) {
            drawSend();
        }

        /// <summary>
        /// 鼠标按下事件，此按钮用于清除Canvas。点击确认变量数值： 1 表征是删除按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
           if (isStudent)
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
            if (isStudent)
                return;
            if (mouseClickedTag != 1) {
                mouseClickedTag = 0;
                return;
            }
            ClearCanvas(userPosition);
            string order = "ClearCanvas@" + roomId + "@" + userPosition + "@";
            sendOrder(order);
            mouseClickedTag = 0;
        }
        /// <summary>
        /// 实际清除Canvas的命令
        /// </summary>
        private void ClearCanvas(int painterPosition) {
            App.Current.Dispatcher.Invoke((Action)(() =>
            {
                printCanvas.Children.Clear();
            }));
            
            colorList = new List<byte[]>();
            linesList = new List<List<double[]>>();
        }

        /// <summary>
        /// 鼠标按下事件，此按钮用于打开颜色选择按钮。点击确认变量数值： 2 表征是颜色选择按钮。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorChooser_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (hasControl != userPosition)
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
            if (hasControl != userPosition)
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
                
                //Socket网络通信
                string order = "Color@" +roomId+"@"+ userPosition + "@" + currentColor[0] + "@" + currentColor[1] + "@" + currentColor[2] + "@" + currentColor[3]+"@";
                sendOrder(order);
            }
            mouseClickedTag = 0;


        }

        /// <summary>
        /// 鼠标落下事件，此按钮用于关闭此窗口。点击确认变量数值： 100 表征是关闭按钮。 
        /// </summary>
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


            RoomControlWindow roomControl = new RoomControlWindow(user,this.server);
            Window thisWindow = Window.GetWindow(this);
            thisWindow.Close();
            roomControl.Show();
        }

        /// <summary>
        /// 窗口关闭前会执行的函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LiveWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            // 关闭所有视频流
            teacherMedia.Close();
            studentMedia1.Close();
            studentMedia2.Close();
            studentMedia3.Close();
            studentMedia4.Close();
            studentMedia5.Close();

            //if (isClosing == false) {
                string order = "Quit@" + roomId + "@" + userPosition + "@";
                sendOrder(order);
            //}           

            if (serverThread != null)
                serverThread.Abort();
            if (socketServer != null)
                socketServer.Close();

            pushTool.Quit();

            server.setEmptyPosition(roomId, userPosition);
        }

        /// <summary>
        /// 鼠标落下事件，此按钮用于刷新。点击确认变量数值： 十位表征序号，3 表征是刷新按钮。 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshIcon_MouseDown(object sender, MouseButtonEventArgs e) {
            int tagHead = int.Parse((sender as Image).Tag.ToString()) / 10;
            mouseClickedTag = tagHead * 10 + 3;
        }
        /// <summary>
        /// 鼠标抬起事件，此按钮用于刷新。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshIcon_MouseUp(object sender, MouseButtonEventArgs e) {
            Image image = sender as Image;
            if (image != null)
            {
                //用tag表示不同按钮
                int tag = int.Parse(image.Tag.ToString());
                //tagTail表示状态
                int tagTail = tag % 10;
                //tagHead表示序号
                int tagHead = tag / 10;
                //检测鼠标落下与抬起是否相同。十位表征序号，个位为 3 表征是刷新按钮。
                Console.WriteLine(tag + ":" + tagHead + ":" + tagTail);
                if (mouseClickedTag != tagHead * 10 + 3)
                {
                    mouseClickedTag = 0;
                    return;
                }
                //在此添加刷新代码（根据tagHead获得位置）
                if (tagHead == 0)
                {
                    if (userPosition == 0)
                    {
                        teacherMedia.Open(teacherAddress);
                        try { teacherMedia.Dispatcher.Invoke(() => { if (teacherMedia.Volume != 0) teacherMedia.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        teacherMedia.Open(teacherAddress);
                    }
                }

                if (tagHead == 1)
                {
                    if (userPosition == 1)
                    {
                        studentMedia1.Open(studentAudio1);
                        try { studentMedia1.Dispatcher.Invoke(() => { if (studentMedia1.Volume != 0) studentMedia1.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查1号位置有没有学生接入
                        if (hasStudent[1])
                        {
                            // 老师播放音视频
                            if (userPosition == 0)
                            {
                                studentMedia1.Open(studentAddress1);
                            }
                            else //学生播放音频
                            {
                                studentMedia1.Open(studentAudio1);
                            }
                        }
                        else
                        {
                            MessageBox.Show("该位置没有学生接入");
                        }
                    }
                }

                if (tagHead == 2)
                {
                    if (userPosition == 2)
                    {
                        studentMedia2.Open(studentAudio2);
                        try { studentMedia2.Dispatcher.Invoke(() => { if (studentMedia2.Volume != 0) studentMedia2.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查2号位置有没有学生接入
                        if (hasStudent[2])
                        {
                            // 老师播放音视频
                            if (userPosition == 0)
                            {
                                studentMedia2.Open(studentAddress2);
                            }
                            else //学生播放音频
                            {
                                studentMedia2.Open(studentAudio2);
                            }
                        }
                        else
                        {
                            MessageBox.Show("该位置没有学生接入");
                        }
                    }
                }

                if (tagHead == 3)
                {
                    if (userPosition == 3)
                    {
                        studentMedia3.Open(studentAudio3);
                        try { studentMedia3.Dispatcher.Invoke(() => { if (studentMedia3.Volume != 0) studentMedia3.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查3号位置有没有学生接入
                        if (hasStudent[3])
                        {
                            // 老师播放音视频
                            if (userPosition == 0)
                            {
                                studentMedia3.Open(studentAddress3);
                            }
                            else //学生播放音频
                            {
                                studentMedia3.Open(studentAudio3);
                            }
                        }
                        else
                        {
                            MessageBox.Show("该位置没有学生接入");
                        }
                    }
                }

                if (tagHead == 4)
                {
                    if (userPosition == 4)
                    {
                        studentMedia4.Open(studentAudio4);
                        try { studentMedia4.Dispatcher.Invoke(() => { if (studentMedia4.Volume != 0) studentMedia4.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查4号位置有没有学生接入
                        if (hasStudent[4])
                        {
                            // 老师播放音视频
                            if (userPosition == 0)
                            {
                                studentMedia4.Open(studentAddress4);
                            }
                            else //学生播放音频
                            {
                                studentMedia4.Open(studentAudio4);
                            }
                        }
                        else
                        {
                            MessageBox.Show("该位置没有学生接入");
                        }
                    }
                }


                if (tagHead == 5)
                {
                    if (userPosition == 5)
                    {
                        studentMedia5.Open(studentAudio5);
                        try { studentMedia5.Dispatcher.Invoke(() => { if (studentMedia5.Volume != 0) studentMedia5.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查5号位置有没有学生接入
                        if (hasStudent[5])
                        {
                            // 老师播放音视频
                            if (userPosition == 0)
                            {
                                studentMedia5.Open(studentAddress5);
                            }
                            else //学生播放音频
                            {
                                studentMedia5.Open(studentAudio5);
                            }
                        }
                        else
                        {
                            MessageBox.Show("该位置没有学生接入");
                        }
                    }
                }

                //重置状态值避免bug
                mouseClickedTag = 0;
            }

            
        }
        
        /// <summary>
        /// 鼠标落下事件，此按钮用于一键静音或解除静音。点击确认变量数值： 99 表征是一键静音按钮。 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SilenceAll_MouseDown(object sender, MouseButtonEventArgs e) {
            mouseClickedTag = 99;
        }
        /// <summary>
        /// 鼠标抬起事件，此按钮用于一键静音或解除静音
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SilenceAll_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            if (image != null) {
                if (mouseClickedTag != 99 || isStudent)
                {
                    mouseClickedTag = 0;//重置状态值避免bug
                    return;
                }
                int tag = int.Parse(image.Tag.ToString());
                if (tag == 0)
                {
                    try
                    {
                        image.Dispatcher.Invoke(() => {
                            image.Tag = 1;
                            image.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]);
                        });
                    }
                    catch (Exception ex) { };
                    for (int position = 1; position < 6; position++)
                    {
                        if (hasStudent[position] == false)
                            BanRecord(position, false);
                        else
                            BanRecord(position, true);
                    }
                    sendOrder("BanVoice@"+roomId+"@0@0@");
                }
                else {
                    try
                    {
                        image.Dispatcher.Invoke(() => {
                            image.Tag = 0;
                            image.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]);
                        });
                    }
                    catch (Exception ex) { };
                    for (int position = 1; position < 6; position++)
                    {
                        if (hasStudent[position] == false)
                            EnableRecord(position, false);
                        else
                            EnableRecord(position, true);
                    }
                    sendOrder("EnableVoice@" + roomId + "@0@0@");
                }
            }
            //重置状态值避免bug
            mouseClickedTag = 0;
        }

        /// <summary>
        /// 鼠标落下事件，此按钮用于一键收回控制权。点击确认变量数值： 98 表征是一键收回控制权按钮。 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecoverControl_MouseDown(object sender, MouseButtonEventArgs e) {
            Image button = sender as Image;
            if (int.Parse(button.Tag.ToString()) == 0)
                return;
            mouseClickedTag = 98;
        }

        /// <summary>
        /// 鼠标抬起事件，此按钮用于一键收回控制权
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecoverControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            if (image != null)
            {
                if (mouseClickedTag != 98 || isStudent) {
                    mouseClickedTag = 0;//重置状态值避免bug
                    return;
                }
                    
                int tag = int.Parse(image.Tag.ToString());
                if (tag == 1 && hasControl != 0) {
                    sendOrder("DisableControl@"+roomId+"@"+userPosition+"@" + hasControl+"@");
                    ActivateCanvasIcons();
                    disableRecoverControl();

                    //这里将一堆控制权按钮改掉
                    for (int position = 1; position < 6; position++)
                    {
                        if (hasStudent[position])
                            DisableComputerIcon(position, true);
                        else
                            DisableComputerIcon(position, false);
                    }
                    hasControl = 0;
                }
                mouseClickedTag = 0;//重置状态值避免bug
            }
        }
        
        /// <summary>
        /// 收回控制权，同时调用方法将其他控制权按钮置为对应的普通状态
        /// </summary>
        private void disableRecoverControl() {
            try
            {
                recoverControlIcon.Dispatcher.Invoke(() => {
                    recoverControlIcon.Tag = 0;
                    recoverControlIcon.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivatedWhenInactiveIcon"]);
                    recoverControlIcon.Cursor = Cursors.Arrow;
                });
            }
            catch (Exception ex) { };
        }

        /// <summary>
        /// 控制权交给学生，启用一键收回控制权按钮
        /// </summary>
        private void enableRecoverControl()
        {
            try
            {
                recoverControlIcon.Dispatcher.Invoke(() => {
                    recoverControlIcon.Tag = 1;
                    recoverControlIcon.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]);
                    recoverControlIcon.Cursor = Cursors.Hand;
                });
            }
            catch (Exception ex) { };
        }

        /// <summary>
        /// 鼠标移入镜头区域的函数，用于使隐藏的三个按钮展示出来
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraMouseEnter(object sender, MouseEventArgs e)
        {
            int tag = int.Parse((sender as Grid).Tag.ToString());
            SetButtonsVisibility(tag, true);
        }

        /// <summary>
        /// 鼠标移处镜头区域的函数，用于隐藏三个按钮。
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
            if (hasStudent[tag] == false)
                return;
            if (setVisible)
            {
                switch (tag)
                {
                    case 1:
                        computerIcon_1.Visibility = Visibility.Visible;
                        recordIcon_1.Visibility = Visibility.Visible;
                        refreshIcon_1.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        computerIcon_2.Visibility = Visibility.Visible;
                        recordIcon_2.Visibility = Visibility.Visible;
                        refreshIcon_2.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        computerIcon_3.Visibility = Visibility.Visible;
                        recordIcon_3.Visibility = Visibility.Visible;
                        refreshIcon_3.Visibility = Visibility.Visible;
                        break;
                    case 4:
                        computerIcon_4.Visibility = Visibility.Visible;
                        recordIcon_4.Visibility = Visibility.Visible;
                        refreshIcon_4.Visibility = Visibility.Visible;
                        break;
                    case 5:
                        computerIcon_5.Visibility = Visibility.Visible;
                        recordIcon_5.Visibility = Visibility.Visible;
                        refreshIcon_5.Visibility = Visibility.Visible;
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
                        if (int.Parse(recordIcon_1.Tag.ToString()) % 10 == 0 || ((isStudent && tag != userPosition)))
                            recordIcon_1.Visibility = Visibility.Hidden;
                        refreshIcon_1.Visibility = Visibility.Hidden;
                        break;
                    case 2:
                        if (int.Parse(computerIcon_2.Tag.ToString()) % 10 == 0)
                            computerIcon_2.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_2.Tag.ToString()) % 10 == 0 || ((isStudent && tag != userPosition)))
                            recordIcon_2.Visibility = Visibility.Hidden;
                        refreshIcon_2.Visibility = Visibility.Hidden;
                        break;
                    case 3:
                        if (int.Parse(computerIcon_3.Tag.ToString()) % 10 == 0)
                            computerIcon_3.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_3.Tag.ToString()) % 10 == 0 || ((isStudent && tag != userPosition)))
                            recordIcon_3.Visibility = Visibility.Hidden;
                        refreshIcon_3.Visibility = Visibility.Hidden;
                        break;
                    case 4:
                        if (int.Parse(computerIcon_4.Tag.ToString()) % 10 == 0)
                            computerIcon_4.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_4.Tag.ToString()) % 10 == 0 || ((isStudent && tag != userPosition)))
                            recordIcon_4.Visibility = Visibility.Hidden;
                        refreshIcon_4.Visibility = Visibility.Hidden;
                        break;
                    case 5:
                        if (int.Parse(computerIcon_5.Tag.ToString()) % 10 == 0)
                            computerIcon_5.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_5.Tag.ToString()) % 10 == 0 || ((isStudent && tag != userPosition)))
                            recordIcon_5.Visibility = Visibility.Hidden;
                        refreshIcon_5.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        /// <summary>
        /// 初始化连接，获得服务器端自己对应的IP
        /// </summary>
        private void connectToServer() {
            this.socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketServer.Connect(serverIP, serverPort);

            string str = "FirstConnect@" + roomId + "@" + userPosition+"@";
            socketServer.Send(System.Text.Encoding.Default.GetBytes(str));
            //回信
            byte[] receiverByte = new byte[1024];
            int count = socketServer.Receive(receiverByte);
            string serverResponse = System.Text.Encoding.UTF8.GetString(receiverByte, 0, count);
            Console.WriteLine(serverResponse);

            if (isStudent)
                studentSocket(serverResponse);
            else
                teacherSocket(serverResponse);
        }

        /// <summary>
        /// 教师端开启Socket监听
        /// </summary>
        private void teacherSocket(string response) {
            if (response.Equals("Success"))
            {
                serverThread = new Thread(new ThreadStart(this.socketThread));
                serverThread.IsBackground = true;
                serverThread.Start();
            }
            else {
                //连接服务器出现问题，退出窗口
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    RoomControlWindow roomControl = new RoomControlWindow(user, this.server);
                    Window thisWindow = Window.GetWindow(this);
                    thisWindow.Close();
                    roomControl.Show();
                }));
            }
        }

        /// <summary>
        /// 学生端开启Socket回复监听
        /// </summary>
        private void studentSocket(string response)
        {
            if (response.Equals("Fail"))
            {
                //连接服务器出现问题，退出窗口
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    RoomControlWindow roomControl = new RoomControlWindow(user, this.server);
                    Window thisWindow = Window.GetWindow(this);
                    thisWindow.Close();
                    roomControl.Show();
                }));               
            }
            else
            {
                orderAnalyze(response);
                serverThread = new Thread(new ThreadStart(this.socketThread));
                serverThread.IsBackground = true;
                serverThread.Start();
            }
        }

        /// <summary>
        /// 客户端的监听线程
        /// </summary>
        private void socketThread()
        {
            while (isClosing == false)
            {
                byte[] readBuff = new byte[2048];
                int count = socketServer.Receive(readBuff);
                string orders = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
                string[] order = orders.Split('@');
                //对于更新画板进行特殊处理
                if (order[0].Equals("Point")) {
                    while (count == 2048) {
                        if (order[order.Length - 1].Equals("#"))
                            break;
                        count = socketServer.Receive(readBuff);
                        orders = orders + System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
                        order = orders.Split('@');
                    }
                    pointAnalyze(orders);
                    continue;
                }

                Boolean canExit = orderAnalyze(orders);
                if (canExit)
                    break;
            }
        }

        /// <summary>
        /// 专门对画板更新点进行分析的函数
        /// 格式"Point@'roomId'@'userPosition'@['具体模式'@'X'@'Y'@]#"。具体模式为1表示后续点，为0表示新的线的初始点。
        /// []内循环
        /// </summary>
        /// <param name="ordersIn"></param>
        private void pointAnalyze(string ordersIn) {
            string orders = ordersIn;
            string[] order = orders.Split('@');
            if (order.Length < 7)
                return;
            int painterPosition = int.Parse(order[2]);
            int position = 3;

            while (position < order.Length) {
                if (order[position].Equals("Point"))
                {
                    if (order.Length < position + 7)
                        return;
                    painterPosition = int.Parse(order[2]);
                    position = position + 3;
                }
                else if (order[position].Equals("#")) {
                    if (order.Length < position + 8)
                        return;
                    position++;
                }
                //绘制新点
                else
                {

                    if (order.Length < position + 4)
                        return;
                    try
                    {
                        int mode = int.Parse(order[position]);
                        double x = double.Parse(order[position + 1]);
                        double y = double.Parse(order[position + 2]);
                        if(x < 0.5 || y < 0.5)
                        {
                            position = position + 3;
                            continue;
                        }

                        if (mode == 0)
                        {
                            newLines(new Point(x, y));
                        }
                        else
                        {
                            drawLines(new Point(x, y), pointsList.Count());
                        }
                        position = position + 3;
                    }
                    catch (FormatException e) {
                        position++;
                        continue;
                    }   
                }
            }
        }


        /// <summary>
        /// Socket的命令分析器，用于分析传进来的命令
        /// </summary>
        /// <param name="socketOrder"></param>
        private Boolean orderAnalyze(string orders) {

            string[] order = orders.Split('@');
            if (order.Length < 1)
                return true;
            //静音命令 格式"BanVoice@'roomId'@'userPosition'@'banPosition'@"
            if (order[0].Equals("BanVoice"))
            {
                if (order.Length < 4)
                    return false;
                int banPosition = int.Parse(order[3]);
                if (banPosition == 0) {//教师一键静音所有学生
                    for (int position = 1; position < 6; position++) {
                            BanRecord(position, false);
                    }   
                }
                else//单个学生被静音
                {
                    BanRecord(banPosition, false);
                }
            }
            //取消静音命令 格式"EnableVoice@'roomId'@'userPosition'@'enablePosition'@"
            else if (order[0].Equals("EnableVoice"))
            {
                if (order.Length < 4)
                    return false;
                int enablePosition = int.Parse(order[3]);
                if (enablePosition == 0)
                {//教师一键解除所有学生静音
                    for (int position = 1; position < 6; position++)
                    {
                            EnableRecord(position, false);
                    }
                }
                else// 单个学生解除静音
                {
                    EnableRecord(enablePosition, false);
                }
            }
            //教师移交控制权命令 格式"EnableControl@'roomId'@'userPosition'@'enablePosition'@"
            else if (order[0].Equals("EnableControl"))
            {
                if (order.Length < 4)
                    return false;
                int enablePosition = int.Parse(order[3]);
                //教师将控制权交于本学生
                if (enablePosition == userPosition)
                {
                    ///将自己的按钮设置为已获得控制权并可按，将其他学生按钮设置为未获得控制权且不可按
                    for (int position = 1; position < 6; position++)
                    {
                        if (position != userPosition)
                            DisableComputerIcon(position, false);
                        else
                            EnableComputerIcon(position, true);
                    }
                    //启用画板
                    ActivateCanvasIcons();
                    hasControl = userPosition;
                }
                //教师将控制权交于其他学生
                else
                {
                    ///将所有未获得控制权的其他学生按钮设置为正确状态
                    for (int position = 1; position < 6; position++)
                    {
                        if (position != userPosition && position != enablePosition)
                            DisableComputerIcon(position, false);
                    }
                    //学生自动取消自己的举手并禁用举手功能
                    DisableComputerIcon(userPosition, false);
                    EnableComputerIcon(enablePosition, false);
                    hasControl = enablePosition;
                }
            }
            //教师拿回控制权命令 格式"DisableControl@'roomId'@'userPosition'@'disablePosition'@'"
            else if (order[0].Equals("DisableControl"))
            {
                if (order.Length < 4)
                    return false;
                int disablePosition = int.Parse(order[3]);
                //教师收到学生交还控制权命令
                if (isStudent == false)
                {
                    ActivateComputerIcons();
                    ActivateCanvasIcons();
                    hasControl = userPosition;
                    disableRecoverControl();
                }
                //学生收到教师收回自己控制权命令
                else if (disablePosition == userPosition)
                {
                    isDrawing = false;
                    if (isDrawing)
                        endDrawing(userPosition);                   
                    DisableComputerIcon(userPosition, true);//将按钮状态从可按已激活变成可按未激活
                    DeactivateCanvasIcons();
                    hasControl = 0;
                    
                }
                //学生收到教师收回他人控制权命令
                else
                {
                    DisableComputerIcon(disablePosition, false);//将按钮状态从不可按已激活变成不可按未激活
                    DisableComputerIcon(userPosition, true);//将按钮状态从不可按变成可按未激活
                    DeactivateCanvasIcons();
                    hasControl = 0;
                }
            }
            //学生举手命令 格式"AskControl@'roomId@''userPosition'@"
            else if (order[0].Equals("AskControl"))
            {
                if (order.Length < 3)
                    return false;
                int studentPosition = int.Parse(order[2]);
                askControlIcon(studentPosition);
            }
            //学生取消举手命令 格式"CancelAskControl@'roomId'@'userPosition'@"
            else if (order[0].Equals("CancelAskControl")) {
                if (order.Length < 3)
                    return false;
                int studentPosition = int.Parse(order[2]);
                DisableComputerIcon(studentPosition, true);
            }
            //修改颜色命令 格式"Color@'roomId'@'userPosition'@'A'@'R'@'G'@'B'@"
            else if (order[0].Equals("Color"))
            {
                if (order.Length < 7)
                    return false;
                currentColor[0] = byte.Parse(order[3]);
                currentColor[1] = byte.Parse(order[4]);
                currentColor[2] = byte.Parse(order[5]);
                currentColor[3] = byte.Parse(order[6]);
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    colorChooser.Fill = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
                }));

            }
            //画板清除命令 格式"ClearCanvas@'roomId'@'userPosition'@"
            else if (order[0].Equals("ClearCanvas"))
            {
                int painterPosition = int.Parse(order[2]);
                ClearCanvas(painterPosition);
                return false;

            }
            //退出命令 格式"Quit@'roomId'@'userPosition'@"
            else if (order[0].Equals("Quit"))
            {
                if (order.Length < 3)
                    return false;
                int position = int.Parse(order[2]);
                if (position == 0)
                {
                    isClosing = true;
                    
                    App.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        RoomControlWindow roomControl = new RoomControlWindow(user, this.server);
                        Window thisWindow = Window.GetWindow(this);
                        thisWindow.Close();
                        roomControl.Show();
                    }));

                    return true;

                }
                else
                {                   
                    hasStudent[position] = false;
                    DisableComputerIcon(position, false);//禁用目标位置控制权按钮并设置为未激活
                    BanRecord(position, false);//将目标位置静音设置为不可按已静音

                    if (isStudent == false && position == hasControl) {
                        hasControl = 0;
                        ActivateCanvasIcons();
                        for (int positionIP = 1; positionIP < 6; positionIP++)
                        {
                            if (hasStudent[positionIP])
                                DisableComputerIcon(positionIP, true);
                            else
                                DisableComputerIcon(positionIP, false);
                        }
                        disableRecoverControl();

                    }
                    
                    switch (position)
                    {
                        case 1:
                            studentMedia1.Close(); break;
                        case 2:
                            studentMedia2.Close(); break;
                        case 3:
                            studentMedia3.Close(); break;
                        case 4:
                            studentMedia4.Close(); break;
                        case 5:
                            studentMedia5.Close(); break;
                    }
                }

            }
            //有学生连接进来后老师广播 格式"StudentIn@'hasStudent_1'@'hasStudent_2'@'hasStudent_3'@'hasStudent_4'@'hasStudent_5'@"
            else if (order[0].Equals("StudentIn"))
            {
                if (order.Length < 6)
                    return false;
                for (int studentPosition = 1; studentPosition < 6; studentPosition++)
                {
                    if (studentPosition != userPosition)
                    {
                        if (int.Parse(order[studentPosition]) == 1)
                        {
                            if (hasStudent[studentPosition] == false) {
                                hasStudent[studentPosition] = true;
                                BanRecord(studentPosition, false);
                                DisableComputerIcon(studentPosition, false);
                                if (userPosition == 0)
                                {
                                    switch (studentPosition)
                                    {
                                        case 1:
                                            studentMedia1.Open(studentAddress1); break;
                                        case 2:
                                            studentMedia2.Open(studentAddress2); break;
                                        case 3:
                                            studentMedia3.Open(studentAddress3); break;
                                        case 4:
                                            studentMedia4.Open(studentAddress4); break;
                                        case 5:
                                            studentMedia5.Open(studentAddress5); break;
                                    }
                                }
                                else
                                {
                                    switch (studentPosition)
                                    {
                                        case 1:
                                            studentMedia1.Open(studentAudio1); break;
                                        case 2:
                                            studentMedia2.Open(studentAudio2); break;
                                        case 3:
                                            studentMedia3.Open(studentAudio3); break;
                                        case 4:
                                            studentMedia4.Open(studentAudio4); break;
                                        case 5:
                                            studentMedia5.Open(studentAudio5); break;
                                    }
                                }
                            }                            
                        }
                    }
                }

            }
            //特殊情况教师端需要学生端暂时停止绘图 格式"EndDraw@'roomId'@'userPosition'@"
            else if (order[0].Equals("EndDraw"))
            {
                if (order.Length < 3)
                    return false;
                int position = int.Parse(order[2]);
                if (position == userPosition && hasControl == userPosition) {
                    if (isDrawing)
                        endDrawing(userPosition);
                    isDrawing = false;
                }
                
            }

            return false;
        }


        /// <summary>
        /// 将命令发送给服务器
        /// </summary>
        /// <param name="order"></param>
        private void sendOrder(String order) {          
            socketServer.Send(System.Text.Encoding.Default.GetBytes(order));
        }

        
    }
}
