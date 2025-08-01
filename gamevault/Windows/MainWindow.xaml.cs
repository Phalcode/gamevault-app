using gamevault.ViewModels;
using MahApps.Metro.Controls;
using gamevault.UserControls;
using System.Linq;
using gamevault.Helper;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using System.IO;
using System.Diagnostics;
using System;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows.Forms;
using gamevault.Models;
using ControlzEx.Standard;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Text.Json;

namespace gamevault.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IDisposable
    {
        private GameTimeTracker GameTimeTracker;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = MainWindowViewModel.Instance;
            InitBootTasks();
        }
        private void InitBootTasks()
        {
            App.HideToSystemTray = true;
            RestoreTheme();
            Task.Run(async () =>
            {
                if (GameTimeTracker == null)
                {
                    GameTimeTracker = new GameTimeTracker();
                    await GameTimeTracker.Start();
                }
            });
            AnalyticsHelper.Instance.SendCustomEvent(CustomAnalyticsEventKeys.USER_SETTINGS, AnalyticsHelper.Instance.PrepareSettingsForAnalytics());
            PipeServiceHandler.Instance.IsReadyForCommands = true;
        }
        private async void HamburgerMenuControl_OnItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs args)
        {
            MainControl activeControlIndex = (MainControl)MainWindowViewModel.Instance.ActiveControlIndex;

            switch (activeControlIndex)
            {
                case MainControl.Library:
                    {
                        MainWindowViewModel.Instance.ActiveControl = MainWindowViewModel.Instance.Library;
                        break;
                    }
                case MainControl.Settings:
                    {
                        MainWindowViewModel.Instance.ActiveControl = MainWindowViewModel.Instance.Settings;
                        break;
                    }
                case MainControl.Downloads:
                    {
                        MainWindowViewModel.Instance.ActiveControl = MainWindowViewModel.Instance.Downloads;
                        break;
                    }
                case MainControl.Community:
                    {
                        MainWindowViewModel.Instance.ActiveControl = MainWindowViewModel.Instance.Community;
                        break;
                    }
                case MainControl.AdminConsole:
                    {
                        MainWindowViewModel.Instance.ActiveControl = MainWindowViewModel.Instance.AdminConsole;
                        break;
                    }
            }
            MainWindowViewModel.Instance.LastMainControl = activeControlIndex;
        }

        private async void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            VisualHelper.AdjustWindowChrome(this);
            MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            LoginState state = LoginManager.Instance.GetState();
            if (LoginState.Success == state)
            {
                if (Preferences.Get(AppConfigKey.LibStartup, LoginManager.Instance.GetUserProfile().UserConfigFile) == "1")
                {
                    await MainWindowViewModel.Instance.Library.LoadLibrary();
                }
            }
            else if (LoginState.Unauthorized == state || LoginState.Forbidden == state)
            {
                MainWindowViewModel.Instance.AppBarText = "You are not logged in";
            }
            else if (LoginState.Error == state)
            {
                MainWindowViewModel.Instance.AppBarText = LoginManager.Instance.GetServerLoginResponseMessage();
                MainWindowViewModel.Instance.Library.ShowLibraryError();
            }
            await MainWindowViewModel.Instance.Library.GetGameInstalls().RestoreInstalledGames();
            await MainWindowViewModel.Instance.Downloads.RestoreDownloadedGames();
            LoginManager.Instance.InitOnlineTimer();
            MainWindowViewModel.Instance.UserAvatar = LoginManager.Instance.GetCurrentUser();

            uiNewsBadge.Badge = await CheckForNews() ? "!" : "";
            InitNewsTimer();

        }
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.HideToSystemTray)
            {
                e.Cancel = true;
                this.Hide();
                if (Preferences.Get(AppConfigKey.RunningInTrayMessage, LoginManager.Instance.GetUserProfile().UserConfigFile) != "1")
                {
                    Preferences.Set(AppConfigKey.RunningInTrayMessage, "1", LoginManager.Instance.GetUserProfile().UserConfigFile);
                    ToastMessageHelper.CreateToastMessage("Information", "GameVault is still running in the background");
                }
            }
        }

        private void UserAvatar_Clicked(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.Instance.Community.ShowUser(LoginManager.Instance.GetCurrentUser());
        }


        private void Premium_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.SetActiveControl(MainControl.Settings);
            MainWindowViewModel.Instance.Settings.SetTabIndex(4);
        }

        private async Task<bool> CheckForNews()
        {
            try
            {
                if (Preferences.Get(AppConfigKey.UnreadNews, LoginManager.Instance.GetUserProfile().UserConfigFile) == "1")
                {
                    return true;
                }
                string gameVaultNews = await WebHelper.GetAsync("https://gamevau.lt/news.md");
                string serverNews = await WebHelper.GetAsync($"{SettingsViewModel.Instance.ServerUrl}/api/config/news");

                string hash = await CacheHelper.CreateHashAsync(gameVaultNews + serverNews);
                if (Preferences.Get(AppConfigKey.NewsHash, LoginManager.Instance.GetUserProfile().UserConfigFile) != hash)
                {
                    Preferences.Set(AppConfigKey.UnreadNews, "1", LoginManager.Instance.GetUserProfile().UserConfigFile);
                    Preferences.Set(AppConfigKey.NewsHash, hash, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        private void InitNewsTimer()
        {
            DispatcherTimer newsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(1)
            };
            newsTimer.Tick += async (s, e) => { uiNewsBadge.Badge = await CheckForNews() ? "!" : ""; };
            newsTimer.Start();
        }

        private void News_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.OpenPopup(new NewsPopup());
            try
            {
                uiNewsBadge.Badge = "";
                Preferences.Set(AppConfigKey.UnreadNews, "0", LoginManager.Instance.GetUserProfile().UserConfigFile);
            }
            catch
            { }
        }
        private void Shortlink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                string? url = (string)((FrameworkElement)sender).Tag;
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                e.Handled = true;
            }
            catch { }
        }

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(MainWindowViewModel.Instance.AppBarText);
            }
            catch { }
        }
        private void RestoreTheme()
        {
            try
            {
                string currentThemeString = Preferences.Get(AppConfigKey.Theme, LoginManager.Instance.GetUserProfile().UserConfigFile, true);
                if (currentThemeString != string.Empty)
                {
                    ThemeItem currentTheme = JsonSerializer.Deserialize<ThemeItem>(currentThemeString)!;

                    if (App.Current.Resources.MergedDictionaries[0].Source.OriginalString != currentTheme.Path)
                    {
                        App.Instance.SetTheme(currentTheme.Path);
                    }
                }
            }
            catch { }
        }
        public void Dispose()
        {
            GameTimeTracker.Stop();
            MainWindowViewModel.Instance.Downloads.CancelAllDownloads();
            InstallViewModel.Instance.InstalledGames.Clear();
            DownloadsViewModel.Instance.DownloadedGames.Clear();
            ProcessShepherd.Instance.KillAllChildProcesses();
            App.HideToSystemTray = false;
            App.Instance.ResetToDefaultTheme();
            LoginManager.Instance.StopOnlineTimer();
            App.Instance.ResetJumpListGames();
            PipeServiceHandler.Instance.IsReadyForCommands = false;
            MainWindowViewModel.Instance.UserAvatar = null;
            this.Close();
        }
    }
}
