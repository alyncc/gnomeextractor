using System;
using System.Windows;
using System.IO;

namespace GnomeExtractor
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            base.OnStartup(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // put your tracing or logging code here (I put a message box as an example)
            FileStream fs = new FileStream("error.log", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(DateTime.Now);
            sw.WriteLine();
            sw.Write(e.ExceptionObject.ToString());
            sw.Close();
            //MemoryStream ms = new MemoryStream()
            //ms.
            //fs.Write
            MessageBox.Show("An error has occured. Please, send your error.log file to forum/vk.com discussion");
        }
    }
}
