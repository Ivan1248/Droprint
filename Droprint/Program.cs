using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Droprint
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 0)
                Application.Run(new Form1());
            else if (args[0] == "uninstall")
                if(MessageBox.Show("Droprint will be uninstalled.\nPress OK to continue.","Droprint") == DialogResult.OK )
                    Uninstall();
        }

        static void Uninstall()
        {
            Form1.RunOnStartup = false;
            try
            {
                //Application.UserAppDataRegistry.DeleteValue("MonitorDir");
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\Droprint");
            }
            catch { }
            ProcessStartInfo Info = new ProcessStartInfo("cmd.exe");
            Info.Arguments = "/C TIMEOUT 3 & RMDIR \"" + Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf('\\')) + "\" /S /Q";
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Process.Start(Info);
        }
    }
}
