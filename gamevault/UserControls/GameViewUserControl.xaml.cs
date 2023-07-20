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
                            string gameList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/games/{m_GameId}");
                            return System.Text.Json.JsonSerializer.Deserialize<Game>(gameList);
                        });
                        ViewModel.Progress = await Task<Progress>.Run(() =>
                        {
                            string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{m_GameId}");
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
            if (true == ViewModel.IsAlreadyInstalled)
            {
                MainWindowViewModel.Instance.SetActiveControl(MainControl.Installs);
                return;
            }
            this.Focus();//Bring back focus for the escape key
            if (SettingsViewModel.Instance.RootPath == string.Empty)
            {
                MainWindowViewModel.Instance.AppBarText = "Root path is not set! Go to ⚙️Settings->Download";
                return;
            }
            if (IsAlreadyDownloading(ViewModel.Game.ID))
            {
                MainWindowViewModel.Instance.AppBarText = $"'{ViewModel.Game.Title}' is already in the download queue";
                return;
            }
            if (IsEnoughDriveSpaceAvailable(SettingsViewModel.Instance.RootPath, Convert.ToInt64(ViewModel.Game.Size)))
            {
                DownloadsViewModel.Instance.DownloadedGames.Add(new GameDownloadUserControl(ViewModel.Game, true));
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
                        WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{m_GameId}", System.Text.Json.JsonSerializer.Serialize(ViewModel.Progress));
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
                });
            }
            else
            {
                m_ProgressLoaded = true;
            }
        }
        private async void RawgGameSearch_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            ViewModel.RawgGames = await Task<Game[]>.Run(() =>
            {
                string currentShownUser = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/v1/rawg/search?query={ViewModel.RawgSearchQuery}");
                return JsonSerializer.Deserialize<Game[]>(currentShownUser);
            });
            ((Button)sender).IsEnabled = true;
        }
        private async void BoxArtImageSave_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put($"{SettingsViewModel.Instance.ServerUrl}/api/v1/utility/overwrite/{m_GameId}/box_image", "{\n\"image_url\": \"" + ViewModel.UpdatedBoxImage + "\"\n}");
                    MainWindowViewModel.Instance.AppBarText = "Successfully updated box image";
                }
                catch (WebException ex)
                {
                    string msg = WebExceptionHelper.GetServerMessage(ex);
                    MainWindowViewModel.Instance.AppBarText = msg;
                }
            });
            ((Button)sender).IsEnabled = true;
            this.Focus();//Bring back focus for the escape key
        }
        private async void RawgGameRemap_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GameRemapPopupIsOpen = false;
            int? rawgId = ((Game)((FrameworkElement)sender).DataContext).RawgId;
            await Task.Run(() =>
            {
                try
                {
                    WebHelper.Put($"{SettingsViewModel.Instance.ServerUrl}/api/v1/utility/overwrite/{m_GameId}/rawg_id", "{\n\"rawg_id\": " + rawgId + "\n}");
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
            ViewModel.GameRemapPopupIsOpen = true;
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
    }
}
