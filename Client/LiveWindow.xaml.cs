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
        //老师与学生IP
        string[] IPs;
        //客户端的服务器Socket
        Socket serverSocket;
        //客户端的服务器Socket线程
        Thread serverThread;
        //画图用的特殊Socket
        Socket[] drawSocket;
        Uri teacherAddress;
        Uri studentAddress1;
        Uri studentAddress2;
        Uri studentAddress3;
        Uri studentAddress4;
        Uri studentAddress5;


        /// <summary>
        /// 暂时利用Tag分辨老师与学生
        /// </summary>
        /// <param name="tag"></param>
        public LiveWindow(int tag,string roomIdIn,user userIn, Server.ServerService server)
        {

            var defaultEncoding = Encoding.Default;
            Console.WriteLine("开始时间:{0}", DateTime.Now.ToString());
            var stream = new FileStream("F:/log.txt", FileMode.Create);
            Console.SetOut(new StreamWriter(stream));
            Console.WriteLine("开始时间:{0}", DateTime.Now.ToString());
            Console.WriteLine("结束时间:{0}", DateTime.Now.ToString());

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

            //变量赋值与初始化
            IPs = new string[6] { null, null, null, null, null, null };
            roomId = roomIdIn;
            teacherAddress = new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "0");
            studentAddress1 = new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "1");
            studentAddress2 = new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "2");
            studentAddress3 = new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "3");
            studentAddress4 = new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "4");
            studentAddress5 = new Uri("rtmp://172.19.241.249:8082/live/" + roomId + "5");


            user = userIn;
            currentColor = new byte[4];
            currentColor[0] = 0xFF;
            currentColor[1] = 0x00;
            currentColor[2] = 0x00;
            currentColor[3] = 0x00;

            linesList = new List<List<double[]>>();
            colorList = new List<byte[]>();
            drawSocket = new Socket[6];

            if (tag == 1)
            {
                userPosition = server.getUserPosition(roomIdIn, userIn.userId);
                socketTest();
                StudentInitialization();
                studentSocket();
            }
            else {
                userPosition = 0;
                socketTest();
                TeacherInitialization();
                teacherSocket();
            }

            // 开始推流
            pushTool.StartCamera(roomId + userPosition);


            // 播放自己
            switch (userPosition)
            {
                case 0:
                    teacherMedia.Open(teacherAddress);break;
                case 1:
                    studentMedia1.Open(studentAddress1);break;
                case 2:
                    studentMedia2.Open(studentAddress2); break;
                case 3:
                    studentMedia3.Open(studentAddress3); break;
                case 4:
                    studentMedia4.Open(studentAddress4); break;
                case 5:
                    studentMedia5.Open(studentAddress5); break;
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
            canControl = true;
        }


        /// <summary>
        /// 作为学生初始化窗口
        /// </summary>
        private void StudentInitialization() {
            isStudent = true;
            canControl = false;

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
        /// 教师端启用全部的控制权移交按钮
        /// </summary>
        private void ActivateComputerIcons() {
            for (int position = 1; position < 6; position++) {
                Image button = getComputerIcon(position);
                
                if (IPs[position] != null)
                    ActivateComputerIcon(position, false);
            }
        }
        /// <summary>
        /// 批量禁用移交控制权按钮。0时为静音所有，用于初始化与学生失去控制权;
        /// 1~5为对应位置设置为激活，并禁用其他，用于将控制权转交到对应学生时的教师端与获得控制权的学生端。
        /// </summary>
        /// <param name="position"></param>
        private void DeactivateComputerIcons(int position) {
            for (int deactivatePosition = 1; deactivatePosition < 6; deactivatePosition++) {
                if (deactivatePosition != position) { 
                    DeactivateComputerIcon(deactivatePosition,false);
                    Image button = getComputerIcon(deactivatePosition);
                    
                }
                    
                else {
                    if (position > 0) {
                        if (IPs[position] != null)
                            ActivateComputerIcon(deactivatePosition, true);
                    }
                }
                    
            }
        }
        /// <summary>
        /// 启用移交控制权按钮，isEnabled表示是否已获得控制权
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isActivated"></param>
        private void ActivateComputerIcon(int position,Boolean isEnabled) {
            Image button = getComputerIcon(position);
            if (button == null)
                return;
            if (isEnabled)
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivedIcon"]); 
                        button.Cursor = Cursors.Hand; 
                    }); 
                } 
                catch (Exception ex) { };
                
            }
            else {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]); 
                        button.Cursor = Cursors.Hand; 
                    }); 
                } 
                catch (Exception ex) { };
                
            }
            
            
        }
        /// <summary>
        /// 禁用移交控制权按钮，isEnabled表示是否已获得控制权
        /// </summary>
        /// <param name="button"></param>
        private void DeactivateComputerIcon(int position,Boolean isEnabled) {
            Image button = getComputerIcon(position);
            if (button == null)
                return;
            if (isEnabled)
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivatedWhenInactiveIcon"]); 
                        button.Cursor = Cursors.Arrow; 
                    }); 
                } 
                catch (Exception ex) { };

            }
            else
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerInactiveIcon"]); 
                        button.Cursor = Cursors.Arrow;
                    }); 
                } 
                catch (Exception ex) { };

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
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]); button.Cursor = Cursors.Hand; }); } catch (Exception ex) { };
                
            }
            else
            {
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]); button.Cursor = Cursors.Hand; }); } catch (Exception ex) { };
                
            }

            
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
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordInactiveIcon"]); button.Cursor = Cursors.Arrow; }); } catch (Exception ex) { };
                
            }
            else
            {
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedWhenInactiveIcon"]); button.Cursor = Cursors.Arrow; }); } catch (Exception ex) { };
                
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
            if (button == null)
                return;
            if (isActivated)
            {
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]); }); } catch (Exception ex) { };
                
            }
            else
            {
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedWhenInactiveIcon"]); }); } catch (Exception ex) { };
                
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
            if (button == null)
                return;
            if (isActivated)
            {
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]); }); } catch (Exception ex) { };
                
            }
            else
            {
                try { button.Dispatcher.Invoke(() => { button.SetValue(Button.StyleProperty, Application.Current.Resources["RecordInactiveIcon"]); }); } catch (Exception ex) { };
                
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
            //修改清除画板按钮状态
            try
            {
                deleteIcon.Dispatcher.Invoke(() => {
                    deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteIcon"]);
                    deleteIcon.Cursor = Cursors.Hand;
                });
            }
            catch (Exception ex) { };
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
                if (IPs[tagHead] == null || IPs[tagHead].Length <1)//该学生不存在
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
                    string order = "DisableControl@" + tagHead;
                    sendOrder(order);
                }
                else {
                    //老师手动拿回控制权的时候
                    //根据状态不同进行切换
                    if (tagTail == 0)
                    {
                        if (canControl == true)
                        {
                            canControl = false;
                            DeactivateComputerIcons(tagHead);//暂时禁用老师向其他学生交出控制权并禁用老师的画板
                            DeactivateCanvasIcons();
                            //老师移交控制权的Socket函数
                            string order = "EnableControl@" + tagHead;
                            broadcastOrder(order, 0);
                            image.Dispatcher.Invoke(() => {
                                image.Tag = tagHead + "" + 1;
                            });
                        }
                    }
                    else
                    {
                        canControl = true;
                        ActivateComputerIcons();
                        ActivateCanvasIcons();
                        //老师拿回控制权的Socket函数
                        string order = "DisableControl@" + tagHead;
                        broadcastOrder(order, 0);
                        image.Dispatcher.Invoke(() => {
                            image.Tag = tagHead + "" + 0;
                        });
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
                if (IPs[tagHead] == null)//该学生不存在
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
                if (isStudent == false || tagHead == userPosition)
                {
                    //根据状态不同进行切换，BanRecord与EnableRecor自带Style切换与实际音量控制，故进行了合并
                    if (tagTail == 0)
                    {
                        try
                        {
                            image.Dispatcher.Invoke(() => {
                                image.Tag = tagHead + "" + 1;
                            });
                        }
                        catch (Exception ex) { };
                        BanRecord(tagHead,true);

                        //Socket网络通信
                        string order = "BanVoice@" + tagHead;
                        socketOrder(order, 0);
                    }
                    else
                    {
                        try
                        {
                            image.Dispatcher.Invoke(() => {
                                image.Tag = tagHead + "" + 0;
                            });
                        }
                        catch (Exception ex) { };
                        EnableRecord(tagHead, true);

                        //Socket网络通信
                        string order = "EnableVoice@" + tagHead;
                        socketOrder(order, 0);
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

        // 静音第number号
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

        // 恢复第number号。永远不会恢复自己
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
            if (canControl == false)
                return;
            Point startPoint = e.GetPosition(printCanvas);
            newLines(startPoint,userPosition);
            isDrawing = true;
        }
        
        //绘图时用于保存所有命令的string
        string drawOrder;
        //已有命令数统计
        int drawOrderCount;

        /// <summary>
        /// 实际初始化画图
        /// </summary>
        /// <param name="startPoint"></param>
        private void newLines(Point startPoint,int painterPosition) {
            pointsList = new List<double[]>();
            double[] startPointPosition = new double[2];
            startPointPosition[0] = startPoint.X;
            startPointPosition[1] = startPoint.Y;
            pointsList.Add(startPointPosition);

            linesList.Add(pointsList);
            colorList.Add(currentColor);
            //Socket网络通信
            string order = "Point@" + painterPosition + "@0@" + startPoint.X + "@" + startPoint.Y+"@";
            //教师绘图或者教师接收到其他学生绘图，广播到学生
            if (isStudent == false)
            {
                for (int position = 1; position < 6; position++)
                {
                    if (position != painterPosition && IPs[position] != null)
                    {
                        drawSocket[position] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        drawSocket[position].Connect(IPs[position], 8085);
                        drawSocket[position].Send(System.Text.Encoding.Default.GetBytes(order));
                        drawSocket[position].Close();
                    }
                }
            }
            //学生自己绘图，通知教师。如果不是本人绘图，表明是教师的广播命令，不能重新通知教师
            else if (userPosition == painterPosition) {
                drawSocket[0] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                drawSocket[0].Connect(IPs[0], 8085);
                drawSocket[0].Send(System.Text.Encoding.Default.GetBytes(order));
                drawSocket[0].Close();
            }
            drawOrder = "";
            drawOrderCount = 0;
               
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
                

                // 去重复点
                if (count > 0) {
                    if (point.X - pointsList[count - 1][0] != 0 || point.Y - pointsList[count - 1][1] != 0) {
                        drawLines(point,count,userPosition); 
                    }
                }
            }
        }

        
        /// <summary>
        /// 实际绘制直线的方法。包含了网络的通信的部分
        /// </summary>
        /// <param name="newPoint"></param>
        /// <param name="count"></param>
        private void drawLines(Point newPoint,int count,int painterPosition) {
            App.Current.Dispatcher.Invoke((Action)(() =>
            {
                var l = new Line();
                l.Stroke = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
                l.StrokeThickness = 1;
                if (count < 1)
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

            //Socket网络通信
            drawOrder = drawOrder + "Point@" + painterPosition + "@1@" + newPoint.X + "@" + newPoint.Y+"@";
            drawOrderCount++;            
            if (drawOrderCount >= 10 && drawOrder.Length >= 450)
                drawSend(painterPosition);

        }

        private void drawSend(int painterPosition) {           
            //教师绘图或者教师接收到其他学生绘图，广播到学生
            if (isStudent == false)
            {
                for (int position = 1; position < 6; position++)
                {
                    if (position != painterPosition && drawSocket[position] != null)
                    {
                        drawSocket[position] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        drawSocket[position].Connect(IPs[position], 8085);
                        drawSocket[position].Send(System.Text.Encoding.Default.GetBytes(drawOrder));
                        drawSocket[position].Close();
                    }
                }
            }
            //学生自己绘图，通知教师。如果不是本人绘图，表明是教师的广播命令，不能重新通知教师
            else if (userPosition == painterPosition)
            {
                drawSocket[0] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                drawSocket[0].Connect(IPs[0], 8085);
                drawSocket[0].Send(System.Text.Encoding.Default.GetBytes(drawOrder));
                drawSocket[0].Close();
            }

            drawOrderCount = 0;
            drawOrder = "";
        }

        /// <summary>
        /// 鼠标抬起事件，此事件用于终止画图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (canControl == false || isDrawing == false)
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
            if (canControl == false || isDrawing == false)
                return;
            isDrawing = false;
            endDrawing(userPosition);
        }

        /// <summary>
        /// 停止绘图调用的函数，主要是网络通信
        /// </summary>
        /// <param name="painterPosition"></param>
        private void endDrawing(int painterPosition) {
            drawSend(painterPosition);

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
            ClearCanvas(userPosition);
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

            ///Socket网络通信
            string order = "Point@" + userPosition + "@" + -1+"@";
            //教师绘图或者教师接收到其他学生绘图，广播到学生
            if (isStudent == false)
                broadcastOrder(order, painterPosition); 
            //学生自己绘图，通知教师。如果不是本人绘图，表明是教师的广播命令，不能重新通知教师
            else if(userPosition == painterPosition)
                sendOrder(order);
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
                
                //Socket网络通信
                string order = "Color@" + userPosition + "@" + currentColor[0] + "@" + currentColor[1] + "@" + currentColor[2] + "@" + currentColor[3];
                socketOrder(order, 0);
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

            string order = "Quit@" + userPosition;
            socketOrder(order, 0);

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
            if (serverThread != null)
                serverThread.Abort();
            if (serverSocket != null)
                serverSocket.Close();
            for (int position = 0; position < 6; position++) {
                if (drawSocket[position] != null)
                    drawSocket[position].Close();
            }
            if (isStudent == false)
                teacherQuit();
            pushTool.Quit();
            Console.Out.Close();
            server.setEmptyPosition(roomId, userPosition);
        }

        /// <summary>
        /// 通知服务器端的Socket本房间已经解散
        /// </summary>
        private void teacherQuit() {
            Socket connectToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectToServer.Connect("172.19.241.249", 8086);
            string str = "CloseRoom@" + roomId;
            connectToServer.Send(System.Text.Encoding.Default.GetBytes(str));
            connectToServer.Close();
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
                        studentMedia1.Open(studentAddress1);
                        try { studentMedia1.Dispatcher.Invoke(() => { if (studentMedia1.Volume != 0) studentMedia1.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查1号位置有没有学生接入
                        if (IPs[1] != null)
                        {
                            studentMedia1.Open(studentAddress1);
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
                        studentMedia2.Open(studentAddress2);
                        try { studentMedia2.Dispatcher.Invoke(() => { if (studentMedia2.Volume != 0) studentMedia2.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查2号位置有没有学生接入
                        if (IPs[2] != null)
                        {
                            studentMedia2.Open(studentAddress2);
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
                        studentMedia3.Open(studentAddress3);
                        try { studentMedia3.Dispatcher.Invoke(() => { if (studentMedia3.Volume != 0) studentMedia3.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查3号位置有没有学生接入
                        if (IPs[3] != null)
                        {
                            studentMedia3.Open(studentAddress3);
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
                        studentMedia4.Open(studentAddress4);
                        try { studentMedia4.Dispatcher.Invoke(() => { if (studentMedia4.Volume != 0) studentMedia4.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查4号位置有没有学生接入
                        if (IPs[4] != null)
                        {
                            studentMedia4.Open(studentAddress4);
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
                        studentMedia5.Open(studentAddress5);
                        try { studentMedia5.Dispatcher.Invoke(() => { if (studentMedia5.Volume != 0) studentMedia5.Volume = 0; }); } catch (Exception ex) { };
                    }
                    else
                    {
                        // 检查5号位置有没有学生接入
                        if (IPs[5] != null)
                        {
                            studentMedia5.Open(studentAddress5);
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
                        if (int.Parse(recordIcon_1.Tag.ToString()) % 10 == 0)
                            recordIcon_1.Visibility = Visibility.Hidden;
                        refreshIcon_1.Visibility = Visibility.Hidden;
                        break;
                    case 2:
                        if (int.Parse(computerIcon_2.Tag.ToString()) % 10 == 0)
                            computerIcon_2.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_2.Tag.ToString()) % 10 == 0)
                            recordIcon_2.Visibility = Visibility.Hidden;
                        refreshIcon_2.Visibility = Visibility.Hidden;
                        break;
                    case 3:
                        if (int.Parse(computerIcon_3.Tag.ToString()) % 10 == 0)
                            computerIcon_3.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_3.Tag.ToString()) % 10 == 0)
                            recordIcon_3.Visibility = Visibility.Hidden;
                        refreshIcon_3.Visibility = Visibility.Hidden;
                        break;
                    case 4:
                        if (int.Parse(computerIcon_4.Tag.ToString()) % 10 == 0)
                            computerIcon_4.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_4.Tag.ToString()) % 10 == 0)
                            recordIcon_4.Visibility = Visibility.Hidden;
                        refreshIcon_4.Visibility = Visibility.Hidden;
                        break;
                    case 5:
                        if (int.Parse(computerIcon_5.Tag.ToString()) % 10 == 0)
                            computerIcon_5.Visibility = Visibility.Hidden;
                        if (int.Parse(recordIcon_5.Tag.ToString()) % 10 == 0)
                            recordIcon_5.Visibility = Visibility.Hidden;
                        refreshIcon_5.Visibility = Visibility.Hidden;
                        break;
                }
            }
        }

        /// <summary>
        /// 初始化连接，获得服务器端自己对应的IP
        /// </summary>
        private void socketTest() {
            Socket connectToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectToServer.Connect("172.19.241.249", 8085);
            
            string ip = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(a => a.AddressFamily.ToString().Equals("InterNetwork")).ToString();
            string str = "firstConnect@"+userPosition+"@"+roomId+"@"+ip;
            connectToServer.Send(System.Text.Encoding.Default.GetBytes(str));
            //回信
            byte[] ipByte = new byte[1024];
            int count = connectToServer.Receive(ipByte);
            string serverResponse = System.Text.Encoding.UTF8.GetString(ipByte, 0, count);
            connectToServer.Close();
            
            IPs[userPosition] = ip;
            //测试socket连接
            Console.WriteLine("Connect to server is " + serverResponse);
        }

        /// <summary>
        /// 教师端开启Socket监听
        /// </summary>
        private void teacherSocket() {
            serverThread = new Thread(new ThreadStart(this.teacherSocketThread));
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        /// <summary>
        /// 教师端的监听线程
        /// </summary>
        private void teacherSocketThread()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine(IPs[userPosition]);
            IPAddress ipAdr = IPAddress.Parse(IPs[userPosition]);
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8085);
            serverSocket.Bind(ipEp);
            serverSocket.Listen(0);
            Console.WriteLine("教师端[服务器]启动成功");
            while (true)
            {
                Socket studentOrder = serverSocket.Accept();
                Console.WriteLine("教师端[服务器]已接收学生端[命令]");
                orderAnalyze(studentOrder);
            }
        }

        /// <summary>
        /// 学生端获取教师端IP地址，并开启自身的Socket监听
        /// </summary>
        private void studentSocket() {
            //获取教师IP
            Socket connectToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectToServer.Connect("172.19.241.249", 8085);
            string str = "getTeacher@"+roomId;
            connectToServer.Send(System.Text.Encoding.Default.GetBytes(str));
            //回信
            byte[] ipByte = new byte[1024];
            int count = connectToServer.Receive(ipByte);
            string teacherIP = System.Text.Encoding.UTF8.GetString(ipByte, 0, count);
            connectToServer.Close();
            if (teacherIP.Length < 1) {
                Console.WriteLine("Something went wrong.");
                Window thisWindow = Window.GetWindow(this);
                thisWindow.Close();
            }
            IPs[0] = teacherIP;
            //与教师连接
            Console.WriteLine(IPs[0]);
            Socket connectToTeacher = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectToTeacher.Connect(IPs[0], 8085);
            str = "ConnectToTeacher@" + userPosition+"@"+IPs[userPosition];
            connectToTeacher.Send(System.Text.Encoding.Default.GetBytes(str));
            connectToTeacher.Close();
            teacherMedia.Open(teacherAddress);

            serverThread = new Thread(new ThreadStart(this.studentSocketThread));
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        /// <summary>
        /// 学生端的Socket监听线程
        /// </summary>
        private void studentSocketThread() {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdr = IPAddress.Parse(IPs[userPosition]);
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8085);
            serverSocket.Bind(ipEp);
            serverSocket.Listen(0);
            Console.WriteLine("学生端[服务器]启动成功");
            while (true) {
                Socket teacherOrder = serverSocket.Accept();
                Console.WriteLine("学生端[服务器]已接收教师端[命令]");
                orderAnalyze(teacherOrder);
            }
        }

        /// <summary>
        /// Socket的命令分析器，用于分析传进来的命令
        /// </summary>
        /// <param name="socketOrder"></param>
        private void orderAnalyze(Socket socketOrder) {
            byte[] readBuff = new byte[2048];
            int count = socketOrder.Receive(readBuff);
            string orders = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);

            string[] order = orders.Split('@');
            //静音命令 格式"BanVoice@'userPosition'"
            if (order[0].Equals("BanVoice"))
            {
                if (order.Length < 2)
                    return;
                int banPosition = int.Parse(order[1]);
                if (isStudent == false || banPosition == userPosition)//教师静音本学生或学生禁音自己并通知教师
                {
                    BanRecord(userPosition, true);
                }
                else//其他学生静音
                {
                    BanRecord(banPosition, false);
                }
            }
            //取消静音命令 格式"EnableVoice@'userPosition'"
            else if (order[0].Equals("EnableVoice"))
            {
                if (order.Length < 2)
                    return;
                int enablePosition = int.Parse(order[1]);
                if (isStudent == false || enablePosition == userPosition)//教师解除静音本学生或学生解除静音自己并通知教师
                {
                    EnableRecord(userPosition, true);
                }
                else// 其他学生解除静音
                {
                    EnableRecord(enablePosition, false);
                }
            }
            //教师移交控制权命令 格式"EnableControl@'userPosition'"
            else if (order[0].Equals("EnableControl"))
            {
                if (order.Length < 2)
                    return;
                int enablePosition = int.Parse(order[1]);
                if (enablePosition == userPosition)//教师将控制权交于本学生
                {
                    DeactivateComputerIcons(userPosition);
                    ActivateCanvasIcons();
                    canControl = true;
                }
                else//教师将控制权交于其他学生
                {
                    DeactivateComputerIcon(enablePosition, true);
                }
            }
            //教师拿回控制权命令 格式"DisableControl@'userPosition'"
            else if (order[0].Equals("DisableControl"))
            {
                if (order.Length < 2)
                    return;
                int disablePosition = int.Parse(order[1]);
                if (isStudent == false)
                {//教师收到学生交还控制权命令
                    ActivateComputerIcons();
                    ActivateCanvasIcons();
                    canControl = true;

                }
                else if (disablePosition == userPosition)//学生收到教师收回自己控制权命令
                {
                    DeactivateComputerIcons(0);
                    DeactivateCanvasIcons();
                    canControl = false;
                    if (isDrawing)
                        endDrawing(userPosition);
                    isDrawing = false;
                }
                else//学生收到教师收回他人控制权命令
                {
                    DeactivateComputerIcon(disablePosition, false);
                }
            }
            //修改颜色命令 格式"Color@'userPosition'@'A'@'R'@'G'@'B'"
            else if (order[0].Equals("Color"))
            {
                if (order.Length < 6)
                    return;
                currentColor[0] = byte.Parse(order[2]);
                currentColor[1] = byte.Parse(order[3]);
                currentColor[2] = byte.Parse(order[4]);
                currentColor[3] = byte.Parse(order[5]);
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    colorChooser.Fill = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
                }));

            }
            //画板更新命令 格式"Point@'userPosition'@'具体操作'@'X'@'Y@",因为是一个Socket多次发送，所以需要用循环便利所有缓冲区。这条命令自带广播功能。
            else if (order[0].Equals("Point"))
            {
                int painterPosition = int.Parse(order[1]);
                int basicOrder = int.Parse(order[2]);

                Console.WriteLine(orders);

                for (int position = 0; position < order.Length; position = position + 5)
                {
                    if (order[position].Equals("Point") == false)
                        break;
                    switch (int.Parse(order[position + 2]))
                    {
                        case -1://具体操作值为-1 表示清除画板
                            ClearCanvas(painterPosition);
                            break;
                        case 0://具体操作值为0 表示新建一条线
                            newLines(new Point(double.Parse(order[position + 3]), double.Parse(order[position + 4])), painterPosition);
                            break;
                        case 1://具体操作值为1 表示添加新的点
                            drawLines(new Point(double.Parse(order[position + 3]), double.Parse(order[position + 4])), pointsList.Count, painterPosition);
                            break;
                    }
                }
                if (basicOrder != -1)
                    endDrawing(painterPosition);
                return;

            }
            //退出命令 格式"Quit@'userPosition'"
            else if (order[0].Equals("Quit"))
            {
                if (order.Length < 2)
                    return;
                if (int.Parse(order[1]) == 0)
                {
                    socketOrder.Close();

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
                    int position = int.Parse(order[1]);
                    IPs[position] = null;
                    DeactivateComputerIcon(position, false);
                    DeactivateRecordIcon(position, false);
                    socketOrder.Close();

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
            //学生连接教师命令 格式"ConnectToTeacher@'hasStudent_1'@'hasStudent_2'@'hasStudent_3'@'hasStudent_4'@'hasStudent_5'"
            //hasStudent=0表示没有，反之则有
            else if (order[0].Equals("ConnectToTeacher"))
            {
                if (order.Length < 3)
                    return;
                string studentIP = order[2];
                int studentPosition = int.Parse(order[1]);
                IPs[studentPosition] = studentIP;
                socketOrder.Close();

                string newOrder = "StudentIn";
                for (int position = 1; position < 6; position++)
                {
                    if (IPs[position] == null)
                        newOrder = newOrder + "@0";
                    else
                        newOrder = newOrder + "@1";
                }

                broadcastOrder(newOrder, 0);

                ActivateComputerIcon(studentPosition, false);
                ActivateRecordIcon(studentPosition, true);

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

                return;
            }
            //有学生连接进来后老师广播 格式"StudentIn@'newStudentPostion'"
            else if (order[0].Equals("StudentIn"))
            {
                if (order.Length < 6)
                    return;
                for (int studentPosition = 1; studentPosition < 6; studentPosition++)
                {
                    if (studentPosition != userPosition)
                    {
                        if (int.Parse(order[studentPosition]) == 1)
                        {
                            IPs[studentPosition] = "hasStudent";
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
                    }
                }

            }
            //特殊情况教师端需要学生端暂时停止绘图 格式"EndDraw@"
            else if (order[0].Equals("EndDraw")) {
                if (isDrawing)
                    endDrawing(userPosition);
                isDrawing = false;
            }

            socketOrder.Close();

            //如果是教师端，则还需要负责将命令广播出去。
            if (isStudent == false) {
                broadcastOrder(orders, int.Parse(order[1]));
            }
        }




        /// <summary>
        /// 发送Socket命令
        /// </summary>
        /// <param name="order"></param>
        /// <param name="notToBroadCast"></param>
        private void socketOrder(String order, int notToBroadCast) {
            if (isStudent)
                sendOrder(order);
            else
                broadcastOrder(order, notToBroadCast);
        }
        /// <summary>
        /// 将命令发送给教师的Socket
        /// </summary>
        /// <param name="order"></param>
        private void sendOrder(String order) {
            Socket sendToTeacher = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sendToTeacher.Connect(IPs[0], 8085);
            sendToTeacher.Send(System.Text.Encoding.Default.GetBytes(order));
            sendToTeacher.Close();
        }

        /// <summary>
        /// 将命令广播到每个学生的Socket
        /// </summary>
        /// <param name="order"></param>
        /// <param name="notToBroeadCast"></param>
        private void broadcastOrder(string order, int notToBroadCast) {
            for (int position = 1; position < 6; position++) {
                if (IPs[position] != null) {
                    if (position != notToBroadCast) {
                        Socket broadcastToStudent = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        broadcastToStudent.Connect(IPs[position],8085);
                        broadcastToStudent.Send(System.Text.Encoding.Default.GetBytes(order));
                        broadcastToStudent.Close();
                    }
                
                }
            }
        }
        
    }
}
