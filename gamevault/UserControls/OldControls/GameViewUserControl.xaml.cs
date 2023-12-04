using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;


namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for GameViewUserControl.xaml
    /// </summary>
    public partial class GameViewUserControl : UserControl
    {
        private GameViewViewModel ViewModel { get; set; }
        private int m_GameId { get; set; }
        private bool m_Loaded = false;
        public GameViewUserControl(Game game, bool reloadGameObject = true)
        {
            InitializeComponent();
            ViewModel = new GameViewViewModel();
            if (false == reloadGameObject)
            {
                ViewModel.Game = game;
            }
            m_GameId = game.ID;
            if (InstallViewModel.Instance.InstalledGames.Where(x => x.GetGameId() == m_GameId).Count() > 0)
            {
                ViewModel.IsAlreadyInstalled = true;
            }
            ViewModel.States = Enum.GetNames(typeof(State));
            this.DataContext = ViewModel;
        }

        private async void GameView_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            if (!m_Loaded)
            {
                m_Loaded = true;
                if (ViewModel.Game == null)
                {
                    try
                    {
                        ViewModel.Game = await Task<Game>.Run(() =>
                        {
                            string gameList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{m_GameId}");
                            return System.Text.Json.JsonSerializer.Deserialize<Game>(gameList);
                        });
                        ViewModel.Progress = await Task<Progress>.Run(() =>
                        {
                            string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{m_GameId}");
                            return System.Text.Json.JsonSerializer.Deserialize<Progress>(result);
                        });
                    }
                    catch (Exception ex) { }
                }
            }
            //(uiTagScrollView.Template.FindName("PART_HorizontalScrollBar", uiTagScrollView) as ScrollBar).Height = 7;
        }
        private void BackButton_Clicked(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
        }
        private void KeyBindingEscape_OnExecuted(object sender, object e)
        {
            this.Focusable = false;
            MainWindowViewModel.Instance.UndoActiveControl();
        }
        private void Website_Navigate(object sender, RequestNavigateEventArgs e)
        {
            string url = e.Uri.OriginalString;
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Download_Click(object sender, MouseButtonEventArgs e)
        {
            if (LoginManager.Instance.IsLoggedIn() == false)
            {
                MainWindowViewModel.Instance.AppBarText = "Could not connect to server";
                return;
            }
            if (true == ViewModel.IsAlreadyInstalled)
            {
                //MainWindowViewModel.Instance.SetActiveControl(MainControl.Installs);
                return;
            }
            this.Focus();//Bring back focus for the escape key
            if (SettingsViewModel.Instance.RootPath == string.Empty)
            {
                MainWindowViewModel.Instance.AppBarText = "Root path is not set! Go to ⚙️Settings->Data";
                return;
            }
            if (IsAlreadyDownloading(ViewModel.Game.ID))
            {
                MainWindowViewModel.Instance.AppBarText = $"'{ViewModel.Game.Title}' is already in the download queue";
                return;
            }
            if (IsEnoughDriveSpaceAvailable(SettingsViewModel.Instance.RootPath, Convert.ToInt64(ViewModel.Game.Size)))
            {
                DownloadsViewModel.Instance.DownloadedGames.Insert(0, new GameDownloadUserControl(ViewModel.Game, true));
                MainWindowViewModel.Instance.AppBarText = $"'{ViewModel.Game.Title}' has been added to the download queue";
            }
            else
            {
                FileInfo f = new FileInfo(SettingsViewModel.Instance.RootPath);
                string? driveName = Path.GetPathRoot(f.FullName);
                MainWindowViewModel.Instance.AppBarText = $"Not enough space available for drive {driveName}";
            }
        }
        bool m_ProgressLoaded = false;
        private async void State_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_ProgressLoaded)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{m_GameId}", System.Text.Json.JsonSerializer.Serialize(new Progress() { State = ViewModel.Progress.State }));
                    }
                    catch (WebException webEx)
                    {
                        string msg = WebExceptionHelper.GetServerMessage(webEx);
                        if (msg == string.Empty)
                        {
                            msg = "Could not connect to server";
                        }
                        MainWindowViewModel.Instance.AppBarText = msg;
                    }
                    catch { }
                });
            }
            else
            {
                m_ProgressLoaded = true;//Because selection changed event is triggered by loading the progress into the view model
            }
        }
        private async void RawgGameSearch_Click(object sender, RoutedEventArgs e)
        {
            await RawgGameSearch();
        }
        private async void RawgGameSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await RawgGameSearch();
            }
        }
        private async Task RawgGameSearch()
        {
            uiBtnRawgGameSearch.IsEnabled = false;
            ViewModel.RawgGames = await Task<RawgGame[]>.Run(() =>
            {
                try
                {
                    string currentShownUser = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/rawg/search?query={ViewModel.RawgSearchQuery}");
                    return JsonSerializer.Deserialize<RawgGame[]>(currentShownUser);
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = $"Could not load rawg data. ({ex.Message})";
                    return null;
                }
            });
            uiBtnRawgGameSearch.IsEnabled = true;
        }
        private async void RawgGameRemap_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GameRemapPopupVisibillity = Visibility.Collapsed;
            int? rawgId = ((RawgGame)((FrameworkElement)sender).DataContext).ID;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put($"{SettingsViewModel.Instance.ServerUrl}/api/games/{m_GameId}", "{\n\"rawg_id\": " + rawgId + "\n}");
                    MainWindowViewModel.Instance.AppBarText = "Successfully remapped game";
                }
                catch (WebException ex)
                {
                    string errMessage = WebExceptionHelper.GetServerMessage(ex);
                    if (errMessage == string.Empty) { errMessage = "Failed to remap game"; }
                    MainWindowViewModel.Instance.AppBarText = errMessage;
                }
            });
        }
        private void GameRemapPopup_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.GameRemapPopupVisibillity == Visibility.Visible)
            {
                ViewModel.GameRemapPopupVisibillity = Visibility.Collapsed;
            }
            else
            {
                ViewModel.GameRemapPopupVisibillity = Visibility.Visible;
            }

        }
        private bool IsEnoughDriveSpaceAvailable(string path, long gameSize)
        {
            FileInfo f = new FileInfo(path);
            string? driveName = Path.GetPathRoot(f.FullName);
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    if ((drive.AvailableFreeSpace - 1000) > gameSize)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }
        private bool IsAlreadyDownloading(int id)
        {
            if (DownloadsViewModel.Instance.DownloadedGames.Where(gameUC => gameUC.IsGameIdDownloading(id) == true).Count() > 0)
            {
                return true;
            }
            return false;
        }

        private async void Recache_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/rawg/{ViewModel.Game.ID}/recache", string.Empty);
                    MainWindowViewModel.Instance.AppBarText = $"Sucessfully re-cached {ViewModel.Game.Title}";
                }
                catch (WebException ex)
                {
                    string msg = WebExceptionHelper.GetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            });
            ((Button)sender).IsEnabled = true;
        }

        private void UploadBoxArtImage_Click(object sender, RoutedEventArgs e)
        {
            if (fileSelectionPopupPP.Visibility == Visibility.Visible)
            {
                fileSelectionPopupPP.Visibility = Visibility.Collapsed;
            }
            else
            {
                fileSelectionPopupPP.Visibility = Visibility.Visible;
            }
            fileSelectionPopupBP.Visibility = Visibility.Collapsed;
        }

        private void UploadBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            if (fileSelectionPopupBP.Visibility == Visibility.Visible)
            {
                fileSelectionPopupBP.Visibility = Visibility.Collapsed;
            }
            else
            {
                fileSelectionPopupBP.Visibility = Visibility.Visible;
            }
            fileSelectionPopupPP.Visibility = Visibility.Collapsed;
        }
        private async void ImagesSave_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    dynamic updateObject = new System.Dynamic.ExpandoObject();
                    updateObject.box_image_id = ViewModel.UpdatedBoxImageId;
                    updateObject.box_image_url = ViewModel.UpdatedBoxImageUrl;
                    updateObject.background_image_id = ViewModel.UpdatedBackgroundImageId;
                    updateObject.background_image_url = ViewModel.UpdatedBackgroundImageUrl;
                    WebHelper.Put($"{SettingsViewModel.Instance.ServerUrl}/api/games/{m_GameId}", JsonSerializer.Serialize(updateObject));
                    MainWindowViewModel.Instance.AppBarText = "Successfully updated image";
                }
                catch (WebException ex)
                {
                    string msg = WebExceptionHelper.GetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
                catch (Exception ex)
                {
                    MainWindowViewModel.Instance.AppBarText = ex.Message;
                }
            });
            ((Button)sender).IsEnabled = true;
            this.Focus();//Bring back focus for the escape key
        }
    }
}
