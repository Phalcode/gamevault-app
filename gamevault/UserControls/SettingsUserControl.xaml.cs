using gamevault.Models;
using gamevault.ViewModels;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using Windows.ApplicationModel;
using System;
using gamevault.Helper;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for SettingsUserControl.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl
    {
        private SettingsViewModel ViewModel { get; set; }
        public SettingsUserControl()
        {
            InitializeComponent();
            ViewModel = SettingsViewModel.Instance;
            this.DataContext = ViewModel;
        }

        private void ClearImageCache_Clicked(object sender, RoutedEventArgs e)
        {
            ViewModel.IsOnIdle = false;
            try
            {
                Directory.Delete(AppFilePath.ImageCache, true);
                Directory.CreateDirectory(AppFilePath.ImageCache);
                MainWindowViewModel.Instance.AppBarText = "Image cache cleared";
            }
            catch
            {
                MainWindowViewModel.Instance.AppBarText = "Something went wrong while the image cache was cleared";
            }
            ViewModel.IsOnIdle = true;
        }
        private void ClearOfflineCache_Clicked(object sender, RoutedEventArgs e)
        {
            ViewModel.IsOnIdle = false;
            try
            {
                if (File.Exists(AppFilePath.IgnoreList))
                {
                    File.Delete(AppFilePath.IgnoreList);
                }
                if (File.Exists(AppFilePath.OfflineCache))
                {
                    File.Delete(AppFilePath.OfflineCache);
                }
                MainWindowViewModel.Instance.AppBarText = "Offline cache cleared";
            }
            catch
            {
                MainWindowViewModel.Instance.AppBarText = "Something went wrong while the offline cache was cleared";
            }
            ViewModel.IsOnIdle = true;
        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.IsWindowsPackage)
            {
                uiAutostartToggle.IsOn = await AutostartHelper.IsWindowsPackageAutostartEnabled();
            }
            else
            {
                uiAutostartToggle.IsOn = AutostartHelper.RegistryAutoStartKeyExists();
            }
            uiAutostartToggle.Toggled += AppAutostart_Toggled;
        }
        private async void AppAutostart_Toggled(object sender, RoutedEventArgs e)
        {
            if (App.IsWindowsPackage)
            {
                await AutostartHelper.HandleWindowsPackageAutostart();
            }
            else
            {
                if (AutostartHelper.RegistryAutoStartKeyExists())
                {
                    AutostartHelper.RegistryDeleteAutostartKey();
                }
                else
                {
                    AutostartHelper.RegistryCreateAutostartKey();
                }
            }
        }
    }
}
