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
using AngleSharp.Common;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Markup;
using gamevault.Helper.Integrations;
using gamevault.Windows;
using gamevault.UserControls.SettingsComponents;

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
                Directory.Delete(LoginManager.Instance.GetUserProfile().ImageCacheDir, true);
                Directory.CreateDirectory(LoginManager.Instance.GetUserProfile().ImageCacheDir);
                ViewModel.ImageCacheSize = 0;
                MainWindowViewModel.Instance.AppBarText = "Image cache cleared";
            }
            catch
            {
                MainWindowViewModel.Instance.AppBarText = "Something went wrong while the image cache was cleared";
            }

        }
        private async void ClearOfflineCache_Clicked(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want delete the offline cache? \nThis can lead to games not being displayed correctly when you are offline.", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    if (File.Exists(LoginManager.Instance.GetUserProfile().IgnoreList))
                    {
                        File.Delete(LoginManager.Instance.GetUserProfile().IgnoreList);
                    }
                    if (File.Exists(LoginManager.Instance.GetUserProfile().OfflineCache))
                    {
                        File.Delete(LoginManager.Instance.GetUserProfile().OfflineCache);
                    }
                    ViewModel.OfflineCacheSize = 0;
                    MainWindowViewModel.Instance.AppBarText = "Offline cache cleared";
                }
                catch
                {
                    MainWindowViewModel.Instance.AppBarText = "Something went wrong while the offline cache was cleared";
                }
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
            LoadThemes();
            uiPwExtraction.Password = Preferences.Get(AppConfigKey.ExtractionPassword, LoginManager.Instance.GetUserProfile().UserConfigFile, true);
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
            if (((TabControl)sender).SelectedIndex == 3)
            {
                ViewModel.ImageCacheSize = await CalculateDirectorySize(new DirectoryInfo(LoginManager.Instance.GetUserProfile().ImageCacheDir));
                ViewModel.OfflineCacheSize = (File.Exists(LoginManager.Instance.GetUserProfile().OfflineCache) ? new FileInfo(LoginManager.Instance.GetUserProfile().OfflineCache).Length : 0);
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

        private void ChangeUserProfile_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            Preferences.DeleteKey(AppConfigKey.LastUserProfile, ProfileManager.ProfileConfigFile);
            ((MainWindow)App.Current.MainWindow).Dispose();
            App.Current.MainWindow = new LoginWindow(true);
            App.Current.MainWindow.Show();
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to log out?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    bool isLoggedInWithSSO = Preferences.Get(AppConfigKey.IsLoggedInWithSSO, LoginManager.Instance.GetUserProfile().UserConfigFile) == "1";
                    Preferences.DeleteKey(AppConfigKey.SessionToken, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    await WebHelper.PostAsync($"{SettingsViewModel.Instance.ServerUrl}/api/auth/revoke", "{" + $"\"refresh_token\": \"{WebHelper.GetRefreshToken()}\"" + "}");
                    if (!isLoggedInWithSSO)
                    {
                        Preferences.DeleteKey(AppConfigKey.Password, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    }
                    Preferences.DeleteKey(AppConfigKey.LastUserProfile, ProfileManager.ProfileConfigFile);
                    ((MainWindow)App.Current.MainWindow).Dispose();
                    App.Current.MainWindow = new LoginWindow(true);
                    App.Current.MainWindow.Show();
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
                }
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private async void LogoutFromAllDevices_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to log out from all devices?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    bool isLoggedInWithSSO = Preferences.Get(AppConfigKey.IsLoggedInWithSSO, LoginManager.Instance.GetUserProfile().UserConfigFile) == "1";
                    Preferences.DeleteKey(AppConfigKey.SessionToken, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    await WebHelper.PostAsync($"{SettingsViewModel.Instance.ServerUrl}/api/auth/revoke/all", "");
                    if (!isLoggedInWithSSO)
                    {
                        Preferences.DeleteKey(AppConfigKey.Password, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    }
                    Preferences.DeleteKey(AppConfigKey.LastUserProfile, ProfileManager.ProfileConfigFile);
                    ((MainWindow)App.Current.MainWindow).Dispose();
                    App.Current.MainWindow = new LoginWindow(true);
                    App.Current.MainWindow.Show();
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
                }
            }
            ((FrameworkElement)sender).IsEnabled = true;
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
            Preferences.Set(AppConfigKey.DownloadLimit, ViewModel.DownloadLimit, LoginManager.Instance.GetUserProfile().UserConfigFile);
            MainWindowViewModel.Instance.AppBarText = "Successfully saved download limit";
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (LoginManager.Instance.IsLoggedIn())
            {
                MainWindowViewModel.Instance.OpenPopup(new UserSettingsUserControl(LoginManager.Instance.GetCurrentUser()) { Width = 1200, Height = 800, Margin = new Thickness(50) });
            }
            else { MainWindowViewModel.Instance.AppBarText = "You are not logged in or offline"; }
        }
        private async void PhalcodeLoginLogout_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            if (string.IsNullOrEmpty(SettingsViewModel.Instance.License.UserName))
            {
                string phalcodeLoginMessage = await LoginManager.Instance.PhalcodeLogin();
                if (phalcodeLoginMessage != string.Empty)
                    MainWindowViewModel.Instance.AppBarText = phalcodeLoginMessage;
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
        private async void RefreshLicense_Click(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            await LoginManager.Instance.PhalcodeLogin(true);
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private void ManageBilling_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://phalco.de/account";
            if (SettingsViewModel.Instance.DevTargetPhalcodeTestBackend)
            {
                url = "https://test.phalco.de/account";
            }
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        private void ManagePhalcodeUser_Click(object sender, RoutedEventArgs e)
        {
            ManageBilling_Click(null, null);//Will maybe change in the Future
        }
        private void SubscribeGVPlus_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://phalco.de/products/gamevault-plus/checkout";
            if (SettingsViewModel.Instance.DevTargetPhalcodeTestBackend)
            {
                url = "https://test.phalco.de/products/gamevault-plus/checkout";
            }
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        #region THEMES
        private void Themes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ThemeItem selectedTheme = (ThemeItem)((ComboBox)sender).SelectedValue;
            if (((ComboBox)sender).SelectionBoxItem == string.Empty)
                return;

            if (selectedTheme == null)
                return;

            if (((ThemeItem)((ComboBox)sender).SelectionBoxItem).Path == selectedTheme.Path)
                return;

            ViewModel.IsCommunityThemeSelected = File.Exists(selectedTheme?.Path);
            if (selectedTheme.IsPlus == true && ViewModel.License.IsActive() == false)
            {
                ((ComboBox)sender).SelectedItem = (ThemeItem)((ComboBox)sender).SelectionBoxItem;
                try
                {
                    MainWindowViewModel.Instance.SetActiveControl(MainControl.Settings);
                    MainWindowViewModel.Instance.Settings.SetTabIndex(4);
                    MainWindowViewModel.Instance.AppBarText = "Oops! You just reached a premium feature of GameVault - Upgrade now and support the devs!";
                }
                catch { }
                return;
            }
            try
            {
                App.Instance.SetTheme(selectedTheme.Path);
                Preferences.Set(AppConfigKey.Theme, JsonSerializer.Serialize(selectedTheme), LoginManager.Instance.GetUserProfile().UserConfigFile, true);
            }
            catch (Exception ex) { MainWindowViewModel.Instance.AppBarText = ex.Message; }
        }
        private void LoadThemes()
        {
            try
            {
                if (ViewModel.Themes == null)
                {
                    ViewModel.Themes = new ObservableCollection<ThemeItem>();
                }
                else
                {
                    ViewModel.Themes.Clear();
                }
                //Load embedded Themes
                ResourceDictionary res = new ResourceDictionary();
                res.Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeDefaultDark.xaml");
                ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = false, Path = res.Source.OriginalString });

                res.Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeDefaultLight.xaml");
                ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = false, Path = res.Source.OriginalString });

                res.Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeClassicDark.xaml");
                ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = false, Path = res.Source.OriginalString });

                res.Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemePhalcodeDark.xaml");
                ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = true, Path = res.Source.OriginalString });

                res.Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemePhalcodeLight.xaml");
                ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = true, Path = res.Source.OriginalString });

                res.Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeHalloweenDark.xaml");
                ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = true, Path = res.Source.OriginalString });

                res.Source = new Uri("pack://application:,,,/gamevault;component/Resources/Assets/Themes/ThemeChristmasDark.xaml");
                ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = true, Path = res.Source.OriginalString });

                if (Directory.Exists(LoginManager.Instance.GetUserProfile().ThemesLoadDir))
                {
                    foreach (var file in Directory.GetFiles(LoginManager.Instance.GetUserProfile().ThemesLoadDir, "*.xaml", SearchOption.AllDirectories))
                    {
                        try
                        {
                            res.Source = new Uri(file);
                            ViewModel.Themes.Add(new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], IsPlus = true, Path = res.Source.OriginalString });
                        }
                        catch { }
                    }
                }
                string currentThemeString = Preferences.Get(AppConfigKey.Theme, LoginManager.Instance.GetUserProfile().UserConfigFile, true);
                ThemeItem currentTheme = JsonSerializer.Deserialize<ThemeItem>(currentThemeString);
                int themeIndex = ViewModel.Themes.ToList().FindIndex(i => i.Path == currentTheme.Path);
                if (themeIndex != -1 && (ViewModel.Themes[themeIndex].IsPlus == true ? ViewModel.License.IsActive() : true))
                {
                    uiCbTheme.SelectedIndex = themeIndex;
                }
                else
                {
                    uiCbTheme.SelectedIndex = 0;
                }
                ViewModel.IsCommunityThemeSelected = File.Exists(((ThemeItem)uiCbTheme.SelectedItem)?.Path);
            }
            catch { uiCbTheme.SelectedIndex = 0; }
        }

        private void OpenThemeFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(LoginManager.Instance.GetUserProfile().ThemesLoadDir))
                {
                    Directory.CreateDirectory(LoginManager.Instance.GetUserProfile().ThemesLoadDir);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{LoginManager.Instance.GetUserProfile().ThemesLoadDir}\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }
        private void OpenCommunityThemeRepository_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/Phalcode/gamevault-community-themes") { UseShellExecute = true });
            }
            catch { }
        }
        private async void ReloadThemeList_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            LoadThemes();
            await Task.Delay(500);
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private async Task<List<JsonElement>> LoadCommunityThemesHeader()
        {
            string jsonResponse = await WebHelper.BaseGetAsync("https://api.github.com/repos/phalcode/gamevault-community-themes/contents/v1");
            return JsonSerializer.Deserialize<List<JsonElement>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        private async Task<ThemeItem> LoadThemeItemFromUrl(string url)
        {
            try
            {
                string result = await WebHelper.BaseGetAsync(url);
                ResourceDictionary res = (ResourceDictionary)XamlReader.Parse(result);
                return new ThemeItem() { DisplayName = (string)res["Theme.DisplayName"], Description = (string)res["Theme.Description"], Author = (string)res["Theme.Author"], Path = url };
            }
            catch { return null; }
        }
        private async Task LoadCommunityThemes()
        {
            try
            {
                List<JsonElement> fetchedList = await LoadCommunityThemesHeader();
                foreach (var entry in fetchedList)
                {
                    if (entry.TryGetProperty("name", out JsonElement nameElement) && nameElement.GetString()?.First() != '_' && entry.TryGetProperty("download_url", out JsonElement downloadUrlElement))
                    {
                        ThemeItem theme = await LoadThemeItemFromUrl(downloadUrlElement.GetString()!);
                        if (theme != null)
                        {
                            ViewModel.CommunityThemes.Add(theme);
                        }
                    }
                }
            }
            catch { }
        }
        private async void CommunityThemes_DropDownOpened(object sender, EventArgs e)
        {
            if (ViewModel.CommunityThemes == null)
            {
                ViewModel.CommunityThemes = new ObservableCollection<ThemeItem>();
                await LoadCommunityThemes();
            }
        }
        private async void ReloadCommunityThemeList_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CommunityThemes == null)
            {
                ViewModel.CommunityThemes = new ObservableCollection<ThemeItem>();
            }
            else
            {
                ViewModel.CommunityThemes.Clear();
            }
            ((FrameworkElement)sender).IsEnabled = false;
            await LoadCommunityThemes();
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private async void InstallCommunityTheme_Click(object sender, RoutedEventArgs e)
        {
            if (uiCBCommunityThemes.SelectedItem == null)
            {
                MainWindowViewModel.Instance.AppBarText = "No Theme selected";
                return;
            }
            ((FrameworkElement)sender).IsEnabled = false;
            try
            {
                ThemeItem theme = (ThemeItem)uiCBCommunityThemes.SelectedItem;
                string installationPath = Path.Combine(LoginManager.Instance.GetUserProfile().ThemesLoadDir, theme.DisplayName + ".xaml");
                string result = await WebHelper.BaseGetAsync(theme.Path);
                File.WriteAllText(installationPath, result);
                LoadThemes();
                try
                {
                    int installedThemeIndex = ViewModel.Themes.IndexOf(ViewModel.Themes.First(t => t.DisplayName == theme.DisplayName));
                    uiCbTheme.SelectedIndex = installedThemeIndex;
                }
                catch { }
                MainWindowViewModel.Instance.AppBarText = $"Successfully installed {theme.DisplayName}";
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
                ((FrameworkElement)sender).IsEnabled = true;
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private void UninstallTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(((ThemeItem)uiCbTheme.SelectedItem)?.Path))
                {
                    File.Delete(((ThemeItem)uiCbTheme.SelectedItem).Path);
                    uiCbTheme.SelectedIndex = 0;
                    LoadThemes();
                }
            }
            catch { }
        }
        #endregion
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch { }
        }

        private void Awesome_Click(object sender, MouseButtonEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            imgAwesome.Data = "https://phalco.de/images/gamevault/eastereggs/awesome.gif";
            AnalyticsHelper.Instance.SendCustomEvent(CustomAnalyticsEventKeys.EASTER_EGG, new { name = "awesome" });
        }

        private void ExtractionPasswordSave_Click(object sender, RoutedEventArgs e)
        {
            Preferences.Set(AppConfigKey.ExtractionPassword, uiPwExtraction.Password, LoginManager.Instance.GetUserProfile().UserConfigFile, true);
            MainWindowViewModel.Instance.AppBarText = "Successfully saved extraction password";
        }
        private async void IgnoredExecutablesReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(LoginManager.Instance.GetUserProfile().IgnoreList))
                    File.Delete(LoginManager.Instance.GetUserProfile().IgnoreList);

                await SettingsViewModel.Instance.InitIgnoreList();
            }
            catch { }
        }
        private void IgnoredExecutablesSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preferences.Set("IL", SettingsViewModel.Instance.IgnoreList, LoginManager.Instance.GetUserProfile().IgnoreList);
            }
            catch { }
        }

        private async void SyncSteamShortcuts_Toggled(object sender, RoutedEventArgs e)
        {
            if (!this.loaded)//Make sure the toggle came from the ui
                return;

            if (((ToggleSwitch)sender).IsOn)
            {
                await SteamHelper.SyncGamesWithSteamShortcuts(InstallViewModel.Instance.InstalledGames.ToDictionary(pair => pair.Key, pair => pair.Value));
            }
            else
            {
                SteamHelper.RemoveGameVaultGamesFromSteamShortcuts();
            }
        }

        private async void RestoreSteamShortcutBackup_Click(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to restore the backup? Your current shortcuts will be reset to the state when the backup was created. This can lead to some shortcuts being lost.", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
            if (result == MessageDialogResult.Affirmative)
            {
                SteamHelper.RestoreBackup();
            }
        }
        private int devModeCount = 0;
        private void DevMode_Click(object sender, MouseButtonEventArgs e)
        {
            devModeCount++;
            if (devModeCount == 5)
            {
                ViewModel.DevModeEnabled = true;
            }
        }
        private void RemoveCustomCloudSaveManifest_Click(object sender, RoutedEventArgs e)
        {
            int index = ViewModel.CustomCloudSaveManifests.IndexOf(((DirectoryEntry)((FrameworkElement)sender).DataContext));
            if (index >= 0)
            {
                ViewModel.CustomCloudSaveManifests.RemoveAt(index);
            }
        }

        private void AddCustomCloudSaveManifest_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CustomCloudSaveManifests.Add(new DirectoryEntry());
        }

        private void SaveCustomCloudSaveManifests_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = string.Join(";", ViewModel.CustomCloudSaveManifests.Where(entry => !string.IsNullOrWhiteSpace(entry.Uri)).Select(entry => entry.Uri));
                Preferences.Set(AppConfigKey.CustomCloudSaveManifests, result, LoginManager.Instance.GetUserProfile().UserConfigFile);
            }
            catch { }
            MainWindowViewModel.Instance.AppBarText = "Successfully saved custom Ludusavi Manifests";
        }
        private async void AddRootDirectory_Click(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).IsEnabled = false;
            try
            {
                string selectedDirectory = await SettingsViewModel.Instance.SelectDownloadPath();
                if (Directory.Exists(selectedDirectory))
                {
                    ViewModel.RootDirectories.Add(new DirectoryEntry() { Uri = selectedDirectory });
                    string result = string.Join(";", ViewModel.RootDirectories.Select(entry => entry.Uri));
                    Preferences.Set(AppConfigKey.RootDirectories, result, LoginManager.Instance.GetUserProfile().UserConfigFile);
                    await MainWindowViewModel.Instance.Library.GetGameInstalls().RestoreInstalledGames();
                    await MainWindowViewModel.Instance.Downloads.RestoreDownloadedGames();
                    if (InstallViewModel.Instance.InstalledGamesDuplicates.Any())
                    {
                        await ShowInstalledGameDuplicates();
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private async Task ShowInstalledGameDuplicates()
        {
            string duplicateMessage = "";
            foreach (var duplicate in InstallViewModel.Instance.InstalledGamesDuplicates)
            {
                var matchingGame = InstallViewModel.Instance.InstalledGames?.FirstOrDefault(game => game.Key.ID == duplicate.Key);
                if (string.IsNullOrEmpty(matchingGame?.Key?.Title))
                    continue;

                duplicateMessage += $"\n\n'{matchingGame?.Key?.Title}' is already installed at:\n{duplicate.Value}";
            }
            await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync("Duplicate game installation detected", duplicateMessage, MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "Ok", DialogTitleFontSize = 20, AnimateHide = false });
        }
        private async void RemoveRootDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = ViewModel.RootDirectories.IndexOf(((DirectoryEntry)((FrameworkElement)sender).DataContext));
                if (index >= 0)
                {
                    ViewModel.RootDirectories.RemoveAt(index);
                    string result = string.Join(";", ViewModel.RootDirectories.Select(entry => entry.Uri));
                    Preferences.Set(AppConfigKey.RootDirectories, result, LoginManager.Instance.GetUserProfile().UserConfigFile);

                    ((FrameworkElement)sender).IsEnabled = false;//Disable the add button to block async restoring installed games
                    await MainWindowViewModel.Instance.Library.GetGameInstalls().RestoreInstalledGames();
                    await MainWindowViewModel.Instance.Downloads.RestoreDownloadedGames();
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private void OpenUserCacheFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(LoginManager.Instance.GetUserProfile().RootDir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{LoginManager.Instance.GetUserProfile().RootDir}\"",
                    UseShellExecute = true
                });
            }
        }
    }
}