using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
    /// Interaction logic for AdminConsoleUserControl.xaml
    /// </summary>
    public partial class AdminConsoleUserControl : UserControl
    {
        private AdminConsoleViewModel ViewModel { get; set; }
        private bool m_Loaded = false;

        public AdminConsoleUserControl()
        {
            InitializeComponent();
            ViewModel = new AdminConsoleViewModel();
            this.DataContext = ViewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!m_Loaded)
            {
                await InitUserList();
            }
            m_Loaded = !m_Loaded;
        }
        public async Task InitUserList()
        {
            try
            {
                ViewModel.Users = await Task<User[]>.Run(() =>
                {
                    string userList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/all");
                    return JsonSerializer.Deserialize<User[]>(userList);
                });
            }
            catch (WebException ex)
            {
                string msg = WebExceptionHelper.GetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = msg;
            }
        }

        private void PermissionRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                User selectedUser = (User)((FrameworkElement)sender).DataContext;
                if (e.RemovedItems.Count < 1 || ((PERMISSION_ROLE)e.RemovedItems[0] == (PERMISSION_ROLE)e.AddedItems[0]))
                {
                    return;
                }
                WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{selectedUser.ID}", JsonSerializer.Serialize(new User() { Role = selectedUser.Role }));
                MainWindowViewModel.Instance.AppBarText = $"Successfully updated permission role of user '{selectedUser.Username}' to '{selectedUser.Role}'";
            }
            catch (WebException ex)
            {
                string msg = WebExceptionHelper.GetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = msg;
            }
        }

        private void Activated_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                User selectedUser = (User)((FrameworkElement)sender).DataContext;
                WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{selectedUser.ID}", JsonSerializer.Serialize(new User() { Activated = selectedUser.Activated }));
                string state = selectedUser.Activated ? "activated" : "deactivated";
                MainWindowViewModel.Instance.AppBarText = $"Successfully {state} user '{selectedUser.Username}'";
            }
            catch (WebException ex)
            {
                string msg = WebExceptionHelper.GetServerMessage(ex);
                MainWindowViewModel.Instance.AppBarText = msg;
            }
        }

        private async void DeleteUser_Clicked(object sender, MouseButtonEventArgs e)
        {
            this.IsEnabled = false;
            User selectedUser = (User)((FrameworkElement)sender).DataContext;

            await Task.Run(async () =>
            {
                try
                {
                    if (selectedUser.DeletedAt == null)
                    {
                        WebHelper.Delete(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{selectedUser.ID}");
                        MainWindowViewModel.Instance.AppBarText = $"Successfully deleted user '{selectedUser.Username}'";
                        await InitUserList();
                    }
                    else
                    {
                        WebHelper.Post(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{selectedUser.ID}/recover", "");
                        MainWindowViewModel.Instance.AppBarText = $"Successfully recovered deleted user '{selectedUser.Username}'";
                        await InitUserList();
                    }
                    //ViewModel.UpdateUsersToUI();//Very dirty. Will be fixed
                }
                catch (WebException ex)
                {
                    string msg = WebExceptionHelper.GetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            });
            this.IsEnabled = true;
        }

        private void EditUser_Clicked(object sender, MouseButtonEventArgs e)
        {
            uiUserEditPopup.IsOpen = true;
            var obj = new UserEditUserControl((User)((FrameworkElement)sender).DataContext);
            obj.UserSaved += UserSaved;
            uiUserEditPopup.Child = obj;
        }
        protected async void UserSaved(object sender, EventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            this.IsEnabled = false;
            User selectedUser = (User)((Button)sender).DataContext;
            bool error = false;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{selectedUser.ID}", JsonSerializer.Serialize(selectedUser));
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
            ((Button)sender).IsEnabled = true;
            this.IsEnabled = true;
        }
        private async Task HandleChangesOnCurrentUser(User selectedUser)
        {
            if (LoginManager.Instance.GetCurrentUser().ID == selectedUser.ID)
            {
                await LoginManager.Instance.ManualLogin(selectedUser.Username, string.IsNullOrEmpty(selectedUser.Password) ? WebHelper.GetCredentials()[1] : selectedUser.Password);
                MainWindowViewModel.Instance.UserIcon = LoginManager.Instance.GetCurrentUser();
            }
            await InitUserList();
            await MainWindowViewModel.Instance.Community.InitUserList();
        }

        private void ShowUser_Click(object sender, MouseButtonEventArgs e)
        {
            User selectedUser = ((FrameworkElement)sender).DataContext as User;
            MainWindowViewModel.Instance.SetActiveControl(MainControl.Community);
            MainWindowViewModel.Instance.Community.ShowUser(selectedUser);
        }

        private async void Reindex_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/files/reindex", string.Empty);
                    MainWindowViewModel.Instance.AppBarText = "Sucessfully reindexed games";
                }
                catch (WebException ex)
                {
                    string msg = WebExceptionHelper.GetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            });
            ((Button)sender).IsEnabled = true;
        }
    }
}
