using AForge.Video.DirectShow;
using System;
using System.Diagnostics;
using System.IO;

namespace OnlineCourse
{
    class LiveCapture
    {
        public static string ffmpeg = Directory.GetCurrentDirectory() + "/ffmpeg/ffmpeg.exe";
        private Process cmdProcess;
        private String videoName;
        private String audioName;

        public LiveCapture()
        {
            getDeviceName();
        }

        // 录制桌面+麦克风
        public void StartDesktop(string offset_x, string offset_y, string videoSize, string address)
        {

            string pushstream;
            pushstream = "-thread_queue_size 128 -rtbufsize 10K -start_time_realtime 0 -f gdigrab -offset_x " + offset_x + " -offset_y " + offset_y +
                " -video_size " + videoSize + " -i desktop -f dshow -i audio=\"" + audioName +
                "\" -vcodec libx264 -preset:v ultrafast -tune:v zerolatency -threads 1 -b:v 200k -g 10 -acodec aac -f flv " +
                "rtmp://172.19.241.249:8082/live/" + address;
            CmdRun(ffmpeg, pushstream);
            
        }

        // 录制摄像头+麦克风
        public void StartCamera(string address)
        {

            //ffmpeg - f dshow - i video = "Integrated Camera" - vcodec libx264 - acodec copy - preset:v ultrafast -tune:v zerolatency -f flv rtmp://eguid.cc:1935/rtmp/eguid
            string pushstream;
            pushstream = "-rtbufsize 3041280*50 -thread_queue_size 128 -start_time_realtime 0 -f dshow -i video=\"" + videoName+"\""+":audio=\"" + audioName +
                "\" -vcodec libx264 -preset:v ultrafast -tune:v zerolatency -threads 1 -b:v 200k -g 20 -acodec aac -f flv " +
                "rtmp://172.19.241.249:8082/live/" + address;
            CmdRun(ffmpeg, pushstream);

        }

        public void Quit()
        {

            cmdProcess.StandardInput.WriteLine((char)113);
            cmdProcess.StandardInput.Flush();

            if (cmdProcess.WaitForExit(3000)) cmdProcess.Close();
            else cmdProcess.Kill();

            cmdProcess = null;
        }

        private void getDeviceName()
        {
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            FilterInfoCollection audeoDevices = new FilterInfoCollection(FilterCategory.AudioInputDevice);
            if (videoDevices.Count == 0)
                throw new ApplicationException();
            if (audeoDevices.Count == 0)
                throw new ApplicationException();
            audioName = audeoDevices[0].Name;
            videoName = videoDevices[0].Name;
        }

        private void CmdRun(string fileName, string arguments)
        {

            if (cmdProcess == null)
            {
                cmdProcess = new Process();
            }

            cmdProcess.StartInfo.FileName = fileName;
            cmdProcess.StartInfo.UseShellExecute = false;
            cmdProcess.StartInfo.RedirectStandardInput = true;
            cmdProcess.StartInfo.RedirectStandardOutput = true;
            cmdProcess.StartInfo.RedirectStandardError = false;
            cmdProcess.StartInfo.CreateNoWindow = true;
            cmdProcess.StartInfo.Arguments = arguments;
            Console.WriteLine(arguments);
            cmdProcess.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

            cmdProcess.Start();
        }
    }
}
