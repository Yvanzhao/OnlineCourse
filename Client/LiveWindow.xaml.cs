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
using System.Net.Sockets;
using System.Net;

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

            IPs = new string[6] { null, null, null, null, null, null };
            roomId = roomIdIn;
            user = userIn;
            this.userPosition = server.getUserPosition(roomIdIn,userIn.userId);

            currentColor = new byte[4];
            currentColor[0] = 0xFF;
            currentColor[1] = 0x00;
            currentColor[2] = 0x00;
            currentColor[3] = 0x00;
            

            if (tag == 1)
            {
                StudentInitialization();
                studentSocket();
            }
            else {
                TeacherInitialization();
                teacherSocket();
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
            canControl = true;
            checkStudent();
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
                Image button = getComputerIcon(position);
                try
                {
                    button.Dispatcher.Invoke(() => {
                        button.Tag = (int)((userPosition * 10) + 0);
                        button.Visibility = Visibility.Hidden;
                    });
                }
                catch (Exception ex) { };
                
                if (hasStudent[position - 1])
                    ActivateComputerIcon(position, false);
            }
        }
        /// <summary>
        /// 批量禁用移交控制权按钮。0时为静音所有，用于学生初始化与学生失去控制权;
        /// 1~5为对应位置设置为激活，并禁用其他，用于将控制权转交到对应学生时的教师端与获得控制权的学生端。
        /// </summary>
        /// <param name="position"></param>
        private void DeactivateComputerIcons(int position) {
            for (int deactivatePosition = 1; deactivatePosition < 6; deactivatePosition++) {
                if (deactivatePosition != position) { 
                    DeactivateComputerIcon(deactivatePosition,false);
                    Image button = getComputerIcon(deactivatePosition);
                    try
                    {
                        button.Dispatcher.Invoke(() => {
                            button.Tag = (int)((userPosition * 10) + 0);
                            button.Visibility = Visibility.Hidden;
                        });
                    }
                    catch (Exception ex) { };
                    
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
        /// 禁用移交控制权按钮isEnabled，isEnabled表示是否已获得控制权
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
                        button.Cursor = Cursors.Hand; 
                    }); 
                } 
                catch (Exception ex) { };

            }
            else
            {
                try { 
                    button.Dispatcher.Invoke(() => { 
                        button.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerInactiveIcon"]); 
                        button.Cursor = Cursors.Hand; 
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
            try
            {//修改Canvas指针
                printCanvas.Dispatcher.Invoke(() => {
                    printCanvas.Cursor = Cursors.Cross;
                });
            }
            catch (Exception ex) { };
            
            try
            {//修改清除画板按钮状态
                deleteIcon.Dispatcher.Invoke(() => {
                    deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteIcon"]);
                    deleteIcon.Cursor = Cursors.Hand;
                });
            }
            catch (Exception ex) { };
            try
            {//修改颜色选择按钮状态
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
            try
            {//修改Canvas指针
                printCanvas.Dispatcher.Invoke(() => {
                    printCanvas.Cursor = Cursors.Arrow;
                });
            }
            catch (Exception ex) { };
            try
            {
                deleteIcon.Dispatcher.Invoke(() => {//修改清除画板按钮状态
                    deleteIcon.SetValue(Button.StyleProperty, Application.Current.Resources["DeleteInactiveIcon"]);
                    deleteIcon.Cursor = Cursors.Arrow;
                });
                colorChooser.Dispatcher.Invoke(() => {//修改颜色选择按钮状态
                    colorChooser.SetValue(Button.StyleProperty, Application.Current.Resources["ColorChoserDiabled"]);
                    colorChooser.Cursor = Cursors.Arrow;
                });
            }
            catch (Exception ex) { };
            
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
                            try
                            {
                                image.Dispatcher.Invoke(() => {
                                    image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerActivedIcon"]);
                                    image.Tag = tagHead + "" + 1;
                                });
                            }
                            catch (Exception ex) { };
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
                        try
                        {
                            image.Dispatcher.Invoke(() => {
                                image.SetValue(Button.StyleProperty, Application.Current.Resources["ComputerIcon"]);
                                image.Tag = tagHead + "" + 0;
                            });
                        }
                        catch (Exception ex) { };
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
                if (isStudent == false || tagHead == userPosition)
                {
                    //此处添加禁用远端某学生录音的方法
                    if (tagTail == 0)
                    {
                        mute(tagHead);
                        server.silenceStudent(roomId,tagHead, true);
                    }

                    else {
                        unMute(tagHead);
                        server.silenceStudent(roomId,tagHead, false);
                    }
                        
                }
                else
                {
                    return;
                }
                    
                //根据状态不同进行切换，此处仅负责按钮的样式
                if (tagTail == 0)
                {
                    try
                    {
                        image.Dispatcher.Invoke(() => {
                            image.SetValue(Button.StyleProperty, Application.Current.Resources["RecordBannedIcon"]);
                            image.Tag = tagHead + "" + 1;
                        });
                    }
                    catch (Exception ex) { };
                }
                else
                {
                    try
                    {
                        image.Dispatcher.Invoke(() => {
                            image.SetValue(Button.StyleProperty, Application.Current.Resources["RecordIcon"]);
                            image.Tag = tagHead + "" + 0;
                        });
                    }
                    catch (Exception ex) { };
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

        // 恢复第number号
        private void unMute(int number)
        {
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
            newLines(startPoint);
            isDrawing = true;

            //以下是将画板变化更新到服务器上
            server.updateCanvas(roomId, startPointPosition, currentColor, 0);
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
            if (canControl == false)
                return;
            if (e.LeftButton == MouseButtonState.Pressed && isDrawing == true) {

                Point point = e.GetPosition(printCanvas);
                int count = pointsList.Count(); // 总点数  
                

                // 去重复点
                if (count > 0) {
                    if (point.X - pointsList[count - 1][0] != 0 || point.Y - pointsList[count - 1][1] != 0) {
                        drawLines(point,count); 
                        //以下是将画板变化更新到服务器上
                        server.updateCanvas(roomId, pointPosition, null, count);
                    }
                }
            }
        }

        /// <summary>
        /// 实际绘制直线的方法
        /// </summary>
        /// <param name="newPoint"></param>
        /// <param name="count"></param>
        private void drawLines(Point newPoint,int count) {
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
            //将点的坐标添加至List中
            double[] pointPosition = new double[2];
            pointPosition[0] = newPoint.X;
            pointPosition[1] = newPoint.Y;
            pointsList.Add(pointPosition);
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
            ClearCanvas();
            mouseClickedTag = 0;

            //以下是将画板变化更新到服务器上
            server.updateCanvas(roomId,null, null, -1);
        }
        /// <summary>
        /// 实际清除Canvas的命令
        /// </summary>
        private void ClearCanvas() {
            printCanvas.Children.Clear();
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
            pushTool.Quit();
            server.setEmptyPosition(roomId, userPosition);
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
        /// 初始化连接，获得服务器端自己对应的IP
        /// </summary>
        private void socketTest() {
            Socket connectToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectToServer.Connect("172.19.241.249", 8086);
            string str = "firstConnect";
            connectToServer.Send(System.Text.Encoding.Default.GetBytes(str));
            //回信
            byte[] ipByte = new byte[1024];
            int count = connectToServer.Receive(ipByte);
            string ip = System.Text.Encoding.UTF8.GetString(ipByte, 0, count);
            connectToServer.Close();
            IPs[userPosition] = ip;
            //测试socket连接
            Console.WriteLine("Local IP is " + ip);
        }

        /// <summary>
        /// 学生端获取教师端IP地址，并开启自身的Socket监听
        /// </summary>
        private void studentSocket() {
            //获取教师IP
            Socket connectToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectToServer.Connect("172.19.241.249", 8086);
            string str = "getTeacher";
            connectToServer.Send(System.Text.Encoding.Default.GetBytes(str));
            //回信
            byte[] ipByte = new byte[1024];
            int count = connectToServer.Receive(ipByte);
            string teacherIP = System.Text.Encoding.UTF8.GetString(ipByte, 0, count);
            connectToServer.Close();
            IPs[0] = teacherIP;
            //与教师连接
            Socket connectToTeacher = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectToServer.Connect(teacherIP, 8086);
            str = "ConnectToTeacher@" + userPosition;
            connectToTeacher.Send(System.Text.Encoding.Default.GetBytes(str));
            connectToTeacher.Close();



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
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 8086);
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
        /// 学生端Socket的命令分析器，用于分析教师的命令
        /// </summary>
        /// <param name="socketOrder"></param>
        private void orderAnalyze(Socket socketOrder) {
            byte[] readBuff = new byte[1024];
            int count = socketOrder.Receive(readBuff);
            string orders = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);

            string[] order = orders.Split('@');
            if (order[0].Equals("BanVoice"))//静音命令 格式"BanVoice@'userPosition'"
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
                mute(banPosition);
            }
            else if (order[0].Equals("EnableVoice")) //取消静音命令 格式"EnableVoice@'userPosition'"
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
                unMute(enablePosition);
            }
            else if (order[0].Equals("EnableControl"))//教师移交控制权命令 格式"EnableControl@'userPosition'"
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
            else if (order[0].Equals("DisableControl"))//教师拿回控制权命令 格式"DisableControl@'userPosition'"
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
                }
                else//学生收到教师收回他人控制权命令
                {
                    DeactivateComputerIcon(disablePosition, false);
                }
            }
            else if (order[0].Equals("Color"))//修改颜色命令 格式"Color@'userPosition'@'A'@'R'@'G'@'B'"
            {
                if (order.Length < 6)
                    return;
                currentColor[0] = byte.Parse(order[2]);
                currentColor[1] = byte.Parse(order[3]);
                currentColor[2] = byte.Parse(order[4]);
                currentColor[3] = byte.Parse(order[5]);
                colorChooser.Fill = new SolidColorBrush(Color.FromArgb(currentColor[0], currentColor[1], currentColor[2], currentColor[3]));
            }
            else if (order[0].Equals("Point"))
            {//画板更新命令 格式"Point@'userPosition'@'具体操作'@'X'@'Y",因为是一个Socket多次发送，所以需要用循环便利所有缓冲区。
                for (int position = 0; position < order.Length; position = position + 5)
                {
                    if (order[position].Equals("Point") == false)
                        break;
                    switch (int.Parse(order[position + 2]))
                    {
                        case -1://具体操作值为-1 表示清除画板
                            ClearCanvas();
                            break;
                        case 0://具体操作值为0 表示新建一条线
                            newLines(new Point(double.Parse(order[position + 3]), double.Parse(order[position + 4])));
                            break;
                        case 1://具体操作值为1 表示添加新的点
                            drawLines(new Point(double.Parse(order[position + 3]), double.Parse(order[position + 4])), pointsList.Count);
                            break;
                    }
                }

            }
            else if (order[0].Equals("Quit"))
            {//退出命令 格式"Quit@'userPosition'"
                if (order.Length < 2)
                    return;
                if (int.Parse(order[1]) == 0)
                {
                    socketOrder.Close();

                    RoomControlWindow roomControl = new RoomControlWindow(user, this.server);
                    Window thisWindow = Window.GetWindow(this);
                    thisWindow.Close();
                    roomControl.Show();
                }
                else
                {
                    IPs[int.Parse(order[1])] = null;
                    socketOrder.Close();
                    return;
                }

            }
            else if (order[0].Equals("ConnectToTeacher")) { //学生连接教师命令 格式"ConnectToTeacher@'userPosition'"
                string studentIP = socketOrder.RemoteEndPoint.ToString();
                IPs[userPosition] = studentIP;
                socketOrder.Close();
                return;
            }
            socketOrder.Close();
        }
        
    }
}
