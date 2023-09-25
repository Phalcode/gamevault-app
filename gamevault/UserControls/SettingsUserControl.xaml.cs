using gamevault.Models;
using gamevault.ViewModels;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using Windows.ApplicationModel;
using System;
using gamevault.Helper;
using System.Threading.Tasks;
using gamevault.Converter;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;
using ABI.System;

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
                ViewModel.ImageCacheSize = 0;
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
                ViewModel.OfflineCacheSize = 0;
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

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TabControl)sender).SelectedIndex == 2)
            {
                ViewModel.ImageCacheSize = await CalculateDirectorySize(new DirectoryInfo(AppFilePath.ImageCache));
                ViewModel.OfflineCacheSize = (File.Exists(AppFilePath.OfflineCache) ? new FileInfo(AppFilePath.OfflineCache).Length : 0);
            }
        }
        private async Task<long> CalculateDirectorySize(DirectoryInfo d)
        {
            return await Task<long>.Run(async () =>
            {
                long size = 0;
                try
                {
                    FileInfo[] fis = d.GetFiles();
                    foreach (FileInfo fi in fis)
                    {
                        size += fi.Length;
                    }
                    DirectoryInfo[] dis = d.GetDirectories();
                    foreach (DirectoryInfo di in dis)
                    {
                        size += await CalculateDirectorySize(di);
                    }
                }
                catch { }
                return size;
            });
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LoginManager.Instance.Logout();
            MainWindowViewModel.Instance.UserIcon = null;
            MainWindowViewModel.Instance.AppBarText = "Successfully logged out";
        }

        private void DownloadLimit_InputValidation(object sender, EventArgs e)
        {
            if (e.GetType() == typeof(TextCompositionEventArgs))
            {
                ((TextCompositionEventArgs)e).Handled = new Regex("[^0-9]+").IsMatch(((TextCompositionEventArgs)e).Text);
            }
            else if (e.GetType() == typeof(TextChangedEventArgs))
            {
                if (string.IsNullOrEmpty(((TextBox)sender).Text))
                {
                    ((TextBox)sender).Text = "0";
                }
                else if (!long.TryParse(((TextBox)sender).Text, out long result))
                {
                    ((TextBox)sender).Text = "0";
                }
            }
        }

        private void DownloadLimit_Save(object sender, EventArgs e)
        {
            if (e.GetType() == typeof(KeyEventArgs))
            {
                if (((KeyEventArgs)e).Key != Key.Enter)
                {
                    return;
                }
            }
            ViewModel.DownloadLimit = ViewModel.DownloadLimitUIValue;
            Preferences.Set(AppConfigKey.DownloadLimit, ViewModel.DownloadLimit, AppFilePath.UserFile);
            MainWindowViewModel.Instance.AppBarText = "Successfully saved download limit";
        }
    }
}
