using gamevault.Models;
using gamevault.ViewModels;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System;
using gamevault.Helper;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Diagnostics;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.Text.Json;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for SettingsUserControl.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl
    {
        private SettingsViewModel ViewModel { get; set; }
        private bool loaded = false;
        public SettingsUserControl()
        {
            InitializeComponent();
            ViewModel = SettingsViewModel.Instance;
            this.DataContext = ViewModel;
        }
        public void SetTabIndex(int index)
        {
            uiTabControl.SelectedIndex = index;
        }
        private void ClearImageCache_Clicked(object sender, RoutedEventArgs e)
        {

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

        }
        private void ClearOfflineCache_Clicked(object sender, RoutedEventArgs e)
        {

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

        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;

            loaded = true;
            if (App.IsWindowsPackage)
            {
                uiAutostartToggle.IsOn = await AutostartHelper.IsWindowsPackageAutostartEnabled();
            }
            else
            {
                uiAutostartToggle.IsOn = AutostartHelper.RegistryAutoStartKeyExists();
            }
            uiAutostartToggle.Toggled += AppAutostart_Toggled;
            await LoadThemes();
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

        private void DownloadLimit_Save(object sender, RoutedEventArgs e)
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

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (LoginManager.Instance.IsLoggedIn())
            {
                MainWindowViewModel.Instance.OpenPopup(new UserSettingsUserControl(LoginManager.Instance.GetCurrentUser()) { Width = 1200, Height = 800, Margin = new Thickness(50) });
            }
            else { MainWindowViewModel.Instance.AppBarText = "You are not logged in"; }
        }
        private async void PhalcodeLoginLogout_Click(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            if (string.IsNullOrEmpty(SettingsViewModel.Instance.License.UserName))
            {
                await LoginManager.Instance.PhalcodeLogin();
            }
            else
            {
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to log out of your Phalcode account? GameVault Plus features will no longer be usable.", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    LoginManager.Instance.PhalcodeLogout();
                }
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }

        private void ManageBilling_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            string url = "https://test.phalco.de/account";
#else
            string url = "https://phalco.de/account";
#endif

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            e.Handled = true;
        }
        private void Themes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ThemeItem selectedTheme = (ThemeItem)((ComboBox)sender).SelectedValue;
            if (((ComboBox)sender).SelectionBoxItem == string.Empty)
                return;
            if (((ThemeItem)((ComboBox)sender).SelectionBoxItem).Value == selectedTheme.Value)
                return;
            if (selectedTheme.IsPlus == true && ViewModel.License.IsActive() == false)
            {
                ((ComboBox)sender).SelectedItem = (ThemeItem)((ComboBox)sender).SelectionBoxItem;
                try
                {
#if DEBUG
                    string url = "https://test.phalco.de/products/gamevault-plus/checkout?hit_paywall=true";
#else
                    string url = "https://phalco.de/products/gamevault-plus/checkout?hit_paywall=true";
#endif

                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { }
                return;
            }
            try
            {
                App.Current.Resources.MergedDictionaries[0] = new ResourceDictionary() { Source = new Uri(selectedTheme.Value) };
                //Reload Base Styles to apply new colors
                App.Current.Resources.MergedDictionaries[3] = new ResourceDictionary() { Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Base.xaml") };
                Preferences.Set(AppConfigKey.Theme, JsonSerializer.Serialize(selectedTheme), AppFilePath.UserFile, true);
            }
            catch (Exception ex) { MainWindowViewModel.Instance.AppBarText = ex.Message; }
        }
        private async Task LoadThemes()
        {
            try
            {
                ViewModel.Themes = new System.Collections.Generic.List<ThemeItem> {
               new ThemeItem() { Key = "GameVault (Dark)", Value = "pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeGameVaultDark.xaml", IsPlus = false },
               new  ThemeItem(){ Key="GameVault (Light)",Value="pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeGameVaultLight.xaml",IsPlus=false},
               new  ThemeItem(){ Key="GameVault (Classic)",Value="pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeGameVaultClassicDark.xaml",IsPlus=false},
               new  ThemeItem(){ Key="Phalcode (Dark)",Value="pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemePhalcodeDark.xaml",IsPlus=true},
               new  ThemeItem(){ Key="Phalcode (Light)",Value="pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemePhalcodeLight.xaml",IsPlus=true}};
                if (Directory.Exists(AppFilePath.ThemesLoadDir))
                {
                    foreach (var file in Directory.GetFiles(AppFilePath.ThemesLoadDir, "*.xaml", SearchOption.AllDirectories))
                    {
                        ViewModel.Themes.Add(new ThemeItem() { Key = Path.GetFileNameWithoutExtension(file), Value = file, IsPlus = true });
                    }
                }
                string currentThemeString = Preferences.Get(AppConfigKey.Theme, AppFilePath.UserFile, true);
                ThemeItem currentTheme = JsonSerializer.Deserialize<ThemeItem>(currentThemeString);
                int themeIndex = ViewModel.Themes.FindIndex(i => i.Value == currentTheme.Value);
                if (themeIndex != -1 && (ViewModel.Themes[themeIndex].IsPlus == true ? ViewModel.License.IsActive() : true))
                {
                    uiCbTheme.SelectedIndex = themeIndex;
                }
                else
                {
                    uiCbTheme.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void OpenThemeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(AppFilePath.ThemesLoadDir))
            {
                Process.Start("explorer.exe", AppFilePath.ThemesLoadDir);
            }
        }
    }
}
