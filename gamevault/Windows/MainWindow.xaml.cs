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

namespace gamevault.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = MainWindowViewModel.Instance;
        }
        private async void HamburgerMenuControl_OnItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs args)
        {
            if (MainWindowViewModel.Instance.ActiveControl != null && MainWindowViewModel.Instance.ActiveControl.GetType() == typeof(Wizard))
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync("", $"Do you want to leave the setup wizard?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false, DialogMessageFontSize = 50 });
                if (result == MessageDialogResult.Negative)
                {
                    return;
                }
            }
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
            AdjustWindowChrome();
            if (SettingsViewModel.Instance.SetupCompleted())
            {
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Library);
            }
            else
            {
                MainWindowViewModel.Instance.SetActiveControl(new Wizard());
            }
            LoginState state = LoginManager.Instance.GetState();
            if (LoginState.Success == state)
            {
                if (Preferences.Get(AppConfigKey.LibStartup, AppFilePath.UserFile) == "1")
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
                MainWindowViewModel.Instance.AppBarText = LoginManager.Instance.GetLoginMessage();
                MainWindowViewModel.Instance.Library.ShowLibraryError();
            }
            await MainWindowViewModel.Instance.Library.GetGameInstalls().RestoreInstalledGames();
            await MainWindowViewModel.Instance.Downloads.RestoreDownloadedGames();
            MainWindowViewModel.Instance.UserAvatar = LoginManager.Instance.GetCurrentUser();

            uiNewsBadge.Badge = await CheckForNews() ? "!" : "";
            InitNewsTimer();

            if (await IsServerTooOutdated())
            {
                try
                {
                    MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync("CLIENT-SERVER-INCOMPABILITY DETECTED",
                          $"Your GameVault Client is not compatible with the GameVault Server you are using (<13.0.0). This server is too old for your client.\r\n\r\nYou have the following options:\r\n",
                          MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Install the older version of the client from GitHub", NegativeButtonText = "Update the server to version 13", AnimateHide = false, DialogMessageFontSize = 25, DialogTitleFontSize = 30 });
                    if (result == MessageDialogResult.Affirmative)
                    {
                        Process.Start(new ProcessStartInfo("https://github.com/Phalcode/gamevault-app/releases") { UseShellExecute = true });
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo("https://github.com/Phalcode/gamevault-backend/releases/tag/12.2.0") { UseShellExecute = true });
                    }
                }
                catch { }
            }
        }
        //User Notification for major client/serveWPFr update 
        private async Task<bool> IsServerTooOutdated()
        {
            try
            {
                using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
                {

                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");
                    string serverResonse = await WebHelper.GetRequestAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/health");
                    string currentServerVersion = System.Text.Json.JsonSerializer.Deserialize<ServerInfo>(serverResonse).Version;
                    if (currentServerVersion == null || currentServerVersion == "")
                    {
                        return true;
                    }
                    return new Version(currentServerVersion) < new Version("13.0.0");
                }
            }
            catch { }
            return false;
        }
        //######

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.ShowToastMessage)
            {
                e.Cancel = true;
                this.Hide();
                if (Preferences.Get(AppConfigKey.RunningInTrayMessage, AppFilePath.UserFile) != "1")
                {
                    Preferences.Set(AppConfigKey.RunningInTrayMessage, "1", AppFilePath.UserFile);
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
                if (Preferences.Get(AppConfigKey.UnreadNews, AppFilePath.UserFile) == "1")
                {
                    return true;
                }
                string gameVaultNews = await WebHelper.DownloadFileContentAsync("https://gamevau.lt/news.md");
                string serverNews = await WebHelper.GetRequestAsync($"{SettingsViewModel.Instance.ServerUrl}/api/config/news");

                string hash = await CacheHelper.CreateHashAsync(gameVaultNews + serverNews);
                if (Preferences.Get(AppConfigKey.NewsHash, AppFilePath.UserFile) != hash)
                {
                    Preferences.Set(AppConfigKey.UnreadNews, "1", AppFilePath.UserFile);
                    Preferences.Set(AppConfigKey.NewsHash, hash, AppFilePath.UserFile);
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
                Preferences.Set(AppConfigKey.UnreadNews, "0", AppFilePath.UserFile);
            }
            catch
            { }
        }
        private void AdjustWindowChrome()
        {
            try
            {
                //var root = this.Template.FindName("PART_Content", this);
                //System.Windows.Controls.Panel.SetZIndex((MetroContentControl)root, 6);
                var thumb = (FrameworkElement)this.Template.FindName("PART_WindowTitleThumb", this);
                thumb.Margin = new Thickness(50, 0, 0, 0);
                System.Windows.Controls.Panel.SetZIndex(thumb, 7);
                var btnCommands = (FrameworkElement)this.Template.FindName("PART_WindowButtonCommands", this);
                System.Windows.Controls.Panel.SetZIndex(btnCommands, 8);
            }
            catch { }
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
    }
}
