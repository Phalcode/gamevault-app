using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for LibraryUserControl.xaml
    /// </summary>
    public partial class LibraryUserControl : UserControl
    {
        private LibraryViewModel ViewModel { get; set; }
        private bool m_Loaded = false;
        private ScrollBar? m_GameCardScrollBar { get; set; }
        private string m_Next = null;
        private bool m_BlockPaging = false;
        private double m_PagingScrollBoder = 0.65;
        public LibraryUserControl()
        {
            InitializeComponent();
            ViewModel = new LibraryViewModel();
            InitYearFilter();
            this.DataContext = ViewModel;
        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (false == m_Loaded)
            {
                if (Preferences.Get(AppConfigKey.LibStartup, AppFilePath.UserFile) == "1")
                {
                    await Search();
                }
                m_Loaded = true;
            }
        }
        private async void On_Search(object sender, EventArgs e)
        {
            if (sender.GetType().Name == "TextBox" && ((System.Windows.Input.KeyEventArgs)e).Key != System.Windows.Input.Key.Enter)
                return;
            await Search();
        }
        private async Task Search()
        {
            if (!LoginManager.Instance.IsLoggedIn())
            {
                if (true == m_Loaded)
                {
                    MainWindowViewModel.Instance.AppBarText = "You are not logged in";
                }
                return;
            }
            TaskQueue.Instance.ClearQueue();
            m_Next = null;
            uiGameCardScrollViewer.ScrollToTop();

            ViewModel.IsSearchEnabled = false;
            string gameSortByFilter = ViewModel.SelectedGameFilterSortBy.Value;
            string gameOrderByFilter = ViewModel.SelectedGameFilterOrderBy.Value;
            ViewModel.GameCards.Clear();
            string filterUrl = @$"{SettingsViewModel.Instance.ServerUrl}/api/games?search={ViewModel.SearchQuery}&sortBy={gameSortByFilter}:{gameOrderByFilter}&limit=80";
            filterUrl = ApplyFilter(filterUrl);

            PaginatedData<Game>? gameResult = await GetGamesData(filterUrl);//add try catch
            if (gameResult != null)
            {
                ViewModel.TotalGamesCount = gameResult.Meta.TotalItems;
                if (gameResult.Data.Length > 0)
                {
                    m_Next = gameResult.Links.Next;
                    await ProcessGamesData(gameResult);
                }
            }
            ViewModel.IsSearchEnabled = true;
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
        private void GameCard_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.SetActiveControl(new GameViewUserControl((Game)((FrameworkElement)sender).DataContext));
        }
        private void Filter_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            uiFilterView.IsPaneOpen = !uiFilterView.IsPaneOpen;
            if (!uiFilterView.IsPaneOpen)
            {
                uiBtnFilter.Text = "FILTER";
            }
            else
            {
                uiBtnFilter.Text = "CLOSE";
            }
        }
        private string ApplyFilter(string filter)
        {
            if (ViewModel.SelectedGameFilterGameType.Value != string.Empty)
            {
                filter += $"&filter.type=$eq:{ViewModel.SelectedGameFilterGameType.Value}";
            }
            if (ViewModel.EarlyAccessOnly == true)
            {
                filter += "&filter.early_access=$eq:true";
            }
            if (uiYearFilterSlider.Minimum != ViewModel.YearFilterLower || uiYearFilterSlider.Maximum != ViewModel.YearFilterUpper)
            {
                filter += $"&filter.release_date=$btw:{ViewModel.YearFilterLower}-01-01,{ViewModel.YearFilterUpper}-12-31";
            }
            string genres = uiGenreSelector.GetSelectedValues();
            if (genres != string.Empty)
            {
                filter += $"&filter.genres.name=$in:{genres}";
            }
            string tags = uiTagSelector.GetSelectedValues();
            if (tags != string.Empty)
            {
                filter += $"&filter.tags.name=$in:{tags}";
            }
            return filter;
        }
        private void InitYearFilter()
        {
            uiYearFilterSlider.Maximum = DateTime.Now.Year;
            ViewModel.YearFilterLower = (int)uiYearFilterSlider.Minimum;
            ViewModel.YearFilterUpper = (int)uiYearFilterSlider.Maximum;
        }

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            if (m_GameCardScrollBar == null)
            {
                m_GameCardScrollBar = uiGameCardScrollViewer.Template.FindName("PART_VerticalScrollBar", ((ScrollViewer)sender)) as ScrollBar;
            }
            else
            {
                double scrollPercentage = m_GameCardScrollBar.Value / m_GameCardScrollBar.Maximum;
                if (m_BlockPaging == false && scrollPercentage > m_PagingScrollBoder && m_Next != null)
                {
                    m_BlockPaging = true;
                    PaginatedData<Game>? gameResult = await GetGamesData(m_Next);
                    m_Next = gameResult.Links.Next;
                    await ProcessGamesData(gameResult);
                    m_BlockPaging = false;
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string url = e.Uri.OriginalString;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            e.Handled = true;
        }

        private async void RandomGame_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
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
