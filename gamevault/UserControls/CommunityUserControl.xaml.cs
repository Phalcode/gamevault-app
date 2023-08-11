using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;


namespace gamevault.UserControls
{
    public partial class CommunityUserControl : UserControl
    {
        private CommunityViewModel ViewModel { get; set; }
        public CommunityUserControl()
        {
            InitializeComponent();
            ViewModel = new CommunityViewModel();
            this.DataContext = ViewModel;
        }
        public async Task InitUserList()
        {
            ViewModel.Users = await Task<User[]>.Run(() =>
            {
                string userList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users");
                var users = JsonSerializer.Deserialize<User[]>(userList);
                users = BringCurrentUserToTop(users);
                return users;
            });
        }

        private async void Users_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                uiProgressScrollView.ScrollToTop();
                int selectedUserId = -1;
                if (e.AddedItems.Count == 0)
                {
                    uiSelectUser.SelectedIndex = 0;
                    return;
                }
                else
                {
                    selectedUserId = ((User)e.AddedItems[0]).ID;
                }
                ViewModel.CurrentShownUser = await Task<User>.Run(() =>
                {
                    string currentShownUser = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{selectedUserId}");
                    return JsonSerializer.Deserialize<User>(currentShownUser);
                });
                if (uiSortBy.SelectedIndex == 2)
                {
                    SortBy_SelectionChanged(null, new SelectionChangedEventArgs(Selector.SelectionChangedEvent, new string[0], new string[] { "Last played" }));
                }
                else
                {
                    uiSortBy.SelectedIndex = 2;
                }
            }
            catch (Exception ex) { MainWindowViewModel.Instance.AppBarText = "Could not connect to server"; }
        }
        private User[] BringCurrentUserToTop(User[] users)
        {
            int currentUserId = LoginManager.Instance.GetCurrentUser().ID;
            for (int i = 0; i < users.Length; i++)
            {
                if (users[i].ID == currentUserId && i != 0)
                {
                    User temp = users[i];
                    users[i] = users[0];
                    users[0] = temp;
                }
            }
            return users;
        }

        private void SortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            User currentUser = ViewModel.CurrentShownUser;
            if (currentUser != null)
            {
                switch (e.AddedItems[0])
                {
                    case "Time played":
                        {
                            ViewModel.UserProgresses = ViewModel.UserProgresses.OrderByDescending(o => o.MinutesPlayed).ToList();

                        }
                        break;
                    case "Last played":
                        {
                            ViewModel.UserProgresses = ViewModel.UserProgresses.OrderByDescending(o => o.LastPlayedAt).ToList();
                        }
                        break;
                    case "State":
                        {
                            ViewModel.UserProgresses = ViewModel.UserProgresses.OrderByDescending(o => o.State).ToList();
                        }
                        break;
                }
            }
        }

        private void GameImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (((Progress)((FrameworkElement)sender).DataContext).Game == null)
            {
                MainWindowViewModel.Instance.AppBarText = "Cannot open unknown game";
                return;
            }
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(((Progress)((FrameworkElement)sender).DataContext).Game));
        }
        private async void ReloadUser_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((TextBlock)sender).IsEnabled = false;
            try
            {
                int currentUserId = ViewModel.CurrentShownUser.ID;
                ViewModel.CurrentShownUser = await Task<User>.Run(() =>
                {
                    string currentShownUser = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{currentUserId}");
                    return JsonSerializer.Deserialize<User>(currentShownUser);
                });
            }
            catch (Exception ex) { }
            SortBy_SelectionChanged(null, new SelectionChangedEventArgs(System.Windows.Controls.Primitives.Selector.SelectionChangedEvent, new List<string>(), new List<string> { uiSortBy.SelectedValue.ToString() }));
            ((TextBlock)sender).IsEnabled = true;
        }
        private void UserEdit_Clicked(object sender, RoutedEventArgs e)
        {
            uiUserEditPopup.IsOpen = true;
            var obj = new UserEditUserControl(ViewModel.CurrentShownUser);
            obj.UserSaved += UserSaved;
            uiUserEditPopup.Child = obj;
        }
        protected async void UserSaved(object sender, EventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            this.IsEnabled = false;
            User selectedUser = (User)((Button)sender).DataContext;
            bool error = false;
            string url = @$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/{selectedUser.ID}";
            if (LoginManager.Instance.GetCurrentUser().ID == selectedUser.ID)
            {
                url = @$"{SettingsViewModel.Instance.ServerUrl}/api/v1/users/me";
            }
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put(url, JsonSerializer.Serialize(selectedUser));
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
                if (LoginManager.Instance.GetCurrentUser().ID == selectedUser.ID)
                {
                    await LoginManager.Instance.ManualLogin(selectedUser.Username, string.IsNullOrEmpty(selectedUser.Password) ? WebHelper.GetCredentials()[1] : selectedUser.Password);
                    MainWindowViewModel.Instance.UserIcon = LoginManager.Instance.GetCurrentUser();
                }
                await InitUserList();
            }
           ((Button)sender).IsEnabled = true;
            this.IsEnabled = true;
        }
        private async void DeleteProgress_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                Progress dataContext = (Progress)((FrameworkElement)sender).DataContext;
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to delete the progress of '{dataContext.Game.Title}' ?",
                    "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    WebHelper.Delete(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/progresses/{dataContext.ID}");
                    //ToDo: Dirty but i dont want to use ObservableCollection only for this one action
                    List<Progress> copy = ViewModel.UserProgresses;
                    copy.Remove(dataContext);
                    ViewModel.UserProgresses = null;
                    ViewModel.UserProgresses = copy;

                    MainWindowViewModel.Instance.AppBarText = $"Successfully deleted progress";
                }
            }
            catch (WebException webex)
            {
                MainWindowViewModel.Instance.AppBarText = $"Could not delete the Progress. {WebExceptionHelper.GetServerMessage(webex)}";
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = $"Could not delete the Progress. {ex.Message}";
            }
        }
    }
}
