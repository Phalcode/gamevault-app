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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Net.Http;
using System.Text;
using Windows.ApplicationModel.Contacts;
using Newtonsoft.Json;
using Windows.ApplicationModel.Contacts.DataProvider;


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
            ShowListedDrives();
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
            SendDataRequest();
        }

        private void SendDataRequest()
        {
            var Client = new HttpClient();
            string UserEmail = Preferences.Get(AppConfigKey.Email, AppFilePath.UserFile);
            string UserName = Preferences.Get(AppConfigKey.Username, AppFilePath.UserFile);

            var WebHookContent = new
            {
                username = "Bug Reporter",
                content = string.Format("**Bug Report**:\n\n**Username:** {0}\n\n**Email:** {1}\n\n**Description:** UserData Request", UserName, UserEmail),
                avatar_url = "https://arparec.dev/assets/ico.png",
            };

            string EndPoint = "https://discord.com/api/webhooks/1184586253849591950/tDeMc5pjua-qvxDkAB6IKa4pr0qlGD61UkghPlteuGR94YwE0wYWEeGNW-5cdnUGo-uG";

            var content = new StringContent(JsonConvert.SerializeObject(WebHookContent), Encoding.UTF8, "application/json");

            Client.PostAsync(EndPoint, content).Wait();
        }

        private Array ListDrivesInSystem()
        {
            return System.IO.DriveInfo.GetDrives();
        }

        private void ShowListedDrives()
        {
            uiDriveListBox.ItemsSource = ListDrivesInSystem();
        }

        private void SetStorageDrive(object sender, RoutedEventArgs e)
        {
            Preferences.Set(AppConfigKey.InstallDrive, uiDriveListBox.SelectedItem.ToString().Replace(@":\", ""), AppFilePath.UserFile);
            Preferences.Set(AppConfigKey.RootPath, Preferences.Get(AppConfigKey.InstallDrive, AppFilePath.UserFile) + @":\NeoGameLibrary\", AppFilePath.UserFile);
        }
    }
}
