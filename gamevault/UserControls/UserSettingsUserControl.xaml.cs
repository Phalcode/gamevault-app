using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            ViewModel.User = user;
            InitializeComponent();
            this.DataContext = ViewModel;
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
        #region Image Change
        private void BackgroundImage_Drop(object sender, DragEventArgs e)
        {

        }

        private void BackgroundImage_ChooseImage(object sender, MouseButtonEventArgs e)
        {

        }

        private void Image_Paste(object sender, KeyEventArgs e)
        {

        }

        private void BackgroundImage_Save(object sender, MouseButtonEventArgs e)
        {

        }
        private void AvatarImage_Save(object sender, MouseButtonEventArgs e)
        {

        }
        private void AvatarImage_Drop(object sender, DragEventArgs e)
        {

        }

        private void AvatarImage_ChooseImage(object sender, MouseButtonEventArgs e)
        {

        }
        private void BackgoundImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void AvatarImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        #endregion

        private void UserDetails_TextChanged(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                ViewModel.UserDetailsChanged = true;
            }
        }

        private async void SaveUserDetails_Click(object sender, MouseButtonEventArgs e)
        {
            ViewModel.UserDetailsChanged = false;
            this.IsEnabled = false;

            User selectedUser = ViewModel.User;
            string newPassword = uiUserPassword.Password;

            if (newPassword != "")
                selectedUser.Password = newPassword;

            bool error = false;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/{selectedUser.ID}", JsonSerializer.Serialize(selectedUser));
                    MainWindowViewModel.Instance.AppBarText = "Sucessfully saved user changes";
                }
                catch (WebException ex)
                {
                    error = true;
                    string msg = WebExceptionHelper.GetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            });
            if (!error)
            {
                await HandleChangesOnCurrentUser(selectedUser);
            }
            this.IsEnabled = true;
        }
        private async Task HandleChangesOnCurrentUser(User selectedUser)
        {
            if (LoginManager.Instance.GetCurrentUser().ID == selectedUser.ID)
            {
                await LoginManager.Instance.ManualLogin(selectedUser.Username, string.IsNullOrEmpty(selectedUser.Password) ? WebHelper.GetCredentials()[1] : selectedUser.Password);
                MainWindowViewModel.Instance.UserIcon = LoginManager.Instance.GetCurrentUser();
            }
            await MainWindowViewModel.Instance.AdminConsole.InitUserList();
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
                            url = "https://gamevau.lt/docs/client-docs/gui#edit-images-1";
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

