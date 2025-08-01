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
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Shell;

namespace gamevault
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Singleton
        private static App instance = null;
        private static readonly object padlock = new object();

        public static App Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new App();
                    }
                    return instance;
                }
            }
        }
        #endregion
        public static bool HideToSystemTray = true;
        public static bool IsWindowsPackage = false;

        public static CommandOptions? CommandLineOptions { get; internal set; } = null;

        private NotifyIcon m_Icon;
        private JumpList jumpList;



        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AnalyticsHelper.Instance.InitHeartBeat();
            AnalyticsHelper.Instance.RegisterGlobalEvents();
            AnalyticsHelper.Instance.SendCustomEvent(CustomAnalyticsEventKeys.APP_INITIALIZED, AnalyticsHelper.Instance.GetSysInfo());
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);

            try
            {
                LoginWindow loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();
                if (result == null || result == false)
                    Shutdown();
                loginWindow = null;
#if WINDOWS
                InitNotifyIcon();
                InitJumpList();
#endif
            }
            catch (Exception ex)
            {
                LogUnhandledException(ex);
            }

            //AnalyticsHelper.Instance.SendCustomEvent(CustomAnalyticsEventKeys.USER_SETTINGS, AnalyticsHelper.Instance.PrepareSettingsForAnalytics());

            //            bool startMinimizedByPreferences = false;
            //            bool startMinimizedByCLI = false;

            //            if ((CommandLineOptions?.Minimized).HasValue)
            //                startMinimizedByCLI = CommandLineOptions!.Minimized!.Value;
            //            else if (SettingsViewModel.Instance.BackgroundStart)
            //                startMinimizedByPreferences = true;

            //            if (!startMinimizedByPreferences && MainWindow == null)
            //            {
            //                MainWindow = new MainWindow();
            //                MainWindow.Show();
            //            }
            //            if (startMinimizedByCLI && MainWindow != null)
            //            {
            //                MainWindow.Hide();
            //            }          
            //            // After the app is created and most things are instantiated, handle any special command line stuff
            if (PipeServiceHandler.Instance != null)
            {
                // Strictly speaking we should hold up all commands until we have a confirmed login & setup is complete, but for now we'll assume that auto-login has worked
                PipeServiceHandler.Instance.IsReadyForCommands = true;
                await PipeServiceHandler.Instance.HandleCommand(App.CommandLineOptions);
            }
        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ProcessShepherd.Instance.KillAllChildProcesses();
#if DEBUG
            e.Handled = false;
#else
            LogUnhandledException(e.Exception);
#endif
        }

        public void LogUnhandledException(Exception e)
        {
            Application.Current.DispatcherUnhandledException -= new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);
            string errorMessage = $"MESSAGE:\n{e.Message}\nINNER_EXCEPTION:{(e.InnerException != null ? "" + e.InnerException.Message : null)}";
            string errorStackTrace = $"STACK_TRACE:\n{(e.StackTrace != null ? "" + e.StackTrace : null)}";
            string errorLogPath = $"{ProfileManager.ErrorLogDir}\\GameVault_ErrorLog_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.txt";
            if (!File.Exists(errorLogPath))
            {
                Directory.CreateDirectory(ProfileManager.ErrorLogDir);
                File.Create(errorLogPath).Close();
            }
            File.WriteAllText(errorLogPath, errorMessage + "\n" + errorStackTrace);
            AnalyticsHelper.Instance.SendErrorLog(e);
            ExceptionWindow exWin = new ExceptionWindow();
            exWin.ShowDialog();
            ShutdownApp();
        }

        private void InitNotifyIcon()
        {
            m_Icon = new NotifyIcon();
            m_Icon.Text = "GameVault";
            m_Icon.MouseDoubleClick += NotifyIcon_DoubleClick;
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/icon.ico")).Stream;
            m_Icon.Icon = new System.Drawing.Icon(iconStream);
            m_Icon.ContextMenuStrip = new ContextMenuStrip();
            m_Icon.ContextMenuStrip.Items.Add("Library", new Bitmap(Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/ContextMenuIcon_Library.png")).Stream), Navigate_Tab_Click);
            m_Icon.ContextMenuStrip.Items.Add("Downloads", new Bitmap(Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/ContextMenuIcon_Downloads.png")).Stream), Navigate_Tab_Click);
            m_Icon.ContextMenuStrip.Items.Add("Community", new Bitmap(Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/ContextMenuIcon_Community.png")).Stream), Navigate_Tab_Click);
            m_Icon.ContextMenuStrip.Items.Add("Settings", new Bitmap(Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/ContextMenuIcon_Settings.png")).Stream), Navigate_Tab_Click);
            m_Icon.ContextMenuStrip.Items.Add("Exit", new Bitmap(Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/ContextMenuIcon_Exit.png")).Stream), NotifyIcon_Exit_Click);
            m_Icon.Visible = true;
        }
        private void InitJumpList()
        {
            jumpList = JumpList.GetJumpList(Application.Current);
            if (jumpList == null)
            {
                jumpList = new JumpList();
                JumpList.SetJumpList(Application.Current, jumpList);
            }

            JumpTask LibraryTask = new JumpTask() { Title = "Library", Description = "Open Library", CustomCategory = "Actions", ApplicationPath = Process.GetCurrentProcess().MainModule.FileName, Arguments = "show --jumplistcommand=0" };
            JumpTask DownloadsTask = new JumpTask() { Title = "Downloads", Description = "Open Downloads", CustomCategory = "Actions", ApplicationPath = Process.GetCurrentProcess().MainModule.FileName, Arguments = "show --jumplistcommand=1" };
            JumpTask CommunityTask = new JumpTask() { Title = "Community", Description = "Open Community", CustomCategory = "Actions", ApplicationPath = Process.GetCurrentProcess().MainModule.FileName, Arguments = "show --jumplistcommand=2" };
            JumpTask SettingsTask = new JumpTask() { Title = "Settings", Description = "Open Settings", CustomCategory = "Actions", ApplicationPath = Process.GetCurrentProcess().MainModule.FileName, Arguments = "show --jumplistcommand=3" };
            JumpTask ExitTask = new JumpTask() { Title = "Exit", Description = "Exit GameVault", CustomCategory = "Actions", ApplicationPath = Process.GetCurrentProcess().MainModule.FileName, Arguments = "show --jumplistcommand=15" };
            jumpList.JumpItems.Add(LibraryTask);
            jumpList.JumpItems.Add(DownloadsTask);
            jumpList.JumpItems.Add(CommunityTask);
            jumpList.JumpItems.Add(SettingsTask);
            jumpList.JumpItems.Add(ExitTask);

        }
        public void SetJumpListGames()
        {
            try
            {
                jumpList.JumpItems.RemoveRange(5, jumpList.JumpItems.Count - 5);// Remove all previous games. Now we add the current game list
                var lastGames = InstallViewModel.Instance.InstalledGames.Take(5).ToArray();
                foreach (var game in lastGames)
                {
                    if (!jumpList.JumpItems.OfType<JumpTask>().Any(jt => jt.Title == game.Key.Title))
                    {
                        JumpTask gameTask = new JumpTask()
                        {
                            Title = game.Key.Title,
                            CustomCategory = "Last Played",
                            ApplicationPath = Process.GetCurrentProcess()?.MainModule?.FileName,
                            IconResourcePath = Preferences.Get(AppConfigKey.Executable, $"{game.Value}\\gamevault-exec"),
                            Arguments = $"start --gameid={game.Key.ID}"
                        };
                        jumpList.JumpItems.Add(gameTask);
                    }
                }

                jumpList.Apply();
            }
            catch { }
        }
        public void ResetJumpListGames()
        {
            try
            {
                jumpList.JumpItems.RemoveRange(5, jumpList.JumpItems.Count - 5);
                jumpList.Apply();
            }
            catch { }
        }
        private void NotifyIcon_DoubleClick(Object sender, EventArgs e)
        {

            if (MainWindow == null || MainWindow.GetType() != typeof(MainWindow))
                return;

            if (MainWindow.IsVisible == false)
            {
                MainWindow.Show();
            }
            else if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }
        }

        public void SetTheme(string themeUri)
        {
            App.Current.Resources.MergedDictionaries[0] = new ResourceDictionary() { Source = new Uri(themeUri) };
            App.Current.Resources.MergedDictionaries[1] = new ResourceDictionary() { Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Base.xaml") };
        }
        public void ResetToDefaultTheme()
        {
            App.Current.Resources.MergedDictionaries[0] = new ResourceDictionary() { Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeDefaultDark.xaml") };
            App.Current.Resources.MergedDictionaries[1] = new ResourceDictionary() { Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Base.xaml") };
        }
        private async void NotifyIcon_Exit_Click(Object sender, EventArgs e)
        {
            await ExitApp();
        }
        public async Task ExitApp()
        {
            if ((DownloadsViewModel.Instance.DownloadedGames.Where(g => g.IsDownloading() == true)).Count() > 0)
            {
                if (MainWindow.IsVisible == false)
                {
                    MainWindow.Show();
                }
                MessageDialogResult result = await ((MetroWindow)MainWindow).ShowMessageAsync($"Downloads are still running in the background, are you sure you want to exit the app anyway?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    MainWindowViewModel.Instance.Downloads.CancelAllDownloads();
                    ShutdownApp();
                }
            }
            else
            {
                ShutdownApp();
            }
        }
        private void Navigate_Tab_Click(Object sender, EventArgs e)
        {
            NotifyIcon_DoubleClick(null, null);
            for (int count = 0; count < m_Icon.ContextMenuStrip.Items.Count; count++)
            {
                if (m_Icon.ContextMenuStrip.Items[count].Text == sender.ToString())
                {
                    MainWindowViewModel.Instance.SetActiveControl((MainControl)count);
                    break;
                }
            }
        }

        private void ShutdownApp()
        {
            HideToSystemTray = false;
            ProcessShepherd.Instance.KillAllChildProcesses();
            if (m_Icon != null)
            {
                m_Icon.Icon.Dispose();
                m_Icon.Dispose();
            }
            Shutdown();
        }
        public bool IsWindowActiveAndControlInFocus(MainControl control)
        {
            if (Current.MainWindow == null)
                return false;

            return Current.MainWindow.IsActive && MainWindowViewModel.Instance.ActiveControlIndex == (int)control;
        }

    }
}
