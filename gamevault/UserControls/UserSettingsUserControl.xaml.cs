using gamevault.Helper;
using gamevault.Models;
using gamevault.Models.Mapping;
using gamevault.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.VoiceCommands;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for UserSettingsUserControl.xaml
    /// </summary>
    public partial class UserSettingsUserControl : UserControl
    {
        private UserSettingsViewModel ViewModel { get; set; }
        private bool loaded = false;
        internal UserSettingsUserControl(User user)
        {
            ViewModel = new UserSettingsViewModel();
            ViewModel.OriginUser = user;
            ConvertToUpdateUser();
            InitializeComponent();
            this.DataContext = ViewModel;
        }
        private void ConvertToUpdateUser()
        {
            UpdateUserDto newUpdateUser = new UpdateUserDto();
            newUpdateUser.Username = ViewModel.OriginUser.Username;
            newUpdateUser.FirstName = ViewModel.OriginUser.FirstName;
            newUpdateUser.LastName = ViewModel.OriginUser.LastName;
            newUpdateUser.EMail = ViewModel.OriginUser.EMail;
            newUpdateUser.BirthDate = ViewModel.OriginUser.BirthDate;
            ViewModel.UpdateUser = newUpdateUser;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            loaded = true;
        }

        private void CommandHelper_Executed(object sender, object e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.ClosePopup();
        }

        private void SettingsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uiSettingsContent.SelectedIndex = ((TabControl)sender).SelectedIndex;
        }
        #region Edit Image
        private async void Image_Drop(object sender, DragEventArgs e)
        {
            string tag = ((FrameworkElement)sender).Tag as string;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (tag == "avatar")
                    {
                        ViewModel.AvatarImageUrl = files[0];
                    }
                    else
                    {
                        ViewModel.BackgroundImageSource = BitmapHelper.GetBitmapImage(files[0]);
                    }
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Html))
            {
                string html = (string)e.Data.GetData(DataFormats.Html);
                string imagePath = ExtractImageUrlFromHtml(html);

                if (!string.IsNullOrEmpty(imagePath))
                {
                    try
                    {
                        if (tag == "avatar")
                        {
                            ViewModel.AvatarImageUrl = imagePath;
                        }
                        else
                        {
                            BitmapImage bitmap = await BitmapHelper.GetBitmapImageAsync(imagePath);
                            ViewModel.BackgroundImageSource = bitmap;
                        }
                    }
                    catch
                    {
                        MainWindowViewModel.Instance.AppBarText = "Failed to download image";
                    }
                }
            }
        }
        private string ExtractImageUrlFromHtml(string html)
        {
            Regex regex = new Regex("<img[^>]+?src\\s*=\\s*['\"]([^'\"]+)['\"][^>]*>");
            Match match = regex.Match(html);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return string.Empty;
        }
        private void ChooseImage(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string tag = ((FrameworkElement)sender).Tag as string;
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK && File.Exists(dialog.FileName))
                    {
                        if (tag == "avatar")
                        {
                            ViewModel.AvatarImageUrl = dialog.FileName;
                        }
                        else
                        {
                            ViewModel.BackgroundImageSource = BitmapHelper.GetBitmapImage(dialog.FileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }

        private void Image_Paste(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.V)
                {
                    try
                    {
                        if (Clipboard.ContainsImage())
                        {

                            if (((FrameworkElement)sender).Tag != null && ((FrameworkElement)sender).Tag.ToString() == "avatar")
                            {
                                //ViewModel.AvatarImageUrl = Clipboard.GetImage();
                            }
                            else
                            {
                                ViewModel.BackgroundImageSource = Clipboard.GetImage();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = ex.Message;
                    }
                }
            }
        }

        private async void LoadImageUrl(string url, string tag)
        {
            try
            {
                if (tag == "avatar")
                {
                    ViewModel.AvatarImageUrl = url;
                }
                else
                {
                    ViewModel.BackgroundImageSource = await BitmapHelper.GetBitmapImageAsync(url);
                }
            }
            catch (Exception ex)
            {
                if (url != string.Empty)
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }
        #region Generic Events
        private InputTimer backgroundImageUrldebounceTimer { get; set; }
        private InputTimer avatarImageUrldebounceTimer { get; set; }
        private void InitImageUrlTimer()
        {
            if (backgroundImageUrldebounceTimer == null)
            {
                backgroundImageUrldebounceTimer = new InputTimer() { Data = string.Empty };
                backgroundImageUrldebounceTimer.Interval = TimeSpan.FromMilliseconds(400);
                backgroundImageUrldebounceTimer.Tick += BackgroundImageDebounceTimerElapsed;
            }
            if (avatarImageUrldebounceTimer == null)
            {
                avatarImageUrldebounceTimer = new InputTimer() { Data = string.Empty };
                avatarImageUrldebounceTimer.Interval = TimeSpan.FromMilliseconds(400);
                avatarImageUrldebounceTimer.Tick += AvatarImageDebounceTimerElapsed;
            }
        }
        private void BackgoundImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            InitImageUrlTimer();
            backgroundImageUrldebounceTimer.Stop();
            backgroundImageUrldebounceTimer.Data = ((TextBox)sender).Text;
            backgroundImageUrldebounceTimer.Start();
        }
        private void AvatarImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            InitImageUrlTimer();
            avatarImageUrldebounceTimer.Stop();
            avatarImageUrldebounceTimer.Data = ((TextBox)sender).Text;
            avatarImageUrldebounceTimer.Start();
        }
        private void BackgroundImageDebounceTimerElapsed(object? sender, EventArgs e)
        {
            backgroundImageUrldebounceTimer.Stop();
            LoadImageUrl(backgroundImageUrldebounceTimer.Data, "");
        }
        private void AvatarImageDebounceTimerElapsed(object? sender, EventArgs e)
        {
            avatarImageUrldebounceTimer.Stop();
            LoadImageUrl(avatarImageUrldebounceTimer.Data, "avatar");
        }
        #endregion
        private async void BackgroundImage_Save(object sender, RoutedEventArgs e)
        {
            await SaveImage("");
        }
        private async void AvatarImage_Save(object sender, RoutedEventArgs e)
        {
            await SaveImage("avatar");
        }
        private async Task SaveImage(string tag)
        {
            bool success = false;
            try
            {
                MemoryStream ms = null;
                string filename = "x.jpg";
                if (tag == "avatar")
                {
                    ViewModel.AvatarImageChanged = false;
                    string avatarImageUrl = ViewModel.AvatarImageUrl.Replace("&amp;", "&");
                    if (System.Uri.IsWellFormedUriString(avatarImageUrl, UriKind.Absolute))
                    {
                        ms = await BitmapHelper.UrlToMemoryStream(avatarImageUrl);
                    }
                    else
                    {
                        ms = BitmapHelper.UriToMemoryStream(avatarImageUrl);
                    }
                    if (GifHelper.IsGif(ms))
                    {
                        if (!SettingsViewModel.Instance.License.IsActive())
                        {
                            try
                            {
                                MainWindowViewModel.Instance.SetActiveControl(MainControl.Settings);
                                MainWindowViewModel.Instance.Settings.SetTabIndex(4);
                                MainWindowViewModel.Instance.AppBarText = "Oops! You just reached a premium feature of GameVault - Upgrade now and support the devs!";
                            }
                            catch { }
                            return;
                        }
                        filename = "x.gif";
                    }
                }
                else
                {
                    ViewModel.BackgroundImageChanged = false;
                    ms = BitmapHelper.BitmapSourceToMemoryStream((BitmapSource)ViewModel.BackgroundImageSource);
                }
                ms.Position = 0;
                string resp = await WebHelper.UploadFileAsync($"{SettingsViewModel.Instance.ServerUrl}/api/media", ms, filename, null);
                ms.Dispose();
                var newImageId = JsonSerializer.Deserialize<Media>(resp).ID;
                try
                {
                    UpdateUserDto updateObject = new UpdateUserDto();
                    if (tag == "avatar")
                    {
                        updateObject.AvatarId = newImageId;
                    }
                    else
                    {
                        updateObject.BackgroundId = newImageId;
                    }
                    string url = $"{SettingsViewModel.Instance.ServerUrl}/api/users/{ViewModel.OriginUser.ID}";
                    if (LoginManager.Instance.GetCurrentUser().ID == ViewModel.OriginUser.ID)
                    {
                        url = @$"{SettingsViewModel.Instance.ServerUrl}/api/users/me";
                    }
                    string updatedUser = await WebHelper.PutAsync(url, JsonSerializer.Serialize(updateObject));
                    ViewModel.OriginUser = JsonSerializer.Deserialize<User>(updatedUser);
                    success = true;
                    MainWindowViewModel.Instance.AppBarText = "Successfully updated image";
                }
                catch (Exception ex)
                {
                    string msg = WebExceptionHelper.TryGetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
                //Update Data Context for Community Page. So that the images are also refreshed there directly
                if (success)
                {
                    await MainWindowViewModel.Instance.AdminConsole.InitUserList();
                    await MainWindowViewModel.Instance.Community.InitUserList();
                    if (LoginManager.Instance.GetCurrentUser().ID == ViewModel.OriginUser.ID)
                    {
                        MainWindowViewModel.Instance.UserAvatar = ViewModel.OriginUser;
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = WebExceptionHelper.TryGetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = msg;
            }
        }
        #endregion

        private void UserDetails_TextChanged(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                ViewModel.UserDetailsChanged = true;
            }
        }

        private async void SaveUserDetails_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UserDetailsChanged = false;
            this.IsEnabled = false;

            UpdateUserDto selectedUser = ViewModel.UpdateUser;
            string newPassword = uiUserPassword.Password;

            if (newPassword != "")
                selectedUser.Password = newPassword;

            if (selectedUser.BirthDate == ViewModel.OriginUser.BirthDate)//Set birthday to null, so a underage user can edit the rest of its data
            {
                selectedUser.BirthDate = null;
            }

            bool error = false;
            try
            {
                string url = $"{SettingsViewModel.Instance.ServerUrl}/api/users/{ViewModel.OriginUser.ID}";
                if (LoginManager.Instance.GetCurrentUser().ID == ViewModel.OriginUser.ID)
                {
                    url = @$"{SettingsViewModel.Instance.ServerUrl}/api/users/me";
                }
                string result = await WebHelper.PutAsync(url, JsonSerializer.Serialize(selectedUser));
                ViewModel.OriginUser = JsonSerializer.Deserialize<User>(result);
                if (LoginManager.Instance.GetCurrentUser().ID == ViewModel.OriginUser.ID)
                {
                    WebHelper.OverrideCredentials(selectedUser.Username, selectedUser.Password);
                }
                MainWindowViewModel.Instance.AppBarText = "Successfully saved user changes";
            }
            catch (Exception ex)
            {
                ConvertToUpdateUser();//Reset to Origin User
                error = true;
                string msg = WebExceptionHelper.TryGetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = msg;
            }
            if (!error)
            {
                try
                {
                    ViewModel.OriginUser.Password = newPassword;
                    await HandleChangesOnCurrentUser(ViewModel.OriginUser);
                }
                catch (Exception ex)
                {
                    string msg = WebExceptionHelper.TryGetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            }
            this.IsEnabled = true;
        }
        private async Task HandleChangesOnCurrentUser(User selectedUser)
        {
            if (LoginManager.Instance.GetCurrentUser().ID == selectedUser.ID)
            {
                UserProfile profile = LoginManager.Instance.GetUserProfile();
                bool isLoggedInWithSSO = Preferences.Get(AppConfigKey.IsLoggedInWithSSO, profile.UserConfigFile) == "1";
                if (isLoggedInWithSSO)
                {
                    await LoginManager.Instance.SSOLogin(profile);
                }
                else
                {
                    await LoginManager.Instance.Login(profile, WebHelper.GetCredentials()[0], WebHelper.GetCredentials()[1]);
                }
                MainWindowViewModel.Instance.UserAvatar = LoginManager.Instance.GetCurrentUser();
            }

            await MainWindowViewModel.Instance.AdminConsole.InitUserList();
            await MainWindowViewModel.Instance.Community.InitUserList();

        }
        private void CopyUserApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ViewModel.OriginUser.ApiKey);
                MainWindowViewModel.Instance.AppBarText = "Copied API Key to Clipboard";
            }
            catch { }
        }
        private void Help_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string url = "";
                switch (uiSettingsContent.SelectedIndex)
                {
                    case 0:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui/#edit-user-images";
                            break;
                        }
                    case 1:
                        {
                            url = "https://gamevau.lt/docs/client-docs/gui#edit-details";
                            break;
                        }
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = ex.Message;
            }
        }        
    }
}

