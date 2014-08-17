using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;
//removed reference System.Deployment, System.Data, System.Data.DataSetExtensions

namespace Droprint
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region "Objects"
        public static bool RunOnStartup
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    return key.GetValue(Application.ProductName) != null;
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (value)
                        key.SetValue(Application.ProductName, Application.ExecutablePath);
                    else if (key.GetValue(Application.ProductName) != null)
                        key.DeleteValue(Application.ProductName);
                }
            }
        }
        Thread WorkThread;
        ManualResetEvent State = new ManualResetEvent(false);

        ContextMenu ContMenu = new ContextMenu();
        MenuItem DroprintM;
        MenuItem BtnStartup, BtnAbout;
        MenuItem BtnRun, BtnDir, BtnExit;

        FolderBrowserDialog Explorer = new FolderBrowserDialog() { Description = "Select folder to be monitored for files to be printed" };

        string PrintedDir;
        FileStream holder;
        #endregion

        #region "Loading, closing"
        private void Form1_Load(object sender, EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            LoadSettings();

            if (string.IsNullOrEmpty(Explorer.SelectedPath))
            {
                SelectDir();
                if (string.IsNullOrEmpty(Explorer.SelectedPath)) Environment.Exit(0);
            }
            else
            {
                Directory.CreateDirectory(Explorer.SelectedPath); //treba kod selectfiraˇ
                Directory.CreateDirectory(PrintedDir);
                holder = new FileStream(PrintedDir + "droprint", FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                File.SetAttributes(PrintedDir + "droprint", FileAttributes.Hidden); 
            }

            WorkThread = new Thread(new ThreadStart(Monitor)) { IsBackground = true };
            WorkThread.Start();

            LoadMenu();

            SetState(true);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            WorkThread.Abort();
            SaveSettings();
        }

        #endregion

        #region "Settings"
        private string SettingsDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Droprint\\";

        private void LoadSettings()
        {
            try
            {
                Explorer.SelectedPath = Application.UserAppDataRegistry.GetValue("MonitorDir").ToString();
                UpdatePaths();
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                Application.UserAppDataRegistry.SetValue("MonitorDir", Explorer.SelectedPath);
            }
            catch { }
        }
        #endregion

        #region "Context menu"
        private void LoadMenu()
        {
            BtnStartup = new MenuItem("Launch on system startup",
                (s, e) => { RunOnStartup = BtnStartup.Checked = !BtnStartup.Checked; });
            BtnStartup.Checked = RunOnStartup;
            BtnAbout = new MenuItem("About Droprint");
            DroprintM = new MenuItem("Droprint", new MenuItem[2] { BtnStartup, BtnAbout }) { DefaultItem = true };
            BtnDir = new MenuItem("Change folder",
                (s, e) => SelectDir());
            BtnRun = new MenuItem("Run",
                (s, e) => SetState(!State.WaitOne(0, false)));
            BtnExit = new MenuItem("Exit",
                (s, e) => Close());
            ContMenu.MenuItems.AddRange(new MenuItem[4] { DroprintM, BtnDir, BtnRun, BtnExit });
            NotifyIcon.ContextMenu = ContMenu;
        }
        #endregion

        #region "Monitor + print"

        private void SelectDir()
        {
            if (Explorer.ShowDialog() == DialogResult.OK) UpdatePaths();
        }

        private void UpdatePaths()
        {
            PrintedDir = Explorer.SelectedPath + "\\Printed\\";
            NotifyIcon.Text = "Droprint: " + Explorer.SelectedPath;
        }

        private void SetState(bool set)
        {
            if (set)
            {
                NotifyIcon.Icon = Properties.Resources.active;
                BtnRun.Text = "Pause";
                State.Set();
            }
            else
            {
                NotifyIcon.Icon = Properties.Resources.idle;
                BtnRun.Text = "Run";
                State.Reset();
            }
        }

        void Monitor()
        {
            while (State.WaitOne())
            {
                foreach (string file in Directory.GetFiles(Explorer.SelectedPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    bool fail = false;
                    try
                    {
                        //ProcessStartInfo startInfo = new ProcessStartInfo(file) { Verb = "print" };
                        //Process PrintProcess = Process.Start(startInfo);
                        //PrintProcess.WaitForExit();

                        using (PrintDialog printDialog1 = new PrintDialog())
                        {
                            if (printDialog1.ShowDialog() == DialogResult.OK)
                            {
                                System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(file);
                                info.Arguments = "\"" + printDialog1.PrinterSettings.PrinterName + "\"";
                                info.CreateNoWindow = true;
                                info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                                info.UseShellExecute = true;
                                info.Verb = "PrintTo";
                                System.Diagnostics.Process.Start(info).WaitForExit();
                            }
                        }

                    }
                    catch
                    {
                        fail = true;
                    }
                    fail = true;
                    string newPath = PrintedDir + (fail ? "[print error] " : "") + file.Substring(file.LastIndexOf('\\') + 1);
                    Directory.CreateDirectory(PrintedDir);
                    File.Delete(newPath);
                    File.Move(file, newPath);
                }
                Thread.Sleep(5000);
            }
        }

        #endregion
    }
}