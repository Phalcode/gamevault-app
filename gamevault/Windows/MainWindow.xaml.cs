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
                case MainControl.Installs:
                    {
                        MainWindowViewModel.Instance.ActiveControl = MainWindowViewModel.Instance.Installs;
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
                await MainWindowViewModel.Instance.Community.InitUserList();
            }
            else if (LoginState.Unauthorized == state || LoginState.Forbidden == state)
            {
                MainWindowViewModel.Instance.AppBarText = "You are not logged in";
            }
            else if (LoginState.Error == state)
            {
                MainWindowViewModel.Instance.AppBarText = "Could not connect to server";
            }
            await MainWindowViewModel.Instance.Installs.StartInstalledGamesTracker();
            await MainWindowViewModel.Instance.Downloads.RestoreDownloadedGames();
            MainWindowViewModel.Instance.UserIcon = LoginManager.Instance.GetCurrentUser();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            new ToastContentBuilder().AddText("Notification").AddText("App is still running in the system tray").Show();
        }

        private void UserIcon_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.SetActiveControl(MainControl.Community);
        }
        private void WindowCommand_Clicked(object sender, RoutedEventArgs e)
        {
            string? url = (string)((FrameworkElement)sender).Tag;
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            e.Handled = true;
        }
    }
}
