using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Gaming.Input;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for LibraryUserControl.xaml
    /// </summary>
    public partial class LibraryUserControl : UserControl
    {
        private LibraryViewModel ViewModel;
        private InputTimer inputTimer { get; set; }

        private bool scrollBlocked = false;
        private Guid searchCancellationToken = Guid.Empty;
        public LibraryUserControl()
        {
            InitializeComponent();
            ViewModel = new LibraryViewModel();
            int sortByIndex = 0;
            if (SettingsViewModel.Instance.RetainLibarySortByAndOrderBy)
            {
                try
                {
                    uiFilterOrderBy.IsChecked = Preferences.Get(AppConfigKey.LastLibraryOrderBy, LoginManager.Instance.GetUserProfile().UserConfigFile) == "desc" ? true : false;

                    string lastSortBy = Preferences.Get(AppConfigKey.LastLibrarySortBy, LoginManager.Instance.GetUserProfile().UserConfigFile);

                    for (int i = 0; i < ViewModel.GameFilterSortByValues.Count; i++)
                    {
                        if (ViewModel.GameFilterSortByValues.ElementAt(i).Value == lastSortBy)
                        {
                            sortByIndex = i;
                            break;
                        }
                    }
                }
                catch { }
            }
            uiFilterSortBy.SelectedIndex = sortByIndex;
            uiFilterOrderBy.Checked += OrderBy_Changed;
            uiFilterOrderBy.Unchecked += OrderBy_Changed;

            this.DataContext = ViewModel;
            InitTimer();
        }
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                this.Focus();
            }
        }
        public async Task LoadLibrary()
        {
            await Search();
        }
        public void ShowLibraryError()
        {
            ViewModel.CanLoadServerGames = false;
            if (!uiExpanderGameCards.IsExpanded)
            {
                uiExpanderGameCards.IsExpanded = true;
            }
        }
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputTimer.Stop();
            inputTimer.Data = ((TextBox)sender).Text;
            inputTimer.Start();
        }
        private void InitTimer()
        {
            inputTimer = new InputTimer();
            inputTimer.Interval = TimeSpan.FromMilliseconds(400);
            inputTimer.Tick += InputTimerElapsed;
        }
        private async void InputTimerElapsed(object sender, EventArgs e)
        {
            inputTimer?.Stop();
            await Search();
        }

        private async Task Search()
        {
            Guid currentSearchToken = Guid.NewGuid();
            searchCancellationToken = currentSearchToken;

            if (!LoginManager.Instance.IsLoggedIn())
            {
                MainWindowViewModel.Instance.AppBarText = "You are offline";
                return;
            }
            if (!uiExpanderGameCards.IsExpanded)
            {
                uiExpanderGameCards.IsExpanded = true;
            }
            ScrollViewer itemScroll = ((ScrollViewer)uiServerGamesItemsControl.Template.FindName("PART_ItemsScroll", uiServerGamesItemsControl));
            if (itemScroll != null)
            {
                itemScroll.ScrollToTop();
            }

            TaskQueue.Instance.ClearQueue();

            string gameSortByFilter = ViewModel.SelectedGameFilterSortBy.Value;
            string gameOrderByFilter = (bool)uiFilterOrderBy.IsChecked ? "DESC" : "ASC";
            ViewModel.GameCards.Clear();
            string filterUrl = @$"{SettingsViewModel.Instance.ServerUrl}/api/games?search={inputTimer.Data}&sortBy={gameSortByFilter}:{gameOrderByFilter}&limit=50";
            filterUrl = ApplyFilter(filterUrl);

            PaginatedData<Game>? gameResult = await GetGamesData(filterUrl);
            if (currentSearchToken != searchCancellationToken)
                return;

            if (gameResult != null)
            {
                ViewModel.CanLoadServerGames = true;
                ViewModel.TotalGamesCount = gameResult.Meta.TotalItems;
                if (gameResult.Data.Length > 0)
                {
                    ViewModel.NextPage = gameResult.Links.Next;
                    await ProcessGamesData(gameResult);
                }
            }
            else
            {
                ViewModel.CanLoadServerGames = false;
            }
        }
        private async void ReloadLibrary_Click(object sender, EventArgs e)
        {
            if (e.GetType() == typeof(MouseButtonEventArgs))
                ((MouseButtonEventArgs)e).Handled = true;

            //Block spamming the reload button and F5 at the same time
            if (uiBtnReloadLibrary.IsEnabled == false || (e.GetType() == typeof(KeyEventArgs) && ((KeyEventArgs)e).Key != Key.F5))
                return;

            uiBtnReloadLibrary.IsEnabled = false;
            await Search();
            if ((e.GetType() == typeof(KeyEventArgs) && ((KeyEventArgs)e).Key == Key.F5))
            {
                await MainWindowViewModel.Instance.Library.GetGameInstalls().RestoreInstalledGames();
            }
            uiBtnReloadLibrary.IsEnabled = true;
        }
        public InstallUserControl GetGameInstalls()
        {
            return uiGameInstalls;
        }
        private async Task<PaginatedData<Game>?> GetGamesData(string url)
        {
            try
            {
                string gameList = await WebHelper.GetAsync(url);
                return JsonSerializer.Deserialize<PaginatedData<Game>>(gameList);
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
                return null;
            }
        }
        private async Task ProcessGamesData(PaginatedData<Game> gameResult)
        {
            await Task.Run(() =>
            {
                foreach (Game game in gameResult.Data)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ViewModel.GameCards.Add(game);
                    });
                }
            });
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                string url = e.Uri.OriginalString;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex) { MainWindowViewModel.Instance.AppBarText = ex.Message; }
        }
        private void GameCard_Clicked(object sender, RoutedEventArgs e)
        {
            if ((Game)((FrameworkElement)sender).DataContext == null)
                return;
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl((Game)((FrameworkElement)sender).DataContext));
        }

        private void Filter_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!uiExpanderGameCards.IsExpanded)
            {
                uiExpanderGameCards.IsExpanded = true;
            }
            ViewModel.FilterVisibility = ViewModel.FilterVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
        private void OpenFilterIfClosed()
        {
            if (!uiExpanderGameCards.IsExpanded)
            {
                uiExpanderGameCards.IsExpanded = true;
            }
            if (ViewModel.FilterVisibility == Visibility.Collapsed)
            {
                ViewModel.FilterVisibility = Visibility.Visible;
            }
        }
        private async void ClearAllFilters_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ClearAllFilters();
            await Search();
        }
        public void ClearAllFilters()
        {
            uiFilterGameTypeSelector.ClearEntries();
            uiFilterGenreSelector.ClearEntries();
            uiFilterTagSelector.ClearEntries();
            uiFilterGameStateSelector.ClearEntries();
            uiFilterReleaseDateRangeSelector.ClearSelection();
            uiFilterPublisherSelector.ClearEntries();
            uiFilterDeveloperSelector.ClearEntries();
            uiFilterBookmarks.IsChecked = false;
            uiFilterEarlyAccess.IsChecked = false;

            RefreshFilterCounter();
        }
        private async void Library_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if ((ScrollViewer)sender != uiMainScrollBar)
            {
                double scrollPercentage = e.VerticalOffset / ((ScrollViewer)sender).ScrollableHeight * 100;

                ViewModel.ScrollToTopVisibility = scrollPercentage > 10 ? Visibility.Visible : Visibility.Collapsed;

                if (scrollBlocked == false && ViewModel.NextPage != null && scrollPercentage > 90)
                {
                    scrollBlocked = true;
                    PaginatedData<Game>? gameResult = await GetGamesData(ViewModel.NextPage);
                    ViewModel.NextPage = gameResult?.Links.Next;
                    if (gameResult == null || gameResult.Data == null)
                    {
                        MainWindowViewModel.Instance.AppBarText = "Failed to load next Page";
                        return;
                    }
                    await ProcessGamesData(gameResult);
                    scrollBlocked = false;
                }
            }
        }
        private void Library_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((e.Delta > 0 && ((ScrollViewer)sender).VerticalOffset == 0) || (e.Delta < 0 && ViewModel.NextPage == null && ((ScrollViewer)sender).VerticalOffset == ((ScrollViewer)sender).ScrollableHeight))
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                uiMainScrollBar.RaiseEvent(eventArg);
            }
        }

        private void ScrollToTop_Click(object sender, MouseButtonEventArgs e)
        {
            ((ScrollViewer)((Grid)((FrameworkElement)sender).Parent).Children[0]).ScrollToTop();
        }

        private async void OrderBy_Changed(object sender, RoutedEventArgs e)
        {
            Preferences.Set(AppConfigKey.LastLibraryOrderBy, (bool)uiFilterOrderBy.IsChecked ? "desc" : "asc", LoginManager.Instance.GetUserProfile().UserConfigFile);
            await Search();
        }
        private string ApplyFilter(string filter)
        {
            string gameType = uiFilterGameTypeSelector.GetSelectedEntries();
            if (gameType != string.Empty)
            {
                filter += $"&filter.type=$in:{gameType}";
            }
            if (uiFilterEarlyAccess.IsChecked == true)
            {
                filter += "&filter.metadata.early_access=$eq:true";
            }
            if (uiFilterReleaseDateRangeSelector.IsValid())
            {
                filter += $"&filter.metadata.release_date=$btw:{uiFilterReleaseDateRangeSelector.GetYearFrom()}-01-01,{uiFilterReleaseDateRangeSelector.GetYearTo()}-12-31";
            }
            string genres = uiFilterGenreSelector.GetSelectedEntries();
            if (genres != string.Empty)
            {
                filter += $"&filter.metadata.genres.name=$in:{genres}";
            }
            string tags = uiFilterTagSelector.GetSelectedEntries();
            if (tags != string.Empty)
            {
                filter += $"&filter.metadata.tags.name=$in:{tags}";
            }
            string developers = uiFilterDeveloperSelector.GetSelectedEntries();
            if (developers != string.Empty)
            {
                filter += $"&filter.metadata.developers.name=$in:{developers}";
            }
            string publishers = uiFilterPublisherSelector.GetSelectedEntries();
            if (publishers != string.Empty)
            {
                filter += $"&filter.metadata.publishers.name=$in:{publishers}";
            }
            string gameStates = uiFilterGameStateSelector.GetSelectedEntries();
            if (gameStates != string.Empty)
            {
                filter += $"&filter.progresses.state=$eq:{gameStates}&filter.progresses.user.id=$eq:{LoginManager.Instance.GetCurrentUser()?.ID}";
            }
            if (uiFilterBookmarks.IsChecked == true)
            {
                filter += $"&filter.bookmarked_users.id=$eq:{LoginManager.Instance.GetCurrentUser()?.ID}";
            }
            return filter;
        }
        private void SelectedGameFilterSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ComboBox)sender).SelectionChanged -= SelectedGameFilterSortBy_SelectionChanged;
            ((ComboBox)sender).SelectionChanged += FilterUpdated;
        }
        private async void FilterUpdated(object sender, EventArgs e)
        {
            if (sender == uiFilterSortBy)
            {
                Preferences.Set(AppConfigKey.LastLibrarySortBy, ViewModel.SelectedGameFilterSortBy.Value, LoginManager.Instance.GetUserProfile().UserConfigFile);
            }
            OpenFilterIfClosed();
            RefreshFilterCounter();
            await Search();
        }


        private async void RandomGame_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ((FrameworkElement)sender).IsEnabled = false;
            Random random = new Random();
            if (random.Next(0, 100) < 7)
            {
                try
                {
                    uiImgRandom.Source = new Uri("https://phalco.de/images/gamevault/eastereggs/777.mp4");
                    uiImgRandom.Visibility = Visibility.Visible;
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(6000);
                    timer.Tick += (s, e) => { timer.Stop(); uiImgRandom.Source = null; uiImgRandom.Visibility = Visibility.Collapsed; Process.Start(new ProcessStartInfo("https://www.ncpgambling.org/help-treatment/") { UseShellExecute = true }); };
                    timer.Start();
                    AnalyticsHelper.Instance.SendCustomEvent(CustomAnalyticsEventKeys.EASTER_EGG, new { name = "777" });
                }
                catch { }
            }
            else
            {
                Game? result = null;
                try
                {
                    string randomGame = await WebHelper.GetAsync($"{SettingsViewModel.Instance.ServerUrl}/api/games/random");
                    result = JsonSerializer.Deserialize<Game>(randomGame);
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
                }

                if (result != null)
                {
                    MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(result, true));
                }
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
        private async void CardSettings_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                ContentControl parent = ((Grid)((FrameworkElement)sender).Parent).TemplatedParent as ContentControl;
                if (parent.Tag == "busy")
                    return;

                parent.Tag = "busy";
                try
                {
                    string result = await WebHelper.GetAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{((Game)((FrameworkElement)sender).DataContext).ID}");
                    Game resultGame = JsonSerializer.Deserialize<Game>(result);
                    MainWindowViewModel.Instance.OpenPopup(new GameSettingsUserControl(resultGame) { Width = 1200, Height = 800, Margin = new Thickness(50) });
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
                }
                parent.Tag = "";
            }
            catch { }
        }
        private async void CardBookmark_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                ContentControl parent = ((Grid)((FrameworkElement)sender).Parent).TemplatedParent as ContentControl;
                if (parent.Tag == "busy")
                {
                    ((ToggleButton)sender).IsChecked = !((ToggleButton)sender).IsChecked;
                    return;
                }
                parent.Tag = "busy";
                try
                {
                    Game currentGame = (Game)((FrameworkElement)sender).DataContext;
                    if ((bool)((ToggleButton)sender).IsChecked == false)
                    {
                        await WebHelper.DeleteAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me/bookmark/{currentGame.ID}");
                        currentGame.BookmarkedUsers = new List<User>();
                    }
                    else
                    {
                        await WebHelper.PostAsync(@$"{SettingsViewModel.Instance.ServerUrl}/api/users/me/bookmark/{currentGame.ID}", "");
                        currentGame.BookmarkedUsers = new List<User> { LoginManager.Instance.GetCurrentUser()! };
                    }

                }
                catch (Exception ex)
                {
                    string message = WebExceptionHelper.TryGetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = message;
                }
                parent.Tag = "";
            }
            catch { }
        }
        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await MainWindowViewModel.Instance.Downloads.TryStartDownload((Game)(((FrameworkElement)sender).DataContext));
        }
        private void RefreshFilterCounter()
        {
            int filterCount = 0;
            filterCount += uiFilterGameTypeSelector.HasEntries() ? 1 : 0;
            filterCount += uiFilterGenreSelector.HasEntries() ? 1 : 0;
            filterCount += uiFilterTagSelector.HasEntries() ? 1 : 0;
            filterCount += uiFilterDeveloperSelector.HasEntries() ? 1 : 0;
            filterCount += uiFilterPublisherSelector.HasEntries() ? 1 : 0;
            filterCount += uiFilterGameStateSelector.HasEntries() ? 1 : 0;
            filterCount += (bool)uiFilterEarlyAccess.IsChecked ? 1 : 0;
            filterCount += (bool)uiFilterBookmarks.IsChecked ? 1 : 0;

            filterCount += (uiFilterReleaseDateRangeSelector.IsValid()) ? 1 : 0;
            ViewModel.FilterCounter = filterCount == 0 ? string.Empty : filterCount.ToString();
        }
        public void RefreshGame(Game gameToRefreshParam)
        {
            Game? gameToRefresh = ViewModel.GameCards.Where(g => g.ID == gameToRefreshParam.ID).FirstOrDefault();
            if (gameToRefresh != null)
            {
                int index = ViewModel.GameCards.IndexOf(gameToRefresh);
                ViewModel.GameCards[index] = null;
                ViewModel.GameCards[index] = gameToRefreshParam;
            }
        }
        #region PREVENT WEIRD AUTO SCROLL
        //The main scrollbar starts scrolling if i click in the server games section. Could not find a better solution for this Problem. Thats why this bad workaround.
        bool isProgrammaticScroll = false;
        private void uiMainScrollBar_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            isProgrammaticScroll = true;
        }

        private void uiMainScrollBar_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (isProgrammaticScroll)
            {
                e.Handled = true;
                isProgrammaticScroll = false;
                ((ScrollViewer)sender).ScrollToVerticalOffset(e.VerticalOffset - e.VerticalChange);
            }
        }


        #endregion

    }
}
