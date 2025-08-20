using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace gamevault.UserControls
{
    public partial class CommunityUserControl : UserControl
    {
        private CommunityViewModel ViewModel { get; set; }
        private int forceShowId = -1;

        private Storyboard skeletonAnimation;
        private List<Storyboard> runningSkeletonAnimations = new List<Storyboard>();
        private List<Border> skeletonBorders = new List<Border>();
        public CommunityUserControl()
        {
            InitializeComponent();
            ViewModel = new CommunityViewModel();
            this.DataContext = ViewModel;
        }
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                this.Focus();
            }
        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                try
                {
                    InitUserLoadingAnimation();
                    await InitUserList();
                }
                catch
                {
                    MainWindowViewModel.Instance.AppBarText = "Can not access community tab while offline";
                }
            }
        }
        private void InitUserLoadingAnimation()
        {
            try
            {
                if (skeletonAnimation != null)
                    return;  //Already initialized

                ViewModel.LoadingUser = true;
                skeletonAnimation = (Storyboard)FindResource("SkeletonLoadingAnimation");
                Style? skeletonBorderStyle = FindResource("SkeletonBorderStyle") as Style;
                foreach (var border in VisualHelper.FindVisualChildren<Border>(LoadingPlaceholder))
                {
                    if (border.Style == skeletonBorderStyle)
                    {
                        skeletonBorders.Add(border);
                    }
                }
                if (skeletonAnimation != null)
                {
                    foreach (var border in skeletonBorders)
                    {
                        skeletonAnimation = skeletonAnimation.Clone();
                        Storyboard.SetTarget(skeletonAnimation, border);
                        skeletonAnimation.Begin(border, true);
                        runningSkeletonAnimations.Add(skeletonAnimation);
                    }
                    LoadingPlaceholder.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }
        private void LoadingPlaceholder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (skeletonAnimation == null)
                return;


            Visibility visibility = ((FrameworkElement)sender).Visibility;
            if (visibility == Visibility.Collapsed)
            {
                for (int i = 0; i < runningSkeletonAnimations.Count; i++)
                {
                    runningSkeletonAnimations[i].Pause(skeletonBorders[i]);
                }
            }
            else if (visibility == Visibility.Visible)
            {
                for (int i = 0; i < runningSkeletonAnimations.Count; i++)
                {
                    runningSkeletonAnimations[i].Resume(skeletonBorders[i]);
                }
            }
        }
        public async Task InitUserList()
        {
            string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users");
            var users = JsonSerializer.Deserialize<User[]>(result);
            users = BringCurrentUserToTop(users?.Where(u => u.DeletedAt == null)?.ToArray());
            //Log server user count only on the first time its set
            if (ViewModel.Users == null)
            {
                AnalyticsHelper.Instance.SendCustomEvent(CustomAnalyticsEventKeys.SERVER_USER_COUNT, new { usercount = users?.Length.ToString() });
            }
            ViewModel.Users = users;
            if (uiSelectUser.SelectedIndex == -1 && ViewModel.CurrentShownUser == null)
            {
                if (forceShowId != -1)
                {
                    int index = ViewModel.Users.ToList().FindIndex(u => u.ID == forceShowId);
                    if (index != -1)
                    {
                        uiSelectUser.SelectedIndex = index;
                    }
                }
                else
                {
                    uiSelectUser.SelectedIndex = 0;
                }
            }
        }
        internal void Reset()
        {
            ViewModel = new CommunityViewModel();
            this.DataContext = ViewModel;
        }
        internal void ShowUser(User userToShow)
        {
            if (userToShow != null)
            {
                forceShowId = userToShow.ID;
                if (MainWindowViewModel.Instance.ActiveControl == MainWindowViewModel.Instance.Community)
                {
                    InitUserList();
                }
                else
                {
                    MainWindowViewModel.Instance.SetActiveControl(MainControl.Community);
                }
            }
        }
        private async void Users_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (uiSelectUser.SelectedIndex == -1 && ViewModel.CurrentShownUser != null)
                {
                    int index = -1;
                    if (forceShowId == -1)
                    {
                        index = ViewModel.Users.ToList().FindIndex(u => u.ID == ViewModel.CurrentShownUser.ID);
                    }
                    else
                    {
                        index = ViewModel.Users.ToList().FindIndex(u => u.ID == forceShowId);
                        forceShowId = -1;
                    }
                    if (index != -1)
                    {
                        uiSelectUser.SelectedIndex = index;
                    }
                    return;
                }
                uiProgressScrollView.ScrollToTop();
                int selectedUserId = -1;
                if (e.AddedItems.Count == 0)
                {
                    return;
                }
                else
                {
                    selectedUserId = ((User)e.AddedItems[0]).ID;
                }
                ViewModel.LoadingUser = true;
                string userUrl = @$"{SettingsViewModel.Instance.ServerUrl}/api/users/{selectedUserId}";
                if (selectedUserId == LoginManager.Instance.GetCurrentUser()?.ID)
                {
                    userUrl = @$"{SettingsViewModel.Instance.ServerUrl}/api/users/me";
                }
                string currentShownUser = await WebHelper.GetAsync(userUrl);
                ViewModel.CurrentShownUser = JsonSerializer.Deserialize<User>(currentShownUser);
                ViewModel.LoadingUser = false;
                string lastSort = TryGetLastProgressSort();
                if (uiSortBy.SelectedValue?.ToString()?.Replace("\"", "") == lastSort)
                {
                    SortBy_SelectionChanged(null, new SelectionChangedEventArgs(Selector.SelectionChangedEvent, new string[0], new string[] { lastSort }));
                }
                else
                {
                    uiSortBy.SelectedValue = lastSort;
                }

            }
            catch (Exception ex)
            {
                ViewModel.LoadingUser = false;
                MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
            }
        }
        private string TryGetLastProgressSort()
        {
            string result = Preferences.Get(AppConfigKey.LastCommunitySortBy, LoginManager.Instance.GetUserProfile().UserConfigFile);
            try
            {
                if (!string.IsNullOrWhiteSpace(result) && ViewModel.SortBy.Contains(result))
                {
                    return result;
                }
            }
            catch { }
            return "Last played"; //default is 'Last played'
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
            if (currentUser != null && e.AddedItems != null)
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
            if (sender != null)
            {
                Preferences.Set(AppConfigKey.LastCommunitySortBy, uiSortBy.SelectedValue.ToString().Replace("\"", ""), LoginManager.Instance.GetUserProfile().UserConfigFile);
            }
        }

        private void GameImage_Click(object sender, RoutedEventArgs e)
        {
            if (((Progress)((FrameworkElement)sender).DataContext).Game == null)
            {
                MainWindowViewModel.Instance.AppBarText = "Cannot open game";
                return;
            }
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(((Progress)((FrameworkElement)sender).DataContext).Game));
        }
        private async void ReloadUser_Clicked(object sender, EventArgs e)
        {
            if (!LoginManager.Instance.IsLoggedIn())
            {
                MainWindowViewModel.Instance.AppBarText = "You are not logged in or offline";
                return;
            }
            if (uiBtnReloadUser.IsEnabled == false || (e.GetType() == typeof(KeyEventArgs) && ((KeyEventArgs)e).Key != Key.F5))
                return;

            uiBtnReloadUser.IsEnabled = false;
            try
            {
                int currentUserId = ViewModel.CurrentShownUser.ID;
                string userUrl = @$"{SettingsViewModel.Instance.ServerUrl}/api/users/{currentUserId}";
                if (currentUserId == LoginManager.Instance.GetCurrentUser()?.ID)
                {
                    userUrl = @$"{SettingsViewModel.Instance.ServerUrl}/api/users/me";
                }
                string currentShownUser = await WebHelper.GetAsync(userUrl);
                ViewModel.CurrentShownUser = JsonSerializer.Deserialize<User>(currentShownUser);
                SortBy_SelectionChanged(null, new SelectionChangedEventArgs(System.Windows.Controls.Primitives.Selector.SelectionChangedEvent, new List<string>(), new List<string> { uiSortBy.SelectedValue.ToString() }));

            }
            catch (Exception ex) { }
            uiBtnReloadUser.IsEnabled = true;
        }
        private void UserEdit_Clicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentShownUser != null)
            {
                User user = JsonSerializer.Deserialize<User>(JsonSerializer.Serialize(ViewModel.CurrentShownUser)); //Dereference
                MainWindowViewModel.Instance.OpenPopup(new UserSettingsUserControl(user) { Width = 1200, Height = 800, Margin = new Thickness(50) });
            }
        }
        private async void DeleteProgress_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Progress dataContext = (Progress)((FrameworkElement)sender).DataContext;
                MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Are you sure you want to delete the progress of '{dataContext.Game.Title}' ?",
                    "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    await WebHelper.DeleteAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{ViewModel?.CurrentShownUser?.ID}/game/{dataContext?.Game.ID}");
                    //ToDo: Dirty but i dont want to use ObservableCollection only for this one action
                    List<Progress> copy = ViewModel.UserProgresses;
                    copy.Remove(dataContext);
                    ViewModel.UserProgresses = null;
                    ViewModel.UserProgresses = copy;

                    MainWindowViewModel.Instance.AppBarText = $"Successfully deleted progress";
                }
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = $"Could not delete. {WebExceptionHelper.TryGetServerMessage(ex)}";
            }
        }
    }
}
