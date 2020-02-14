using System;
using System.Diagnostics;
using System.IO;

namespace OnlineCourse
{
    class LiveCapture
    {
        public static string ffmpeg = Directory.GetCurrentDirectory() + "/ffmpeg/ffmpeg.exe";
        public static Process cmdProcess;

        private static void CmdRun(string fileName, string arguments)
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
            cmdProcess.Start();
        }

        public static void Start(string audioDevice, string videoDevice, string offset_x, string offset_y, string videoSize)
        {
            string time = DateTime.Now.ToString("yyyyMMddHHmmss");
            string[] fileName = new string[3];
            fileName[0] = "screen" + time + ".mp4";
            fileName[1] = "camera" + time + ".mp4";
            fileName[2] = "microwave" + time + ".wav";

            //string arguments;
            //arguments = "-f gdigrab -framerate 30 -offset_x " + offset_x + " -offset_y " + offset_y + "" +
            //    " -video_size " + videoSize + " -i desktop -f dshow -i video=\"" + videoDevice + "\" -f dshow -i audio=\"" +
            //    audioDevice + "\" -vcodec libx264 -acodec acc -strict -2 -map 0:0 " + fileName[0]
            //    + " -map 1:0 " + fileName[1] + " -map 2:0 " + fileName[2];
            //Console.WriteLine(arguments);
            //CmdRun(ffmpeg, arguments);

            string pushstream;
            pushstream = "-f gdigrab -framerate 30 -offset_x " + offset_x + " -offset_y " + offset_y + "" +
                " -video_size " + videoSize + " -i desktop -vcodec libx264 -preset:v ultrafast -tune:v zerolatency -f flv " +
                "rtmp://localhost:1935/live/home";
            CmdRun(ffmpeg, pushstream);

            // ffplay rtmp://localhost:1935/live/home
        }
        public static void Quit()
        {

            cmdProcess.StandardInput.WriteLine((char)113);
            cmdProcess.StandardInput.Flush();

            if (cmdProcess.WaitForExit(3000)) cmdProcess.Close();
            else cmdProcess.Kill();

            cmdProcess = null;
        }
    }
}
