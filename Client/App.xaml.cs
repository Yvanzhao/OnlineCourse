using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Unosquare.FFME;

namespace OnlineCourse
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Library.FFmpegDirectory = Directory.GetCurrentDirectory() + "/ffmpeg";
            //Library.LoadFFmpeg();
            MediaElement.FFmpegMessageLogged += (s, ev) =>
            {
                System.Diagnostics.Debug.WriteLine(ev.Message);
            };
            base.OnStartup(e);
        }
    }
}
