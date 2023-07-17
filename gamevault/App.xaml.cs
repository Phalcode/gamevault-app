using gamevault.Models;
using gamevault.ViewModels;
using gamevault.Windows;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Linq;
using gamevault.Helper;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO.Pipes;
using ControlzEx.Standard;
using System.Windows.Threading;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel;

namespace gamevault
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIcon m_Icon;

        private GameTimeTracker m_gameTimeTracker;
        private async void Application_Startup(object sender, StartupEventArgs e)
        {

            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);
            try
            {
                NewNameMigrationHelper.MigrateIfNeeded();
            }
            catch (Exception ex)
            {
                LogUnhandledException(ex);
                //m_StoreHelper.NoInternetException();              
            }

#if DEBUG
            AppFilePath.InitDebugPaths();
#else
            try
            {
                UpdateWindow updateWindow = new UpdateWindow(); 
                updateWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogUnhandledException(ex);
                //m_StoreHelper.NoInternetException();              
            }
            int pcount = Process.GetProcessesByName(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name).Count();
            //System.Windows.MessageBox.Show("pcount: "+pcount);
            if (pcount != 1)
            {
                var client = new NamedPipeClientStream("GameVault");
                client.Connect();
                StreamWriter writer = new StreamWriter(client);
                writer.WriteLine("ShowMainWindow");
                writer.Flush();
                Shutdown();
            }
            else
            {
                StartServer();
            }
#endif
            #region DirectoryCreation
            if (!Directory.Exists(AppFilePath.ImageCache))
            {
                Directory.CreateDirectory(AppFilePath.ImageCache);
            }
            if (!Directory.Exists(AppFilePath.ConfigDir))
            {
                Directory.CreateDirectory(AppFilePath.ConfigDir);
            }
            #endregion
            await LoginManager.Instance.StartupLogin();
            m_gameTimeTracker = new GameTimeTracker();
            await m_gameTimeTracker.Start();

            if (false == SettingsViewModel.Instance.BackgroundStart && MainWindow == null)
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            InitNotifyIcon();

        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
            e.Handled = false;
#else
            LogUnhandledException(e);
#endif
        }
        public void LogUnhandledException(Exception e)
        {
            string errorMessage = $"MESSAGE:\n{e.Message}\nINNER_EXCEPTION:{(e.InnerException != null ? "" + e.InnerException.Message : null)}\nSTACK_TRACE:\n{(e.StackTrace != null ? "" + e.StackTrace : null)}";
            string errorLogPath = $"{AppFilePath.ErrorLog}\\GameVault_ErrorLog_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.txt";
            if (!File.Exists(errorLogPath))
            {
                Directory.CreateDirectory(AppFilePath.ErrorLog);
                File.Create(errorLogPath).Close();
            }
            File.WriteAllText(errorLogPath, errorMessage);
        }
        void LogUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogUnhandledException(e.Exception);
            System.Windows.MessageBox.Show("Something went wrong. View error log for more details", "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown();
        }
        private void StartServer()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    using (var server = new NamedPipeServerStream("GameVault"))
                    {
                        server.WaitForConnection();
                        StreamReader reader = new StreamReader(server);

                        var line = reader.ReadLine();
                        if (line == "ShowMainWindow")
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (MainWindow == null)
                                {
                                    MainWindow = new MainWindow();
                                }
                                MainWindow.Show();
                            });
                        }
                    }
                }
            });
        }
        private void InitNotifyIcon()
        {
            m_Icon = new NotifyIcon();
            m_Icon.MouseDoubleClick += NotifyIcon_DoubleClick;
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/icon.ico")).Stream;
            m_Icon.Icon = new System.Drawing.Icon(iconStream);
            m_Icon.ContextMenuStrip = new ContextMenuStrip();
            m_Icon.ContextMenuStrip.Items.Add("Show", null, NotifyIcon_DoubleClick);
            m_Icon.ContextMenuStrip.Items.Add("Delete system32", null, DeleteSytem32);
            m_Icon.ContextMenuStrip.Items.Add("Exit", null, NotifyIcon_Exit_Click);
            m_Icon.Visible = true;
        }
        private void DeleteSytem32(Object sender, EventArgs e)
        {
            MainWindow = new MetroWindow();
            ((MetroWindow)MainWindow).UseNoneWindowStyle = true;
            ((MetroWindow)MainWindow).WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ((MetroWindow)MainWindow).WindowState = WindowState.Maximized;
            ((MetroWindow)MainWindow).IgnoreTaskbarOnMaximize = true;
            ((MetroWindow)MainWindow).ResizeMode = ResizeMode.NoResize;
            ImageBrush bgimg = new ImageBrush();
            bgimg.ImageSource = new BitmapImage(new Uri("https://upload.wikimedia.org/wikipedia/commons/5/56/Bsodwindows10.png"));
            ((MetroWindow)MainWindow).Background = bgimg;
            MainWindow.Show();

        }
        private void NotifyIcon_DoubleClick(Object sender, EventArgs e)
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            else if (MainWindow.IsVisible == false)
            {
                MainWindow.Show();
            }
        }
        private async void NotifyIcon_Exit_Click(Object sender, EventArgs e)
        {
            if ((DownloadsViewModel.Instance.DownloadedGames.Where(g => g.IsDownloading() == true)).Count() > 0)
            {
                if (MainWindow.IsVisible == false)
                {
                    MainWindow.Show();
                }
                MessageDialogResult result = await ((MetroWindow)MainWindow).ShowMessageAsync($"Downloads are still running in the background, are you sure you want to close the app anyway?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    MainWindowViewModel.Instance.Downloads.CancelAllDownloads();
                    Shutdown();
                }
            }
            else
            {
                Shutdown();
            }
        }

    }
}
