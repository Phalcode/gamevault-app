using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for NewLibraryUserControl.xaml
    /// </summary>
    public partial class NewLibraryUserControl : UserControl
    {
        private NewLibraryViewModel ViewModel;
        private InputTimer inputTimer { get; set; }

        private bool scrollBlocked = false;
        public NewLibraryUserControl()
        {
            InitializeComponent();
            ViewModel = new NewLibraryViewModel();
            this.DataContext = ViewModel;
            InitTimer();
            uiFilterYearTo.Text = DateTime.Now.Year.ToString();
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
            if (!LoginManager.Instance.IsLoggedIn())
            {
                MainWindowViewModel.Instance.AppBarText = "You are not logged in";
                return;
            }
            if (!uiExpanderGameCards.IsExpanded)
            {
                uiExpanderGameCards.IsExpanded = true;
            }
            ((ScrollViewer)uiServerGamesItemsControl.Template.FindName("PART_ItemsScroll", uiServerGamesItemsControl)).ScrollToTop();

            TaskQueue.Instance.ClearQueue();

            string gameSortByFilter = ViewModel.SelectedGameFilterSortBy.Value;
            string gameOrderByFilter = ViewModel.OrderByValue;
            ViewModel.GameCards.Clear();
            string filterUrl = @$"{SettingsViewModel.Instance.ServerUrl}/api/games?search={inputTimer.Data}&sortBy={gameSortByFilter}:{gameOrderByFilter}&limit=50";
            filterUrl = ApplyFilter(filterUrl);

            PaginatedData<Game>? gameResult = await GetGamesData(filterUrl);//add try catch
            if (gameResult != null)
            {
                ViewModel.TotalGamesCount = gameResult.Meta.TotalItems;
                if (gameResult.Data.Length > 0)
                {
                    ViewModel.NextPage = gameResult.Links.Next;
                    await ProcessGamesData(gameResult);
                }
            }
        }
        public NewInstallUserControl GetGameInstalls()
        {
            return uiGameInstalls;
        }
        private async Task<PaginatedData<Game>?> GetGamesData(string url)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string gameList = WebHelper.GetRequest(url);
                    return JsonSerializer.Deserialize<PaginatedData<Game>>(gameList);
                }
                catch (JsonException exJson)
                {
                    MainWindowViewModel.Instance.AppBarText = exJson.Message;
                    return null;
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = "Could not connect to server";
                    return null;
                }
            });
        }
        private async Task ProcessGamesData(PaginatedData<Game> gameResult)
        {
            await Task.Run(async () =>
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
            string url = e.Uri.OriginalString;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            e.Handled = true;
        }

        private void GameCard_Clicked(object sender, MouseButtonEventArgs e)
        {
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
                    await ProcessGamesData(gameResult);
                    scrollBlocked = false;

                }
            }
        }

        private void ScrollToTop_Click(object sender, MouseButtonEventArgs e)
        {
            ((ScrollViewer)((Grid)((FrameworkElement)sender).Parent).Children[0]).ScrollToTop();
            //uiMainScrollBar.ScrollToTop();
        }

        private async void OrderBy_Changed(object sender, RoutedEventArgs e)
        {
            ViewModel.OrderByValue = (ViewModel.OrderByValue == "ASC") ? "DESC" : "ASC";
            await Search();
        }
        private string ApplyFilter(string filter)
        {
            string gameType = uiFilterGameTypeSelector.GetSelectedEntries();
            if (gameType != string.Empty)
            {
                filter += $"&filter.type=$in:{gameType}";
            }
            if (uiFilterEarlyAccess.IsOn == true)
            {
                filter += "&filter.early_access=$eq:true";
            }
            if (int.TryParse(uiFilterYearFrom.Text, out int yearFrom) && int.TryParse(uiFilterYearTo.Text, out int yearTo))
            {
                filter += $"&filter.release_date=$btw:{yearFrom}-01-01,{yearTo}-12-31";
            }
            string genres = uiFilterGenreSelector.GetSelectedEntries();
            if (genres != string.Empty)
            {
                filter += $"&filter.genres.name=$in:{genres}";
            }
            string tags = uiFilterTagSelector.GetSelectedEntries();
            if (tags != string.Empty)
            {
                filter += $"&filter.tags.name=$in:{tags}";
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
            await Search();
        }
        private void YearSelector_Changed(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = (((TextBox)e.Source).Text == "" && e.Text == "0") || regex.IsMatch(e.Text);
        }

        private async void RandomGame_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ((FrameworkElement)sender).IsEnabled = false;
            Game? result = await Task<Game>.Run(() =>
            {
                try
                {
                    string randomGame = WebHelper.GetRequest($"{SettingsViewModel.Instance.ServerUrl}/api/games/random");
                    return JsonSerializer.Deserialize<Game>(randomGame);
                }
                catch (JsonException exJson)
                {
                    MainWindowViewModel.Instance.AppBarText = exJson.Message;
                    return null;
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = "Could not connect to server";
                    return null;
                }
            });
            if (result != null)
            {
                MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl(result, true));
            }
            ((FrameworkElement)sender).IsEnabled = true;
        }
    }
}
